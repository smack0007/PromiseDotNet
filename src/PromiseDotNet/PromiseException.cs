using System;

namespace PromiseDotNet
{
    public class PromiseException : Exception
    {
        public static readonly PromiseException Default = new PromiseException();

        public PromiseException()
        {
        }

        public PromiseException(string message)
            : base(message)
        {
        }

        public PromiseException(Exception innerException)
            : base(innerException.Message, innerException)
        {
        }
    }
}
