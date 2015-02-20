using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace RSG.Promise.Tests.A__Spec
{
    public class _2_2
    {
        // 2.2.6
        public class then_may_be_called_multiple_times_on_the_same_promise
        {
            // 2.2.6.1
            [Fact]
            public void when_promise_is_fulfilled_all_respective_onFulfilled_callbacks_must_execute_in_the_order_of_their_originating_calls_to_then()
            {
                var promise = new Promise<object>();

                var order = 0;

                promise.Then(_ =>
                {
                    Assert.Equal(1, ++order);
                });
                promise.Then(_ =>
                {
                    Assert.Equal(2, ++order);
                });
                promise.Then(_ =>
                {
                    Assert.Equal(3, ++order);
                });

                promise.Resolve(new object());

                Assert.Equal(3, order);
            }

            // 2.2.6.2
            [Fact]
            public void when_promise_is_rejected_all_respective_onRejected_callbacks_must_execute_in_the_order_of_their_originating_calls_to_then()
            {
                var promise = new Promise<object>();

                var order = 0;

                promise.Catch(_ =>
                {
                    Assert.Equal(1, ++order);
                });
                promise.Catch(_ =>
                {
                    Assert.Equal(2, ++order);
                });
                promise.Catch(_ =>
                {
                    Assert.Equal(3, ++order);
                });

                promise.Reject(new Exception());

                Assert.Equal(3, order);
            }
        }

        // 2.2.7
        public class then_must_return_a_promise
        {

            // 2.2.7.1
            // todo: Catch handler needs to be able to return a value.
            public class If_either_onFulfilled_or_onRejected_returns_a_value_x_fulfill_promise_with_x
            {
                [Fact]
                public void when_promise1_is_resolved()
                {
                    var promise1 = new Promise<object>();

                    var promisedValue1 = new object();
                    var promisedValue2 = new object();

                    var promise2 = 
                        promise1.Transform(_ => promisedValue2);

                    var promise1ThenHandler = 0;
                    promise1.Then(v =>
                    {
                        Assert.Equal(promisedValue1, v);
                        ++promise1ThenHandler;
                        
                    });

                    var promise2ThenHandler = 0;
                    promise2.Then(v =>
                    {
                        Assert.Equal(promisedValue2, v);
                        ++promise2ThenHandler;

                    });

                    promise1.Resolve(promisedValue1);

                    Assert.Equal(1, promise1ThenHandler);
                    Assert.Equal(1, promise2ThenHandler);
                }
            }

            // 2.2.7.2
            public class if_either_onFulfilled_or_onRejected_throws_an_exception_e_promise2_must_be_rejected_with_e_as_the_reason
            {
                [Fact]
                public void when_promise1_is_resolved_1()
                {
                    var promise1 = new Promise<object>();

                    var e = new Exception();
                    Func<object, IPromise<object>> thenHandler = _ =>
                    {
                        throw e;
                    };

                    var promise2 = 
                        promise1.Then(thenHandler);

                    promise1.Catch(_ =>
                    {
                        throw new Exception("This shouldn't happen!");
                    });

                    var errorHandledForPromise2 = 0;
                    promise2.Catch(ex =>
                    {
                        Assert.Equal(e, ex);

                        ++errorHandledForPromise2;
                    });

                    promise1.Resolve(new object());

                    Assert.Equal(1, errorHandledForPromise2);
                }

                [Fact]
                public void when_promise1_is_resolved_2()
                {
                    var promise1 = new Promise<object>();

                    var e = new Exception();
                    Action<object> thenHandler = _ =>
                    {
                        throw e;
                    };

                    var promise2 = 
                        promise1.Then(thenHandler);

                    promise1.Catch(_ =>
                    {
                        throw new Exception("This shouldn't happen!");
                    });

                    var errorHandledForPromise2 = 0;
                    promise2.Catch(ex =>
                    {
                        Assert.Equal(e, ex);

                        ++errorHandledForPromise2;
                    });

                    promise1.Resolve(new object());

                    Assert.Equal(1, errorHandledForPromise2);
                }

                [Fact]
                public void when_promise1_is_rejected()
                {
                    var promise1 = new Promise<object>();

                    var e = new Exception();
                    var promise2 = 
                        promise1.Catch(_ =>
                        {
                            throw e;
                        });

                    promise1.Catch(_ =>
                    {
                        throw new Exception("This shouldn't happen!");
                    });

                    var errorHandledForPromise2 = 0;
                    promise2.Catch(ex =>
                    {
                        Assert.Equal(e, ex);

                        ++errorHandledForPromise2;
                    });

                    promise1.Reject(new Exception());

                    Assert.Equal(1, errorHandledForPromise2);
                }
            }

            // 2.2.7.3
            [Fact]
            public void If_onFulfilled_is_not_a_function_and_promise1_is_fulfilled_promise2_must_be_fulfilled_with_the_same_value_as_promise1()
            {
                var promise1 = new Promise<object>();

                var promise2 = promise1.Catch(_ => 
                {
                    throw new Exception("There shouldn't be an error");
                });

                var promisedValue = new object();
                var promise2ThenHandler = 0;

                promise2.Then(v =>
                {
                    Assert.Equal(promisedValue, v);
                    ++promise2ThenHandler;
                });

                promise1.Resolve(promisedValue);

                Assert.Equal(1, promise2ThenHandler);
            }

            [Fact]
            public void If_onRejected_is_not_a_function_and_promise1_is_rejected_promise2_must_be_rejected_with_the_same_reason_as_promise1()
            {
                var promise1 = new Promise<object>();

                var promise2 = promise1.Then(_ =>
                {
                    throw new Exception("There shouldn't be a then callback");
                });

                var e = new Exception();
                var promise2CatchHandler = 0;

                promise2.Catch(ex =>
                {
                    Assert.Equal(e, ex);
                    ++promise2CatchHandler;
                });

                promise1.Reject(e);

                Assert.Equal(1, promise2CatchHandler);
            }
        }
    }
}
