using System;
using System.Runtime.Serialization;

namespace RSG
{
    public abstract class PromiseException : Exception
    {
        public PromiseException() { }

        public PromiseException(string message) : base(message) { }

        public PromiseException(string message, Exception innerException) : base(message, innerException) { }

        public PromiseException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}