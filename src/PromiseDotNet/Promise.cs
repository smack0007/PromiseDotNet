﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PromiseDotNet
{
    public sealed class Promise
    {
        public static readonly Action Empty = () => { };
        public static readonly Action Identity = Empty;
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
            Exception exception = null)
        {
            _task = Task.CompletedTask;
            State = state;
            _exception = exception;
        }

        public static Promise All(params Promise[] promises)
        {
            return new Promise((resolve, reject) => {
                Promise rejected = promises.FirstOrDefault(x => x.State == PromiseState.Rejected);

                if (rejected == null)
                {
                    while (true)
                    {
                        var whenAnyTask = Task.WhenAny(
                            promises
                                .Where(x => x._task != Task.CompletedTask)
                                .Select(x => x._task)
                        );

                        whenAnyTask.Wait();

                        var finishedPromise = promises.Single(x => x._task == whenAnyTask.Result);
                        
                        if (finishedPromise.State == PromiseState.Rejected)
                        {
                            rejected = finishedPromise;
                            break;
                        }

                        if (!promises.Any(x => x.State == PromiseState.Pending))
                            break;
                    }
                }

                if (rejected == null)
                {
                    resolve();
                }
                else
                {
                    reject(rejected._exception);
                }
            });
        }

        public static Promise Race(params Promise[] promises)
        {
            return new Promise((resolve, reject) => {
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
                    resolve();
                }
                else
                {
                    reject(winner._exception);
                }
            });
        }

        public static Promise Resolve()
        {
            return new Promise(PromiseState.Resolved);
        }

        public static Promise Reject()
        {
            return new Promise(PromiseState.Rejected, exception: PromiseException.Default);
        }

        public static Promise Reject(Exception ex)
        {
            return new Promise(PromiseState.Rejected, exception: ex);
        }

        public Promise Then(Action onResolved)
        {
            return Then(onResolved, Thrower);
        }

        public Promise Then(Action onResolved, Action<Exception> onRejected)
        {
            if (onResolved == null)
                throw new ArgumentNullException(nameof(onResolved));

            if (onRejected == null)
                throw new ArgumentNullException(nameof(onRejected));

            return new Promise((resolve, reject) =>
            {
                _task.Wait();

                try
                {
                    if (State == PromiseState.Resolved)
                    {
                        onResolved();
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

        public Promise Then(Func<Promise> onResolved)
        {
            return Then(onResolved, x => Reject(x));
        }

        public Promise Then(Func<Promise> onResolved, Func<Exception, Promise> onRejected)
        {
            if (onResolved == null)
                throw new ArgumentNullException(nameof(onResolved));

            if (onRejected == null)
                throw new ArgumentNullException(nameof(onRejected));

            return new Promise((resolve, reject) =>
            {
                _task.Wait();

                Promise promise = null;

                if (State == PromiseState.Resolved)
                    promise = onResolved();
                else
                    promise = onRejected(_exception);

                promise._task.Wait();

                if (promise.State == PromiseState.Resolved)
                {
                    resolve();
                }
                else
                {
                    reject(promise._exception);
                }
            });
        }

        public Promise Catch(Action onRejected) =>
            Then(Empty, ex => onRejected());

        public Promise Catch(Action<Exception> onRejected) =>
            Then(Empty, onRejected);

        public Promise Finally(Action onFinally) =>
            Then(() => onFinally(), ex => onFinally());
    }
}
