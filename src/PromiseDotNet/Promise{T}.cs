using System;
using System.Threading.Tasks;

namespace PromiseDotNet
{
    public sealed class Promise<T>
    {
        public static readonly Action<T> Empty = x => { };
        public static readonly Func<T, T> Identity = x => x;
        public static readonly Func<Exception, T> Thrower = x => throw new PromiseException(x);

        private Task _task;
        private T _value;
        private Exception _exception;
         
        public PromiseState State { get; private set; } = PromiseState.Pending;

        public Promise(Action<Action<T>> executor)
            : this((resolve, reject) => executor(resolve))
        {
        }

        public Promise(Action<Action<T>, Action<Exception>> executor)
        {
            if (executor == null)
                throw new ArgumentNullException(nameof(executor));

            void resolve(T value)
            {
                _value = value;
                State = PromiseState.Fulfilled;
            }

            void reject(Exception ex)
            {
                _exception = ex;
                State = PromiseState.Rejected;
            }

            _task = Task.Run(() =>
            {
                try
                {
                    executor(resolve, reject);
                }
                catch (Exception ex)
                {
                    reject(ex);
                }

                if (State == PromiseState.Pending)
                    reject(PromiseException.NotSettled);
            });
        }

        private Promise(
            PromiseState state,
            T value = default,
            Exception exception = null)
        {
            _task = Task.CompletedTask;
            State = state;
            _value = value;
            _exception = exception;
        }

        public static Promise<T> Resolve(T value)
        {
            return new Promise<T>(PromiseState.Fulfilled, value: value);
        }

        public static Promise<T> Reject()
        {
            return new Promise<T>(PromiseState.Rejected, exception: PromiseException.Default);
        }

        public static Promise<T> Reject(Exception ex)
        {
            return new Promise<T>(PromiseState.Rejected, exception: ex);
        }

        public Promise<T> Then(Action<T> onFulfilled)
        {
            if (onFulfilled == null)
                throw new ArgumentNullException(nameof(onFulfilled));

            Func<T, T> onFullfilledWrapper = x =>
            {
                onFulfilled(x);
                return x;
            };

            return Then(onFullfilledWrapper, Thrower);
        }

        public Promise<T> Then(
            Action<T> onFulfilled,
            Action<Exception> onRejected)
        {
            if (onFulfilled == null)
                throw new ArgumentNullException(nameof(onFulfilled));

            if (onRejected == null)
                throw new ArgumentNullException(nameof(onRejected));

            T onFullfilledWrapper(T x)
            {
                onFulfilled(x);
                return x;
            }

            T onRejectedWrapper(Exception x)
            {
                onRejected(x);
                return default;
            }

            return Then(onFullfilledWrapper, onRejectedWrapper);
        }

        public Promise<TThenValue> Then<TThenValue>(
            Func<T, TThenValue> onFulfilled)
        {
            return Then(onFulfilled, Promise<TThenValue>.Thrower);
        }

        public Promise<TThenValue> Then<TThenValue>(
            Func<T, TThenValue> onFulfilled,
            Func<Exception, TThenValue> onRejected)
        {
            if (onFulfilled == null)
                throw new ArgumentNullException(nameof(onFulfilled));

            if (onRejected == null)
                throw new ArgumentNullException(nameof(onRejected));

            return new Promise<TThenValue>((resolve, reject) =>
            {
                _task.Wait();

                try
                {
                    if (State == PromiseState.Fulfilled)
                    {
                        resolve(onFulfilled(_value));
                    }
                    else
                    {
                        resolve(onRejected(_exception));
                    }
                }
                catch (Exception ex)
                {
                    reject(ex);
                }
            });
        }

        public Promise<TThenValue> Then<TThenValue>(
            Func<T, Promise<TThenValue>> onFulfilled)
        {
            return Then(onFulfilled, x => Promise<TThenValue>.Reject(x));
        }

        public Promise<TThenValue> Then<TThenValue>(
            Func<T, Promise<TThenValue>> onFulfilled,
            Func<Exception, Promise<TThenValue>> onRejected)
        {
            if (onFulfilled == null)
                throw new ArgumentNullException(nameof(onFulfilled));

            if (onRejected == null)
                throw new ArgumentNullException(nameof(onRejected));

            return new Promise<TThenValue>((resolve, reject) =>
            {
                _task.Wait();

                Promise<TThenValue> promise = null;

                if (State == PromiseState.Fulfilled)
                    promise = onFulfilled(_value);
                else
                    promise = onRejected(_exception);

                promise._task.Wait();

                if (promise.State == PromiseState.Fulfilled)
                {
                    resolve(promise._value);
                }
                else
                {
                    reject(promise._exception);
                }
            });
        }

        public Promise<T> Catch(Action<Exception> onRejected) =>
            Then(Empty, onRejected);

        public Promise<TCatchValue> Catch<TCatchValue>(Func<Exception, TCatchValue> onRejected) =>
            Then(x => default, onRejected);
    }
}
