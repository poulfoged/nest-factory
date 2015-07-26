using System;

namespace NestClientFactory
{
    public abstract class ClientFactoryException : Exception
    {
        protected ClientFactoryException(string message, Exception exception) : base(message, exception) { }

        protected ClientFactoryException(string message) : base(message) { }
    }

    public class UnableToProbeException : ClientFactoryException
    {
        public UnableToProbeException(string message, Exception exception) : base(message, exception) { }
    }

    public class UnableToExecuteActionException : ClientFactoryException
    {
        public UnableToExecuteActionException(string message, Exception exception) : base(message, exception) { }

        public UnableToExecuteActionException(string message) : base(message) { }
    }
}