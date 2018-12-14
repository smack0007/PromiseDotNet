using System;
using System.Threading;
using Xunit;

namespace PromiseDotNet.Tests
{
    public class PromiseTests
    {
        private void WaitForPromise(Promise promise)
        {
            while (promise.State == PromiseState.Pending)
                Thread.Sleep(10);
        }

        [Fact]
        public void NotCallingResolveOrRejectInExecutorCausesRejection()
        {
            Exception actual = null;

            WaitForPromise(
                new Promise((resolve, reject) => { })
                    .Then(
                        () => { },
                        ex => actual = ex
                    )
            );

            Assert.Equal(PromiseException.NotSettled, actual);
        }

        [Fact]
        public void ResolveProducesResolvedPromise()
        {
            bool wasResolved = false;

            WaitForPromise(
                Promise.Resolve().Then(
                    () => wasResolved = true,
                    ex => { }
                )
            );

            Assert.True(wasResolved);
        }

        [Fact]
        public void RejectProducesRejectedPromise()
        {
            Exception expected = new PromiseException();
            Exception actual = null;

            WaitForPromise(
                Promise.Reject(expected).Then(
                    () => { },
                    ex => actual = ex
                )
            );

            Assert.Same(expected, actual);
        }

        [Fact]
        public void ThenOnRejectCallbackProducesResolvedPromise()
        {
            Exception expected = new PromiseException();
            Exception actual = null;
            bool wasResolved = false;

            WaitForPromise(
                Promise.Reject(expected)
                    .Then(
                        () => { },
                        ex => actual = ex
                    )
                    .Then(
                        () => wasResolved = true,
                        ex => { }
                    )
            );

            Assert.Equal(expected, actual);
            Assert.True(wasResolved);
        }

        [Fact]
        public void ThenCanReturnPromiseInResolved()
        {
            bool wasResolved = false;

            WaitForPromise(
                Promise.Resolve()
                    .Then(() => new Promise((resolve) => resolve()))
                    .Then(() => wasResolved = true)
            );

            Assert.True(wasResolved);
        }

        [Fact]
        public void CatchCalledWhenRejected()
        {
            Exception expected = new PromiseException();
            Exception actual = null;

            WaitForPromise(
                Promise.Reject(expected)
                    .Catch(ex => actual = ex)
            );

            Assert.Same(expected, actual);
        }

        [Fact]
        public void CatchNotCalledWhenResolved()
        {
            Exception actual = null;

            WaitForPromise(
                Promise.Resolve()
                    .Catch(ex => actual = ex)
            );

            Assert.Null(actual);
        }

        [Fact]
        public void FinallyCalledWhenResolved()
        {
            bool finallyCalled = false;

            WaitForPromise(
                Promise.Resolve()
                    .Then(() => Console.WriteLine("Then"))
                    .Finally(() => finallyCalled = true)
            );

            Assert.True(finallyCalled);
        }

        [Fact]
        public void FinallyCalledWhenRejected()
        {
            bool finallyCalled = false;

            WaitForPromise(
                Promise.Reject()
                    .Catch(ex => Console.WriteLine("Catch"))
                    .Finally(() => finallyCalled = true)
            );

            Assert.True(finallyCalled);
        }

        [Fact]
        public void AllResolvesWhenAllPromisesResolve()
        {
            bool resolved = false;
            
            WaitForPromise(
                Promise.All(
                    Promise.Resolve(),
                    Promise.Resolve(),
                    new Promise((resolve) =>
                    {
                        Thread.Sleep(100);
                        resolve();
                    })
                )
                .Then(() => resolved = true)
            );

            Assert.True(resolved);
        }

        [Fact]
        public void AllRejectsWhenOnePromisesRejects()
        {
            bool rejected = false;

            WaitForPromise(
                Promise.All(
                    new Promise((resolve, reject) =>
                    {
                        Thread.Sleep(10);
                        reject(PromiseException.Default);
                    }),
                    Promise.Resolve(),
                    Promise.Resolve()
                )
                .Catch(() => rejected = true)
            );

            Assert.True(rejected);
        }

        [Fact]
        public void RaceResolvesWhenFirstPromiseResolves()
        {
            bool resolved = false;

            WaitForPromise(
                Promise.Race(
                    new Promise((resolve, reject) =>
                    {
                        Thread.Sleep(10);
                        resolve();
                    }),
                    new Promise((resolve, reject) =>
                    {
                        Thread.Sleep(500);
                        reject(PromiseException.Default);
                    }),
                    new Promise((resolve, reject) =>
                    {
                        Thread.Sleep(500);
                        reject(PromiseException.Default);
                    })
                )
                .Then(() => resolved = true)
            );

            Assert.True(resolved);
        }

        [Fact]
        public void RaceRejectsWhenFirstPromiseRejects()
        {
            var expected = new PromiseException("Rejected.");
            Exception actual = null;

            WaitForPromise(
                Promise.Race(
                    new Promise((resolve, reject) =>
                    {
                        Thread.Sleep(10);
                        reject(expected);
                    }),
                    new Promise((resolve, reject) =>
                    {
                        Thread.Sleep(500);
                        reject(PromiseException.Default);
                    }),
                    new Promise((resolve, reject) =>
                    {
                        Thread.Sleep(500);
                        reject(PromiseException.Default);
                    })
                )
                .Catch(ex => actual = ex)
            );

            Assert.Same(expected, actual);
        }
    }
}
