using System;
using System.IO;

namespace WotlkClient.Shared
{
    public static class Log
    {
        private static object _lockObj = new object();

        public static void WriteLine(LogType type, string format, string prefix, params object[] parameters)
        {
            lock (_lockObj)
            {
                try
                {
                    format = string.Format("[{0}][{1}]{2}", Time.GetTime(), type, format);
                    
                    string msg = string.Format(format, parameters);

                    if (Config.LogToFile)
                    {
                        string suffix = "";
                        switch (type)
                        {
                            case LogType.Packet: suffix = "_packet_log.txt"; break;
                            case LogType.Network: suffix = "_network_log.txt"; break;
                            case LogType.Error: suffix = "_error_log.txt"; break;
                            case LogType.Debug: suffix = "_debug_log.txt"; break;
                            case LogType.Chat: suffix = "_chat_log.txt"; break;
                            case LogType.Normal: suffix = "_normal_log.txt"; break;
                        }

                        if (!string.IsNullOrEmpty(suffix))
                        {
                            try
                            {
                                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                                string logDir = Path.Combine(baseDir, "logs");
                                if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);

                                string path = Path.Combine(logDir, prefix + suffix);
                                using (StreamWriter packetFile = File.AppendText(path))
                                {
                                    packetFile.WriteLine(msg);
                                    packetFile.Flush();
                                }
                            }
                            catch { } // Prevent crash during logging
                        }

                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    Console.WriteLine(ex.StackTrace);
                    Console.WriteLine(format + ", " + parameters.Length);
                }
            }
        }
    }


    public enum LogType : long
    {
        Command = 0x1000000000000000,
        Normal = 0x0100000000000000,
        Success = 0x0010000000000000,
        Error = 0x0001000000000000,
        Debug = 0x0000100000000000,
        Test = 0x0000010000000000,
        Chat = 0x0000001000000000,
        Terrain = 0x0000000100000000,
        Network = 0x0000000010000000,
        Packet = 0x0000000001000000,
    }

}
