using System;

namespace PSOTLP.Logging
{
    public interface IOTLPLogger
    {
        void Info(string component, string message);
        void Warning(string component, string message);
        void Error(string component, string message);
        void Error(string component, string message, Exception exception);
    }
}
