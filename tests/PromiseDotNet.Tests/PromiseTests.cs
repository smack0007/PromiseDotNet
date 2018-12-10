using System;
using System.Threading;
using Xunit;

namespace PromiseDotNet.Tests
{
    public class PromiseTests
    {
        private void WaitForPromise<TValue>(Promise<TValue> promise)
        {
            while (promise.State == PromiseState.Pending)
                Thread.Sleep(10);
        }

        [Fact]
        public void ResolveProducesFulfilledPromise()
        {
            int resolvedValue = 0;

            WaitForPromise(
                Promise<int>.Resolve(42).Then(
                    x => resolvedValue = x,
                    x => { }
                )
            );

            Assert.Equal(42, resolvedValue);
        }

        [Fact]
        public void RejectProducesRejectedPromise()
        {
            Exception expected = new PromiseException();
            Exception actual = null;

            WaitForPromise(
                Promise<int>.Reject(expected).Then(
                    x => { },
                    x => actual = x
                )
            );

            Assert.Same(expected, actual);
        }

        [Fact]
        public void ThenOnRejectCallbackProducesFulfilledPromise()
        {
            int expected = 42;
            int actual = -1;

            WaitForPromise(
                Promise<int>.Reject(new PromiseException())
                    .Then(
                        x => 0,
                        x => expected
                    )
                    .Then(
                        x => actual = x,
                        x => 0
                    )
            );

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void NonChainedThenCallsShouldAllReceiveTheSameValueForFulfilled()
        {
            int value = 0;

            var promise = Promise<int>.Resolve(42);

            promise.Then(x => x * 2);

            promise.Then(x => x * 2);

            WaitForPromise(promise.Then(x => value = x));

            Assert.Equal(42, value);
        }

        [Fact]
        public void ChainedThenCallsShouldReceiveNewValueForFulfilled()
        {
            int value = 0;

            WaitForPromise(
                Promise<int>.Resolve(42)
                    .Then(x => x * 2)
                    .Then(x => x * 2)
                    .Then(x => value = x)
            );

            Assert.Equal(168, value);
        }

        [Fact]
        public void ThenCanReturnPromiseInFulfilled()
        {
            int value = 0;

            WaitForPromise(
                Promise<int>.Resolve(42)
                    .Then(x => new Promise<int>((resolve) => resolve(x * 2)))
                    .Then(x => x * 2)
                    .Then(x => value = x)
            );

            Assert.Equal(168, value);
        }
    }
}
