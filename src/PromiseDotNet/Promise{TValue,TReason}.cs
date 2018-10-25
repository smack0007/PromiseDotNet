using System;
using System.Threading.Tasks;

namespace PromiseDotNet
{
    public class Promise<TValue, TReason> : IThen<TValue, TReason>
    {
        public static readonly Func<TValue, TValue> Identity = x => x;
        public static readonly Func<TReason, TReason> Thrower = x => throw new PromiseException<TReason>(x);

        private Task _task;
        private TValue _value;
        private TReason _reason;
        private Exception _exception;

        public PromiseState State { get; private set; } = PromiseState.Pending;

        public Promise(Action<Action<TValue>, Action<TReason>> executor)
        {
            if (executor == null)
                throw new ArgumentNullException(nameof(executor));

            _task = Task.Run(() =>
            {
                void resolve(TValue x)
                {
                    _value = x;
                    State = PromiseState.Fulfilled;
                }

                void reject(TReason x)
                {
                    _reason = x;
                    State = PromiseState.Rejected;
                }

                try
                {
                    executor(resolve, reject);                  
                }
                catch (Exception ex)
                {
                    _exception = ex;
                    State = PromiseState.Rejected;
                }
            });
        }

        private Promise(
            PromiseState state,
            TValue value = default,
            TReason reason = default,
            Exception exception = null)
        {
            _task = Task.CompletedTask;
            State = state;
            _value = value;
            _reason = reason;
            _exception = exception;
        }

        public static Promise<TValue, TReason> Resolve(TValue value)
        {
            return new Promise<TValue, TReason>(PromiseState.Fulfilled, value: value);
        }

        public static Promise<TValue, TReason> Reject(TReason reason)
        {
            return new Promise<TValue, TReason>(PromiseState.Rejected, reason: reason);
        }

        public IThen<TThenValue, TThenReason> Then<TThenValue, TThenReason>(
            Func<TValue, TThenValue> onFulfilled,
            Func<TReason, TThenReason> onRejected)
        {
            return new Promise<TThenValue, TThenReason>((resolve, reject) =>
            {
                _task.Wait();
                if (State == PromiseState.Fulfilled)
                {
                    resolve(onFulfilled(_value));
                }
                else
                {
                    reject(onRejected(_reason));
                }
            });
        }
    }
}
