using System;

namespace PromiseDotNet
{
    public static class IThenExtensions
    {
        public static IThen<TValue, TReason> Then<TValue, TReason>(
            this IThen<TValue, TReason> then,
            Action<TValue> onFulfilled)
        {
            Func<TValue, TValue> onFullfilledWrapper = null;

            if (onFulfilled != null)
            {
                onFullfilledWrapper = x =>
                {
                    onFulfilled(x);
                    return x;
                };
            }

            return then.Then(onFullfilledWrapper, Promise<TValue, TReason>.Thrower);
        }

        public static IThen<TValue, TReason> Then<TValue, TReason>(
            this IThen<TValue, TReason> then,
            Action<TValue> onFulfilled,
            Action<TReason> onRejected)
        {
            Func<TValue, TValue> onFullfilledWrapper = null;
            Func<TReason, TReason> onRejectedWrapper = null;

            if (onFulfilled != null)
            {
                onFullfilledWrapper = x =>
                {
                    onFulfilled(x);
                    return x;
                };
            }

            if (onRejected != null)
            {
                onRejectedWrapper = x =>
                {
                    onRejected(x);
                    return x;
                };
            }

            return then.Then(onFullfilledWrapper, onRejectedWrapper);
        }

        public static IThen<TThenValue, TReason> Then<TValue, TReason, TThenValue>(
            this IThen<TValue, TReason> then,
            Func<TValue, TThenValue> onFulfilled)
        {
            return then.Then(onFulfilled, Promise<TValue, TReason>.Thrower);
        }
    }
}
