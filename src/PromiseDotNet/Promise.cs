using System;
using System.Threading.Tasks;

namespace PromiseDotNet
{
    public sealed class Promise
    {
        public static readonly Action Identity = () => { };
        public static readonly Action<Exception> Thrower = x => throw new PromiseException(x);

        private Task _task;
        private Exception _exception;

        public PromiseState State { get; private set; } = PromiseState.Pending;

        public Promise(Action<Action> executor)
            : this((resolve, reject) => executor(resolve))
        {
        }

        public Promise(Action<Action, Action<Exception>> executor)
        {
            if (executor == null)
                throw new ArgumentNullException(nameof(executor));

            void resolve()
            {
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
            Exception exception = null)
        {
            _task = Task.CompletedTask;
            State = state;
            _exception = exception;
        }

        public static Promise Resolve()
        {
            return new Promise(PromiseState.Fulfilled);
        }

        public static Promise Reject()
        {
            return new Promise(PromiseState.Rejected, exception: PromiseException.Default);
        }

        public static Promise Reject(Exception ex)
        {
            return new Promise(PromiseState.Rejected, exception: ex);
        }

        public Promise Then(Action onFulfilled)
        {
            return Then(onFulfilled, Thrower);
        }

        public Promise Then(Action onFulfilled, Action<Exception> onRejected)
        {
            if (onFulfilled == null)
                throw new ArgumentNullException(nameof(onFulfilled));

            if (onRejected == null)
                throw new ArgumentNullException(nameof(onRejected));

            return new Promise((resolve, reject) =>
            {
                _task.Wait();

                try
                {
                    if (State == PromiseState.Fulfilled)
                    {
                        onFulfilled();
                        resolve();
                    }
                    else
                    {
                        onRejected(_exception);
                        resolve();
                    }
                }
                catch (Exception ex)
                {
                    reject(ex);
                }
            });
        }

        public Promise Then(Func<Promise> onFulfilled)
        {
            return Then(onFulfilled, x => Reject(x));
        }

        public Promise Then(Func<Promise> onFulfilled, Func<Exception, Promise> onRejected)
        {
            if (onFulfilled == null)
                throw new ArgumentNullException(nameof(onFulfilled));

            if (onRejected == null)
                throw new ArgumentNullException(nameof(onRejected));

            return new Promise((resolve, reject) =>
            {
                _task.Wait();

                Promise promise = null;

                if (State == PromiseState.Fulfilled)
                    promise = onFulfilled();
                else
                    promise = onRejected(_exception);

                promise._task.Wait();

                if (promise.State == PromiseState.Fulfilled)
                {
                    resolve();
                }
                else
                {
                    reject(promise._exception);
                }
            });
        }
    }
}
