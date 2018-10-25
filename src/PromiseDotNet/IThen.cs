using System;

namespace PromiseDotNet
{
    public interface IThen<TValue, TReason>
    {
        IThen<TThenValue, TThenReason> Then<TThenValue, TThenReason>(
            Func<TValue, TThenValue> onFulfilled,
            Func<TReason, TThenReason> onRejected);
    }
}
