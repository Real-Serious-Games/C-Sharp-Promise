using System;
using System.Runtime.Serialization;

namespace RSG
{
    [Serializable]
    public class ProgressReportException : PromiseException
    {
        public ProgressReportException() { }

        public ProgressReportException(string message) : base(message) { }

        public ProgressReportException(string message, Exception innerException) : base(message, innerException) { }

        protected ProgressReportException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}