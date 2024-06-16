using System;
using System.Collections.Generic;
using System.Text;

namespace TcpHex
{
    public interface ITcpHexLogger
    {
        void LogTrace(string message);
        void LogDebug(string message);
        void LogInfo(string message);
        void LogError(string message, Exception ex);
    }

    public class TcpHexLogger : ITcpHexLogger
    {
        public void LogError(string message, Exception ex)
        {
            Console.WriteLine($"{DateTime.Now} - {message}");
            Console.WriteLine($"{DateTime.Now} - {ex}");
        }

        public void LogInfo(string message)
        {
            Console.WriteLine($"{DateTime.Now} - {message}");
        }

        public void LogDebug(string message)
        {
            Console.WriteLine($"{DateTime.Now} - {message}");
        }

        public void LogTrace(string message)
        {
            Console.WriteLine($"{DateTime.Now} - {message}");
        }
    }
}
