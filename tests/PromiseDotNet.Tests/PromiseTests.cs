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
    }
}
