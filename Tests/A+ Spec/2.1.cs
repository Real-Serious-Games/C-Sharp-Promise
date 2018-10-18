using System;
using RSG.Exceptions;
using Xunit;

namespace RSG.Tests.A__Spec
{
    public class _2_1
    {
        // 2.1.1.1.
        [Fact]
        public void When_pending_a_promise_may_transition_to_either_the_fulfilled_or_rejected_state()
        {
            var pendingPromise1 = new Promise<object>();
            Assert.Equal(PromiseState.Pending, pendingPromise1.CurState);
            pendingPromise1.Resolve(new object());
            Assert.Equal(PromiseState.Resolved, pendingPromise1.CurState);
        
            var pendingPromise2 = new Promise<object>();
            Assert.Equal(PromiseState.Pending, pendingPromise2.CurState);
            pendingPromise2.Reject(new Exception());
            Assert.Equal(PromiseState.Rejected, pendingPromise2.CurState);
        }

        // 2.1.2
        public class When_fulfilled_a_promise_
        {
            // 2.1.2.1
            [Fact]
            public void _must_not_transition_to_any_other_state()
            {
                var fulfilledPromise = new Promise<object>();
                fulfilledPromise.Resolve(new object());

                Assert.Throws<PromiseStateException>(() => fulfilledPromise.Reject(new Exception()));

                Assert.Equal(PromiseState.Resolved, fulfilledPromise.CurState);
            }

            // 2.1.2.2
            [Fact]
            public void _must_have_a_value_which_must_not_change()
            {
                var promisedValue = new object();
                var fulfilledPromise = new Promise<object>();
                var handled = 0;

                fulfilledPromise.Then(v =>
                {
                    Assert.Equal(promisedValue, v);
                    ++handled;
                });

                fulfilledPromise.Resolve(promisedValue);

                Assert.Throws<PromiseStateException>(() => fulfilledPromise.Resolve(new object()));

                Assert.Equal(1, handled);
            }
        }

        // 2.1.3
        public class When_rejected_a_promise_
        {
            // 2.1.3.1
            [Fact]
            public void _must_not_transition_to_any_other_state()
            {
                var rejectedPromise = new Promise<object>();
                rejectedPromise.Reject(new Exception());

                Assert.Throws<PromiseStateException>(() => rejectedPromise.Resolve(new object()));

                Assert.Equal(PromiseState.Rejected, rejectedPromise.CurState);
            }
            
            // 2.1.3.21
            [Fact]         
            public void _must_have_a_reason_which_must_not_change()
            {
                var rejectedPromise = new Promise<object>();
                var reason = new Exception();
                var handled = 0;

                rejectedPromise.Catch(e =>
                {
                    Assert.Equal(reason, e);
                    ++handled;
                });

                rejectedPromise.Reject(reason);

                Assert.Throws<PromiseStateException>(() => rejectedPromise.Reject(new Exception()));

                Assert.Equal(1, handled);
            }
        }
    }
}
