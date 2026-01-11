using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using WotlkClient.Shared;

namespace WotlkClient.Clients
{
    /// <summary>
    /// Manages AI chat using Ollama API (local)
    /// Automatically starts Ollama if not running
    /// </summary>
    public class AIChatMgr
    {
        private string prefix;
        private bool isInitialized = false;
        private Process ollamaProcess = null;

        // Configuration
        public bool AIEnabled { get; set; } = false;
        public string OllamaUrl { get; set; } = "http://localhost:11434";
        public string ModelName { get; set; } = "llama3";
        public string SystemPrompt { get; set; } = "Tu es un Druide Orc Rôliste (RP) sur World of Warcraft. Tu parles UNIQUEMENT en français. Fais des réponses COURTES (max 2 phrases). IMPORTANT: Si on te demande de SUIVRE ou VENIR, tu DOIS utiliser [CMD: follow]. N'utilise JAMAIS [CMD: emote] pour dire que tu viens. Exemple: 'Je te suis ! [CMD: follow]'.";
        
        private const string GREETING_PROMPT = "Tu croises un autre joueur nommé '{0}'. Salue-le brièvement en français (1 phrase max) en restant dans ton rôle d'aventurier World of Warcraft. Ne pose pas de question.";

        public AIChatMgr(string _prefix)
        {
            prefix = _prefix;
        }

        public string GetGreeting(string playerName)
        {
            if (!AIEnabled) return null;
            
            string prompt = string.Format(GREETING_PROMPT, playerName);
            return QueryOllama(prompt);
        }

        /// <summary>
        /// Generate a response to a message using Ollama API
        /// </summary>
        public string GetResponse(string userMessage, string contextInfo = "")
        {
            if (!isInitialized && !Initialize())
            {
                return null;
            }

            return QueryOllama(userMessage, contextInfo);
        }

        /// <summary>
        /// Initialize - start Ollama if needed and test connection
        /// </summary>
        public bool Initialize()
        {
            try
            {
                Console.WriteLine("[AI] Testing Ollama connection...");
                
                // First try to connect
                if (!IsOllamaRunning())
                {
                    Console.WriteLine("[AI] Ollama not running, attempting to start...");
                    if (!StartOllama())
                    {
                        Console.WriteLine("[AI] Failed to start Ollama. Please install from: https://ollama.ai");
                        return false;
                    }
                }

                // Check if model is available
                if (!CheckModelAvailable())
                {
                    Console.WriteLine($"[AI] Model {ModelName} not found. Pulling...");
                    PullModel();
                }

                Console.WriteLine("[AI] Ollama is running with " + ModelName + "!");
                isInitialized = true;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AI] Initialization failed: {ex.Message}");
            }
            return false;
        }

        private string QueryOllama(string userMessage, string contextInfo = "")
        {
            try
            {
                var request = (HttpWebRequest)WebRequest.Create($"{OllamaUrl}/api/generate");
                request.Method = "POST";
                request.ContentType = "application/json";
                request.Timeout = 30000; // 30 second timeout

                // Inject context into the prompt
                string prompt = $"{SystemPrompt}\n";
                if(!string.IsNullOrEmpty(contextInfo))
                {
                    prompt += $"[Contexte Actuel : {contextInfo}]\n";
                }
                prompt += $"\nUser: {userMessage}\n\nAssistant:";
                
                // Build JSON manually since we don't have JSON libraries
                string json = "{" +
                    "\"model\":\"" + EscapeJson(ModelName) + "\"," +
                    "\"prompt\":\"" + EscapeJson(prompt) + "\"," +
                    "\"stream\":false," +
                    "\"options\":{\"num_predict\":100}" +
                    "}";

                byte[] data = Encoding.UTF8.GetBytes(json);
                request.ContentLength = data.Length;

                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }

                using (var response = (HttpWebResponse)request.GetResponse())
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    string responseText = reader.ReadToEnd();
                    
                    // Parse response manually
                    string aiResponse = ExtractJsonField(responseText, "response");
                    
                    if (!string.IsNullOrEmpty(aiResponse))
                    {
                        aiResponse = aiResponse.Trim();
                        return aiResponse;
                    }
                }
            }
            catch (WebException wex)
            {
                Console.WriteLine($"[AI] Ollama request failed: {wex.Message}");
                if (wex.Response != null)
                {
                    using (var reader = new StreamReader(wex.Response.GetResponseStream()))
                    {
                        Console.WriteLine($"[AI] Response: {reader.ReadToEnd()}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine(LogType.Error, "AI Response error: {0}", prefix, ex.Message);
            }

            return null;
        }

        private bool IsOllamaRunning()
        {
            try
            {
                var request = (HttpWebRequest)WebRequest.Create($"{OllamaUrl}/api/tags");
                request.Method = "GET";
                request.Timeout = 2000;
                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    return response.StatusCode == HttpStatusCode.OK;
                }
            }
            catch
            {
                return false;
            }
        }

        private bool StartOllama()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "ollama",
                    Arguments = "serve",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                ollamaProcess = Process.Start(psi);
                
                for (int i = 0; i < 10; i++)
                {
                    Thread.Sleep(500);
                    if (IsOllamaRunning())
                    {
                        Console.WriteLine("[AI] Ollama started successfully!");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AI] Could not start Ollama: {ex.Message}");
            }
            return false;
        }

        private bool CheckModelAvailable()
        {
            try
            {
                var request = (HttpWebRequest)WebRequest.Create($"{OllamaUrl}/api/tags");
                request.Method = "GET";
                request.Timeout = 5000;
                using (var response = (HttpWebResponse)request.GetResponse())
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    string json = reader.ReadToEnd();
                    return json.Contains(ModelName.Split(':')[0]);
                }
            }
            catch
            {
                return false;
            }
        }

        private void PullModel()
        {
            try
            {
                Console.WriteLine($"[AI] Downloading {ModelName}... (this may take a few minutes)");
                
                var request = (HttpWebRequest)WebRequest.Create($"{OllamaUrl}/api/pull");
                request.Method = "POST";
                request.ContentType = "application/json";
                request.Timeout = 600000; 

                string json = "{\"name\":\"" + ModelName + "\",\"stream\":false}";
                byte[] data = Encoding.UTF8.GetBytes(json);
                request.ContentLength = data.Length;

                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }

                using (var response = (HttpWebResponse)request.GetResponse())
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    string result = reader.ReadToEnd();
                    if (result.Contains("\"status\":\"success\"") || result.Contains("success"))
                    {
                        Console.WriteLine("[AI] Model download complete!");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AI] Failed to pull model via API: {ex.Message}");
            }
        }

        public void ResetConversation()
        {
            Console.WriteLine("[AI] Conversation reset.");
        }

        public bool IsModelAvailable()
        {
            try
            {
                var request = (HttpWebRequest)WebRequest.Create($"{OllamaUrl}/api/tags");
                request.Method = "GET";
                request.Timeout = 3000;
                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    return response.StatusCode == HttpStatusCode.OK;
                }
            }
            catch
            {
                return false;
            }
        }

        private string EscapeJson(string str)
        {
            if (string.IsNullOrEmpty(str)) return "";
            
            StringBuilder sb = new StringBuilder();
            foreach (char c in str)
            {
                if (c == '\0') continue;
                
                switch (c)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (char.IsControl(c)) { }
                        else { sb.Append(c); }
                        break;
                }
            }
            return sb.ToString();
        }

        private string ExtractJsonField(string json, string fieldName)
        {
            string searchPattern = "\"" + fieldName + "\":\"";
            int startIndex = json.IndexOf(searchPattern);
            if (startIndex == -1) return null;

            startIndex += searchPattern.Length;
            int endIndex = startIndex;

            while (endIndex < json.Length)
            {
                if (json[endIndex] == '"' && json[endIndex - 1] != '\\')
                    break;
                endIndex++;
            }

            string value = json.Substring(startIndex, endIndex - startIndex);
            value = value.Replace("\\n", "\n").Replace("\\\"", "\"").Replace("\\\\", "\\");
            return value;
        }

        public string ModelPath => $"{OllamaUrl} (model: {ModelName})";
    }
}
