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
        public void ResolveProducesFulfilledPromise()
        {
            bool wasFulfilled = false;

            WaitForPromise(
                Promise.Resolve().Then(
                    () => wasFulfilled = true,
                    ex => { }
                )
            );

            Assert.True(wasFulfilled);
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
        public void ThenOnRejectCallbackProducesFulfilledPromise()
        {
            Exception expected = new PromiseException();
            Exception actual = null;
            bool wasFulfilled = false;

            WaitForPromise(
                Promise.Reject(expected)
                    .Then(
                        () => { },
                        ex => actual = ex
                    )
                    .Then(
                        () => wasFulfilled = true,
                        ex => { }
                    )
            );

            Assert.Equal(expected, actual);
            Assert.True(wasFulfilled);
        }

        [Fact]
        public void ThenCanReturnPromiseInFulfilled()
        {
            bool wasFulfilled = false;

            WaitForPromise(
                Promise.Resolve()
                    .Then(() => new Promise((resolve) => resolve()))
                    .Then(() => wasFulfilled = true)
            );

            Assert.True(wasFulfilled);
        }
    }
}
