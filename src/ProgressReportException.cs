using System;

namespace RSG
{
    #if NET35
        [System.Serializable]
    #endif
    public class ProgressReportException : PromiseException
    {
        public ProgressReportException() { }

        public ProgressReportException(string message) : base(message) { }
        public ProgressReportException(string message, Exception innerException) : base(message, innerException) { }
#if NET35
        public ProgressReportException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
#endif
    }
}