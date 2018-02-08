using System;

namespace RSG
{
#if NET35
    [System.Serializable]
#endif
    public abstract class PromiseException : Exception
    {
        public PromiseException() { }

        public PromiseException(string message) : base(message) { }

        public PromiseException(string message, Exception innerException) : base(message, innerException) { }
#if NET35
        public PromiseException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
#endif
    }
}