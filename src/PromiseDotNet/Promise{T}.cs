using System;
using System.Linq;
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
                State = PromiseState.Resolved;
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
            return new Promise<T>(PromiseState.Resolved, value: value);
        }

        public static Promise<T> Reject()
        {
            return new Promise<T>(PromiseState.Rejected, exception: PromiseException.Default);
        }

        public static Promise<T> Reject(Exception ex)
        {
            return new Promise<T>(PromiseState.Rejected, exception: ex);
        }

        public static Promise<T> Race(params Promise<T>[] promises)
        {
            return new Promise<T>((resolve, reject) => {
                var winner = promises.FirstOrDefault(x => x.State != PromiseState.Pending);

                if (winner == null)
                {
                    var whenAnyTask = Task.WhenAny(
                        promises
                            .Where(x => x._task != Task.CompletedTask)
                            .Select(x => x._task)
                    );

                    whenAnyTask.Wait();

                    winner = promises.Single(x => x._task == whenAnyTask.Result);
                }

                if (winner.State == PromiseState.Resolved)
                {
                    resolve(winner._value);
                }
                else
                {
                    reject(winner._exception);
                }
            });
        }

        public Promise<T> Then(Action<T> onResolved)
        {
            if (onResolved == null)
                throw new ArgumentNullException(nameof(onResolved));

            Func<T, T> onResolvedWrapper = x =>
            {
                onResolved(x);
                return x;
            };

            return Then(onResolvedWrapper, Thrower);
        }

        public Promise<T> Then(
            Action<T> onResolved,
            Action<Exception> onRejected)
        {
            if (onResolved == null)
                throw new ArgumentNullException(nameof(onResolved));

            if (onRejected == null)
                throw new ArgumentNullException(nameof(onRejected));

            T onResolvedWrapper(T x)
            {
                onResolved(x);
                return x;
            }

            T onRejectedWrapper(Exception x)
            {
                onRejected(x);
                return default;
            }

            return Then(onResolvedWrapper, onRejectedWrapper);
        }

        public Promise<TThenValue> Then<TThenValue>(
            Func<T, TThenValue> onResolved)
        {
            return Then(onResolved, Promise<TThenValue>.Thrower);
        }

        public Promise<TThenValue> Then<TThenValue>(
            Func<T, TThenValue> onResolved,
            Func<Exception, TThenValue> onRejected)
        {
            if (onResolved == null)
                throw new ArgumentNullException(nameof(onResolved));

            if (onRejected == null)
                throw new ArgumentNullException(nameof(onRejected));

            return new Promise<TThenValue>((resolve, reject) =>
            {
                _task.Wait();

                try
                {
                    if (State == PromiseState.Resolved)
                    {
                        resolve(onResolved(_value));
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
            Func<T, Promise<TThenValue>> onResolved)
        {
            return Then(onResolved, x => Promise<TThenValue>.Reject(x));
        }

        public Promise<TThenValue> Then<TThenValue>(
            Func<T, Promise<TThenValue>> onResolved,
            Func<Exception, Promise<TThenValue>> onRejected)
        {
            if (onResolved == null)
                throw new ArgumentNullException(nameof(onResolved));

            if (onRejected == null)
                throw new ArgumentNullException(nameof(onRejected));

            return new Promise<TThenValue>((resolve, reject) =>
            {
                _task.Wait();

                Promise<TThenValue> promise = null;

                if (State == PromiseState.Resolved)
                    promise = onResolved(_value);
                else
                    promise = onRejected(_exception);

                promise._task.Wait();

                if (promise.State == PromiseState.Resolved)
                {
                    resolve(promise._value);
                }
                else
                {
                    reject(promise._exception);
                }
            });
        }

        public Promise<T> Catch(Action onRejected) =>
            Then(Empty, ex => onRejected());

        public Promise<T> Catch(Action<Exception> onRejected) =>
            Then(Empty, onRejected);

        public Promise<TCatchValue> Catch<TCatchValue>(Func<TCatchValue> onRejected) =>
            Then(x => default, ex => onRejected());

        public Promise<TCatchValue> Catch<TCatchValue>(Func<Exception, TCatchValue> onRejected) =>
            Then(x => default, onRejected);

        public Promise<T> Finally(Action onFinally) =>
            Then(x => onFinally(), ex => onFinally());
    }
}
