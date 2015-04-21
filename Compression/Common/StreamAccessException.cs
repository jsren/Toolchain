using System.Runtime.Serialization;

namespace System.IO.Compression
{
    [Serializable]
    public class StreamAccessException : Exception
    {
        public StreamAccessException() { }
        public StreamAccessException(string message) : base(message) { }

        protected StreamAccessException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }
}
