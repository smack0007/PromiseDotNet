using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PromiseDotNet
{
    public class Promise<TValue, TReason>
    {
        private static readonly Func<TValue, TValue> Identity = x => x;
        private static readonly Func<TReason, TReason> Thrower = x => throw new PromiseException<TReason>(x);

        private object _lock = new object();
        private TValue _value = default;
        private TReason _reason = default;
        private Exception _exception = null;
        private Queue<Func<TValue, TValue>> _onFulfilledQueue;
        private Queue<Func<TReason, TReason>> _onRejectedQueue;

        public PromiseState State { get; private set; } = PromiseState.Pending;

        public Promise(Action<Action<TValue>, Action<TReason>> executor)
        {
            if (executor == null)
                throw new ArgumentNullException(nameof(executor));

            Task.Run(() =>
            {
                bool fulfilled = false;
                TValue value = default;
                TReason reason = default;
                Exception exception = null;

                void resolve(TValue x)
                {
                    value = x;
                    fulfilled = true;
                }

                void reject(TReason x)
                {
                    reason = x;
                }

                try
                {
                    executor(resolve, reject);                  
                }
                catch (Exception ex)
                {
                    exception = ex;         
                }

                lock (_lock)
                {
                    State = fulfilled ? PromiseState.Fulfilled : PromiseState.Rejected;
                    _value = value;
                    _exception = exception;

                    if (fulfilled)
                    {
                        if (_onFulfilledQueue != null)
                        {
                            while (_onFulfilledQueue.Count > 0)
                                _onFulfilledQueue.Dequeue().Invoke(value);
                        }
                    }
                    else
                    {
                        if (_onRejectedQueue != null)
                        {
                            while (_onRejectedQueue.Count > 0)
                                _onRejectedQueue.Dequeue().Invoke(reason);                            
                        }
                    }

                    _onFulfilledQueue = null;
                    _onRejectedQueue = null;
                }
            });
        }

        public Promise<TValue, TReason> Then(Action<TValue> onFulfilled = null, Action<TReason> onRejected = null)
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

            return Then(onFullfilledWrapper, onRejectedWrapper);
        }

        public Promise<TValue, TReason> Then(Func<TValue, TValue> onFulfilled = null, Func<TReason, TReason> onRejected = null)
        {
            lock (_lock)
            {
                if (State != PromiseState.Pending)
                {
                    if (State == PromiseState.Fulfilled)
                    {
                        onFulfilled?.Invoke(_value);
                    }
                    else
                    {
                        onRejected?.Invoke(_reason);
                    }
                }
                else
                {
                    if (onFulfilled != null)
                    {
                        if (_onFulfilledQueue == null)
                            _onFulfilledQueue = new Queue<Func<TValue, TValue>>();

                        _onFulfilledQueue.Enqueue(onFulfilled);
                    }

                    if (onRejected != null)
                    {
                        if (_onRejectedQueue == null)
                            _onRejectedQueue = new Queue<Func<TReason, TReason>>();

                        _onRejectedQueue.Enqueue(onRejected);
                    }
                }
            }

            return this;
        }
    }
}
