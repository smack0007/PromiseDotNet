using System;
using System.Threading.Tasks;

namespace PromiseDotNet
{
    public class Promise<TValue>
    {
        public static readonly Func<TValue, TValue> Identity = x => x;
        public static readonly Func<Exception, TValue> Thrower = x => throw new PromiseException(x);

        private Task _task;
        private TValue _value;
        private Exception _exception;

        public PromiseState State { get; private set; } = PromiseState.Pending;

        public Promise(Func<TValue> executor)
        {
            if (executor == null)
                throw new ArgumentNullException(nameof(executor));

            _task = Task.Run(() =>
            {
                try
                {
                    _value = executor();
                    State = PromiseState.Fulfilled;
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
            Exception exception = null)
        {
            _task = Task.CompletedTask;
            State = state;
            _value = value;
            _exception = exception;
        }

        public static Promise<TValue> Resolve(TValue value)
        {
            return new Promise<TValue>(PromiseState.Fulfilled, value: value);
        }

        public static Promise<TValue> Reject()
        {
            return new Promise<TValue>(PromiseState.Rejected, exception: PromiseException.Default);
        }

        public static Promise<TValue> Reject(Exception ex)
        {
            return new Promise<TValue>(PromiseState.Rejected, exception: ex);
        }

        public Promise<TValue> Then(Action<TValue> onFulfilled)
        {
            if (onFulfilled == null)
                throw new ArgumentNullException(nameof(onFulfilled));

            Func<TValue, TValue> onFullfilledWrapper = x =>
            {
                onFulfilled(x);
                return x;
            };

            return Then(onFullfilledWrapper, Thrower);
        }

        public Promise<TValue> Then(
            Action<TValue> onFulfilled,
            Action<Exception> onRejected)
        {
            if (onFulfilled == null)
                throw new ArgumentNullException(nameof(onFulfilled));

            if (onRejected == null)
                throw new ArgumentNullException(nameof(onRejected));

            TValue onFullfilledWrapper(TValue x)
            {
                onFulfilled(x);
                return x;
            }

            TValue onRejectedWrapper(Exception x)
            {
                onRejected(x);
                return default;
            }

            return Then(onFullfilledWrapper, onRejectedWrapper);
        }

        public Promise<TThenValue> Then<TThenValue>(
            Func<TValue, TThenValue> onFulfilled)
        {
            return Then(onFulfilled, Promise<TThenValue>.Thrower);
        }

        public Promise<TThenValue> Then<TThenValue>(
            Func<TValue, TThenValue> onFulfilled,
            Func<Exception, TThenValue> onRejected)
        {
            if (onFulfilled == null)
                throw new ArgumentNullException(nameof(onFulfilled));

            if (onRejected == null)
                throw new ArgumentNullException(nameof(onRejected));

            return new Promise<TThenValue>(() =>
            {
                _task.Wait();

                if (State == PromiseState.Fulfilled)
                {
                    return onFulfilled(_value);
                }
                else
                {
                    return onRejected(_exception);
                }
            });
        }

        public Promise<TThenValue> Then<TThenValue>(
            Func<TValue, Promise<TThenValue>> onFulfilled)
        {
            return Then(onFulfilled, x => Promise<TThenValue>.Reject(x));
        }

        public Promise<TThenValue> Then<TThenValue>(
            Func<TValue, Promise<TThenValue>> onFulfilled,
            Func<Exception, Promise<TThenValue>> onRejected)
        {
            if (onFulfilled == null)
                throw new ArgumentNullException(nameof(onFulfilled));

            if (onRejected == null)
                throw new ArgumentNullException(nameof(onRejected));

            return new Promise<TThenValue>(() =>
            {
                _task.Wait();

                Promise<TThenValue> promise = null;

                if (State == PromiseState.Fulfilled)
                    promise = onFulfilled(_value);
                else
                    promise = onRejected(_exception);

                promise._task.Wait();

                //if (promise.State == PromiseState.Fulfilled)
                //    return promise._reason;

                return promise._value;
            });
        }
    }
}
