using System;
using PluginFramework.Interfaces;

namespace LoggerExample {

    public class ConsoleLogger : MarshalByRefObject, ILogger
    {
        int _count = 0;

        public void Log(string text){
            Console.Write("2 Log:" + text + "["+_count++ +"]\n");
        }
        public void LogWarning(string text){
            Console.Write("Warning: " + text + "[" + _count++ + "]\n");
        }

        public void LogError(string text){
            Console.Write("Error: " + text + "[" + _count++ + "]\n");
        }
    }
}