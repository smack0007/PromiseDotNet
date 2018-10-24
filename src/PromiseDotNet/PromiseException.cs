using System;

namespace PromiseDotNet
{
    public class PromiseException<TReason> : Exception
    {
        public TReason Reason { get; }

        public PromiseException(TReason reason)
        {
            Reason = reason;
        }
    }
}
