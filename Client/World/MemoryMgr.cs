using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
// using Newtonsoft.Json; // Avoid dependency if possible, stick to simple parsing or native XML if .NET 3.5

namespace WotlkClient.Clients
{
    public class MemoryMgr
    {
        private string memoryFile = "memory.txt"; // Simple Key=Value format to avoid JSON lib dependency complexity
        private Dictionary<string, string> memories = new Dictionary<string, string>();
        
        public MemoryMgr()
        {
            Load();
        }

        public void Load()
        {
            try
            {
                if (File.Exists(memoryFile))
                {
                    string[] lines = File.ReadAllLines(memoryFile);
                    foreach(var line in lines)
                    {
                        if (line.Contains("="))
                        {
                            var parts = line.Split(new char[] { '=' }, 2);
                            if (parts.Length == 2)
                            {
                                memories[parts[0].Trim()] = parts[1].Trim();
                            }
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("[Memory] Load failed: " + ex.Message);
            }
        }

        public void Save()
        {
            try
            {
                List<string> lines = new List<string>();
                foreach(var kvp in memories)
                {
                    lines.Add($"{kvp.Key}={kvp.Value}");
                }
                File.WriteAllLines(memoryFile, lines.ToArray());
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Memory] Save failed: " + ex.Message);
            }
        }

        public void SetMemory(string key, string value)
        {
            memories[key] = value;
            Save();
        }

        public string GetMemory(string key)
        {
            if (memories.ContainsKey(key)) return memories[key];
            return null;
        }

        public string GetContextSummary()
        {
            if (memories.Count == 0) return "";
            
            StringBuilder sb = new StringBuilder();
            sb.Append("Souvenirs pertinents : ");
            foreach(var kvp in memories)
            {
                sb.Append($"[{kvp.Key}: {kvp.Value}] ");
            }
            return sb.ToString();
        }
    }
}
