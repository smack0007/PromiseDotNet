using System.Threading;
using Xunit;

namespace PromiseDotNet.Tests
{
    public class PromiseTests
    {
        private void WaitForPromise<TValue, TReason>(Promise<TValue, TReason> promise)
        {
            while (promise.State == PromiseState.Pending)
                Thread.Sleep(10);
        }

        [Fact]
        public void ResolveProducesFulfilledPromise()
        {
            int resolvedValue = 0;

            WaitForPromise(
                Promise<int, int>.Resolve(42).Then(
                    x => resolvedValue = x,
                    x => { }
                )
            );

            Assert.Equal(42, resolvedValue);
        }

        [Fact]
        public void RejectProducesRejectedPromise()
        {
            int rejectedValue = 0;

            WaitForPromise(
                Promise<int, int>.Reject(42).Then(
                    x => { },
                    x => rejectedValue = x
                )
            );

            Assert.Equal(42, rejectedValue);
        }

        [Fact]
        public void NonChainedThenCallsShouldAllReceiveTheSameValueForFulfilled()
        {
            int value = 0;

            var promise = Promise<int, int>.Resolve(42);

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
                Promise<int, int>.Resolve(42)
                    .Then(x => x * 2)
                    .Then(x => x * 2)
                    .Then(x => value = x)
            );

            Assert.Equal(168, value);
        }
    }
}
