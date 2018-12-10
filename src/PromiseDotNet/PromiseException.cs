using System;

namespace PromiseDotNet
{
    public class PromiseException : Exception
    {
        public static readonly PromiseException Default = new PromiseException("The default PromiseException was used.");
        public static readonly PromiseException NotSettled = new PromiseException("Neither the resolve method nor the reject method were called.");

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
