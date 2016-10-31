namespace RSG
{
    public class PromiseStateException : PromiseException
    {
        public PromiseStateException() { }
        public PromiseStateException(string message) : base(message) { }
        public PromiseStateException(string message, System.Exception inner) : base(message, inner) { }
    }
}