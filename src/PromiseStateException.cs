namespace RSG
{
#if NET35
    [System.Serializable]
#endif
    public class PromiseStateException : PromiseException
    {
        public PromiseStateException() { }
        public PromiseStateException(string message) : base(message) { }
        public PromiseStateException(string message, System.Exception inner) : base(message, inner) { }
#if NET35
        public PromiseStateException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
#endif
    }
}