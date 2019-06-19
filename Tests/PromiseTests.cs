using RSG.Promises;
using System;
using System.Linq;
using RSG.Exceptions;
using Xunit;

namespace RSG.Tests
{
    public class PromiseTests
    {
        [Fact]
        public void can_resolve_simple_promise()
        {
            const int promisedValue = 5;
            var promise = Promise<int>.Resolved(promisedValue);

            var completed = 0;
            promise.Then(v =>
                {
                    Assert.Equal(promisedValue, v);
                    ++completed;
                });

            Assert.Equal(1, completed);
        }

        [Fact]
        public void can_reject_simple_promise()
        {
            var ex = new Exception();
            var promise = Promise<int>.Rejected(ex);

            var errors = 0;
            promise.Catch(e =>
            {
                Assert.Equal(ex, e);
                ++errors;
            });

            Assert.Equal(1, errors);
        }

        [Fact]
        public void exception_is_thrown_for_reject_after_reject()
        {
            var promise = new Promise<int>();

            promise.Reject(new Exception());

            Assert.Throws<PromiseStateException>(() =>
                promise.Reject(new Exception())
            );
        }

        [Fact]
        public void exception_is_thrown_for_reject_after_resolve()
        {
            var promise = new Promise<int>();

            promise.Resolve(5);

            Assert.Throws<PromiseStateException>(() =>
                promise.Reject(new Exception())
            );
        }

        [Fact]
        public void exception_is_thrown_for_resolve_after_reject()
        {
            var promise = new Promise<int>();

            promise.Reject(new Exception());

            Assert.Throws<PromiseStateException>(() => promise.Resolve(5));
        }

        [Fact]
        public void can_resolve_promise_and_trigger_then_handler()
        {
            var promise = new Promise<int>();

            var completed = 0;
            const int promisedValue = 15;

            promise.Then(v =>
            {
                Assert.Equal(promisedValue, v);
                ++completed;
            });

            promise.Resolve(promisedValue);

            Assert.Equal(1, completed);
        }

        [Fact]
        public void exception_is_thrown_for_resolve_after_resolve()
        {
            var promise = new Promise<int>();

            promise.Resolve(5);

            Assert.Throws<PromiseStateException>(() => promise.Resolve(5));
        }

        [Fact]
        public void can_resolve_promise_and_trigger_multiple_then_handlers_in_order()
        {
            var promise = new Promise<int>();

            var completed = 0;

            promise.Then(v => Assert.Equal(1, ++completed));
            promise.Then(v => Assert.Equal(2, ++completed));

            promise.Resolve(1);

            Assert.Equal(2, completed);
        }

        [Fact]
        public void can_resolve_promise_and_trigger_then_handler_with_callback_registration_after_resolve()
        {
            var promise = new Promise<int>();

            var completed = 0;
            const int promisedValue = -10;
            
            promise.Resolve(promisedValue);

            promise.Then(v => 
            {
                Assert.Equal(promisedValue, v);
                ++completed;
            });

            Assert.Equal(1, completed);
        }

        [Fact]
        public void can_reject_promise_and_trigger_error_handler()
        {
            var promise = new Promise<int>();

            var ex = new Exception();
            var completed = 0;
            promise.Catch(e =>
            {
                Assert.Equal(ex, e);
                ++completed;
            });

            promise.Reject(ex);

            Assert.Equal(1, completed);
        }

        [Fact]
        public void can_reject_promise_and_trigger_multiple_error_handlers_in_order()
        {
            var promise = new Promise<int>();

            var ex = new Exception();
            var completed = 0;

            promise.Catch(e =>
            {
                Assert.Equal(ex, e);
                Assert.Equal(1, ++completed);
            });
            promise.Catch(e =>
            {
                Assert.Equal(ex, e);
                Assert.Equal(2, ++completed);
            });

            promise.Reject(ex);

            Assert.Equal(2, completed);
        }

        [Fact]
        public void can_reject_promise_and_trigger_error_handler_with_registration_after_reject()
        {
            var promise = new Promise<int>();

            var ex = new Exception();
            promise.Reject(ex);

            var completed = 0;
            promise.Catch(e =>
            {
                Assert.Equal(ex, e);
                ++completed;
            });

            Assert.Equal(1, completed);
        }

        [Fact]
        public void error_handler_is_not_invoked_for_resolved_promised()
        {
            var promise = new Promise<int>();

            promise.Catch(e => throw new Exception("This shouldn't happen"));

            promise.Resolve(5);
        }

        [Fact]
        public void then_handler_is_not_invoked_for_rejected_promise()
        {
            var promise = new Promise<int>();

            promise.Then(v => throw new Exception("This shouldn't happen"));

            promise.Reject(new Exception("Rejection!"));
        }

        [Fact]
        public void chain_multiple_promises_using_first()
        {
            var promise = new Promise<int>();
            var chainedPromise1 = Promise<int>.Rejected(null);
            var chainedPromise2 = Promise<int>.Rejected(null);
            var chainedPromise3 = Promise<int>.Resolved(9001);

            bool completed = false;

            Promise<int>
                .First(() => chainedPromise1, () => chainedPromise2, () => chainedPromise3, () =>
                {
                    Assert.True(false, "Didn't stop on the first resolved promise");
                    return Promise<int>.Rejected(null);
                })
                .Then(result =>
                {
                    Assert.Equal(9001, result);
                    completed = true;
                })
            ;

            Assert.Equal(true, completed);
        }

        [Fact]
        public void chain_multiple_rejected_promises_using_first()
        {
            var promise = new Promise<int>();
            var chainedPromise1 = Promise<int>.Rejected(new Exception("First chained promise"));
            var chainedPromise2 = Promise<int>.Rejected(new Exception("Second chained promise"));
            var chainedPromise3 = Promise<int>.Rejected(new Exception("Third chained promise"));

            bool completed = false;

            Promise<int>
                .First(() => chainedPromise1, () => chainedPromise2, () => chainedPromise3)
                .Catch(ex =>
                {
                    Assert.Equal("Third chained promise", ex.Message);
                    completed = true;
                })
            ;

            Assert.Equal(true, completed);
        }

        [Fact]
        public void chain_multiple_promises_using_all()
        {
            var promise = new Promise<string>();
            var chainedPromise1 = new Promise<int>();
            var chainedPromise2 = new Promise<int>();
            const int chainedResult1 = 10;
            const int chainedResult2 = 15;

            var completed = 0;

            TestHelpers.VerifyDoesntThrowUnhandledException(() =>
            {
                promise
                    .ThenAll(i => EnumerableExt.FromItems(chainedPromise1, chainedPromise2)
                        .Cast<IPromise<int>>())
                    .Then(result =>
                    {
                        var items = result.ToArray();
                        Assert.Equal(2, items.Length);
                        Assert.Equal(chainedResult1, items[0]);
                        Assert.Equal(chainedResult2, items[1]);

                        ++completed;
                    });

                Assert.Equal(0, completed);

                promise.Resolve("hello");

                Assert.Equal(0, completed);

                chainedPromise1.Resolve(chainedResult1);

                Assert.Equal(0, completed);

                chainedPromise2.Resolve(chainedResult2);

                Assert.Equal(1, completed);
            });
        }


        [Fact]
        public void chain_multiple_promises_using_all_that_are_resolved_out_of_order()
        {
            var promise = new Promise<string>();
            var chainedPromise1 = new Promise<int>();
            var chainedPromise2 = new Promise<int>();
            const int chainedResult1 = 10;
            const int chainedResult2 = 15;

            var completed = 0;

            TestHelpers.VerifyDoesntThrowUnhandledException(() =>
            {
                promise
                    .ThenAll(i => EnumerableExt.FromItems(chainedPromise1, chainedPromise2)
                        .Cast<IPromise<int>>())
                    .Then(result =>
                    {
                        var items = result.ToArray();
                        Assert.Equal(2, items.Length);
                        Assert.Equal(chainedResult1, items[0]);
                        Assert.Equal(chainedResult2, items[1]);

                        ++completed;
                    });

                Assert.Equal(0, completed);

                promise.Resolve("hello");

                Assert.Equal(0, completed);

                chainedPromise2.Resolve(chainedResult2);

                Assert.Equal(0, completed);

                chainedPromise1.Resolve(chainedResult1);

                Assert.Equal(1, completed);
            });
        }

        [Fact]
        public void chain_multiple_promises_using_all_and_convert_to_non_value_promise()
        {
            var promise = new Promise<string>();
            var chainedPromise1 = new Promise();
            var chainedPromise2 = new Promise();

            var completed = 0;

            TestHelpers.VerifyDoesntThrowUnhandledException(() =>
            {
                promise
                    .ThenAll(i => EnumerableExt.FromItems(chainedPromise1, chainedPromise2)
                        .Cast<IPromise>())
                    .Then(() => ++completed);

                Assert.Equal(0, completed);

                promise.Resolve("hello");

                Assert.Equal(0, completed);

                chainedPromise1.Resolve();

                Assert.Equal(0, completed);

                chainedPromise2.Resolve();

                Assert.Equal(1, completed);
            });
        }

        [Fact]
        public void combined_promise_is_resolved_when_children_are_resolved()
        {
            var promise1 = new Promise<int>();
            var promise2 = new Promise<int>();

            TestHelpers.VerifyDoesntThrowUnhandledException(() =>
            {
                var all = Promise<int>.All(EnumerableExt.FromItems<IPromise<int>>(promise1, promise2));

                var completed = 0;

                all.Then(v =>
                {
                    ++completed;

                    var values = v.ToArray();
                    Assert.Equal(2, values.Length);
                    Assert.Equal(1, values[0]);
                    Assert.Equal(2, values[1]);
                });

                promise1.Resolve(1);
                promise2.Resolve(2);

                Assert.Equal(1, completed);
            });
        }

        [Fact]
        public void combined_promise_of_multiple_types_is_resolved_when_children_are_resolved()
        {
            var promise1 = new Promise<int>();
            var promise2 = new Promise<bool>();

            TestHelpers.VerifyDoesntThrowUnhandledException(() =>
            {
                var all = PromiseHelpers.All(promise1, promise2);

                var completed = 0;

                all.Then(v =>
                {
                    ++completed;

                    Assert.Equal(1, v.Item1);
                    Assert.Equal(true, v.Item2);
                });

                promise1.Resolve(1);
                promise2.Resolve(true);

                Assert.Equal(1, completed);
            });
        }

        [Fact]
        public void combined_promise_of_three_types_is_resolved_when_children_are_resolved()
        {
            var promise1 = new Promise<int>();
            var promise2 = new Promise<bool>();
            var promise3 = new Promise<float>();

            TestHelpers.VerifyDoesntThrowUnhandledException(() =>
            {
                var all = PromiseHelpers.All(promise1, promise2, promise3);

                var completed = 0;

                all.Then(v =>
                {
                    ++completed;

                    Assert.Equal(1, v.Item1);
                    Assert.Equal(true, v.Item2);
                    Assert.Equal(3.0f, v.Item3);
                });

                promise1.Resolve(1);
                promise2.Resolve(true);
                promise3.Resolve(3.0f);

                Assert.Equal(1, completed);
            });
        }

        [Fact]
        public void combined_promise_of_four_types_is_resolved_when_children_are_resolved()
        {
            var promise1 = new Promise<int>();
            var promise2 = new Promise<bool>();
            var promise3 = new Promise<float>();
            var promise4 = new Promise<double>();

            TestHelpers.VerifyDoesntThrowUnhandledException(() =>
            {
                var all = PromiseHelpers.All(promise1, promise2, promise3, promise4);

                var completed = 0;

                all.Then(v =>
                {
                    ++completed;

                    Assert.Equal(1, v.Item1);
                    Assert.Equal(true, v.Item2);
                    Assert.Equal(3.0f, v.Item3);
                    Assert.Equal(4.0, v.Item4);
                });

                promise1.Resolve(1);
                promise2.Resolve(true);
                promise3.Resolve(3.0f);
                promise4.Resolve(4.0);

                Assert.Equal(1, completed);
            });
        }

        [Fact]
        public void combined_promise_is_rejected_when_first_promise_is_rejected()
        {
            var promise1 = new Promise<int>();
            var promise2 = new Promise<int>();

            TestHelpers.VerifyDoesntThrowUnhandledException(() =>
            {
                var all = Promise<int>.All(EnumerableExt.FromItems<IPromise<int>>(promise1, promise2));

                all.Then(v => throw new Exception("Shouldn't happen"));

                var errors = 0;
                all.Catch(e => ++errors);

                promise1.Reject(new Exception("Error!"));
                promise2.Resolve(2);

                Assert.Equal(1, errors);
            });
        }

        [Fact]
        public void combined_promise_of_multiple_types_is_rejected_when_first_promise_is_rejected()
        {
            var promise1 = new Promise<int>();
            var promise2 = new Promise<bool>();

            TestHelpers.VerifyDoesntThrowUnhandledException(() =>
            {
                var all = PromiseHelpers.All(promise1, promise2);

                all.Then(v => throw new Exception("Shouldn't happen"));

                var errors = 0;
                all.Catch(e => ++errors);

                promise1.Reject(new Exception("Error!"));
                promise2.Resolve(true);

                Assert.Equal(1, errors);
            });
        }

        [Fact]
        public void combined_promise_is_rejected_when_second_promise_is_rejected()
        {
            var promise1 = new Promise<int>();
            var promise2 = new Promise<int>();

            TestHelpers.VerifyDoesntThrowUnhandledException(() =>
            {
                var all = Promise<int>.All(EnumerableExt.FromItems<IPromise<int>>(promise1, promise2));

                all.Then(v => throw new Exception("Shouldn't happen"));

                var errors = 0;
                all.Catch(e => ++errors);

                promise1.Resolve(2);
                promise2.Reject(new Exception("Error!"));

                Assert.Equal(1, errors);
            });
        }

        [Fact]
        public void combined_promise_of_multiple_types_is_rejected_when_second_promise_is_rejected()
        {
            var promise1 = new Promise<int>();
            var promise2 = new Promise<bool>();

            TestHelpers.VerifyDoesntThrowUnhandledException(() =>
            {
                var all = PromiseHelpers.All(promise1, promise2);

                all.Then(v => throw new Exception("Shouldn't happen"));

                var errors = 0;
                all.Catch(e => ++errors);

                promise1.Resolve(2);
                promise2.Reject(new Exception("Error!"));

                Assert.Equal(1, errors);
            });
        }

        [Fact]
        public void combined_promise_is_rejected_when_both_promises_are_rejected()
        {
            var promise1 = new Promise<int>();
            var promise2 = new Promise<int>();

            TestHelpers.VerifyDoesntThrowUnhandledException(() =>
            {
                var all = Promise<int>.All(EnumerableExt.FromItems<IPromise<int>>(promise1, promise2));

                all.Then(v => throw new Exception("Shouldn't happen"));

                var errors = 0;
                all.Catch(e => { ++errors; });

                promise1.Reject(new Exception("Error!"));
                promise2.Reject(new Exception("Error!"));

                Assert.Equal(1, errors);
            });
        }

        [Fact]
        public void combined_promise_of_multiple_types_is_rejected_when_both_promises_are_rejected()
        {
            var promise1 = new Promise<int>();
            var promise2 = new Promise<bool>();

            TestHelpers.VerifyDoesntThrowUnhandledException(() =>
            {
                var all = PromiseHelpers.All(promise1, promise2);

                all.Then(v => throw new Exception("Shouldn't happen"));

                var errors = 0;
                all.Catch(e => ++errors);

                promise1.Reject(new Exception("Error!"));
                promise2.Reject(new Exception("Error!"));

                Assert.Equal(1, errors);
            });
        }

        [Fact]
        public void combined_promise_is_resolved_if_there_are_no_promises()
        {
            TestHelpers.VerifyDoesntThrowUnhandledException(() =>
            {
                var all = Promise<int>.All(Enumerable.Empty<IPromise<int>>());

                var completed = 0;

                all.Then(v =>
                {
                    ++completed;

                    Assert.Empty(v);
                });

                Assert.Equal(1, completed);
            });
        }

        [Fact]
        public void combined_promise_is_resolved_when_all_promises_are_already_resolved()
        {
            var promise1 = Promise<int>.Resolved(1);
            var promise2 = Promise<int>.Resolved(1);

            TestHelpers.VerifyDoesntThrowUnhandledException(() =>
            {
                var all = Promise<int>.All(EnumerableExt.FromItems(promise1, promise2));

                var completed = 0;

                all.Then(v =>
                {
                    ++completed;

                    Assert.Empty(v);
                });

                Assert.Equal(1, completed);
            });
        }

        [Fact]
        public void combined_promise_of_multiple_types_is_resolved_when_all_promises_are_already_resolved()
        {
            var promise1 = Promise<int>.Resolved(1);
            var promise2 = Promise<bool>.Resolved(true);

            TestHelpers.VerifyDoesntThrowUnhandledException(() =>
            {
                var all = PromiseHelpers.All(promise1, promise2);

                var completed = 0;

                all.Then(v =>
                {
                    ++completed;

                    Assert.Equal(1, v.Item1);
                    Assert.Equal(true, v.Item2);
                });

                Assert.Equal(1, completed);
            });
        }

        [Fact]
        public void all_with_rejected_promise()
        {
            bool resolved = false;
            bool rejected = false;
            Exception caughtException = null;
            Exception exception = new Exception();

            var promiseA = new Promise<int>();
            var promise = Promise<int>
                .All(promiseA, Promise<int>.Rejected(exception))
                .Then(values => resolved = true)
                .Catch(ex =>
                {
                    caughtException = ex;
                    rejected = true;
                });
            promiseA.ReportProgress(0.5f);
            promiseA.Resolve(0);

            Assert.Equal(false, resolved);
            Assert.Equal(true, rejected);
            Assert.Equal(exception, caughtException);
        }

        [Fact]
        public void can_transform_promise_value()
        {
            var promise = new Promise<int>();

            var promisedValue = 15;
            var completed = 0;

            promise
                .Then(v => v.ToString())
                .Then(v =>
                {
                    Assert.Equal(promisedValue.ToString(), v);

                    ++completed;
                });

            promise.Resolve(promisedValue);

            Assert.Equal(1, completed);           
        }

        [Fact]
        public void rejection_of_source_promise_rejects_transformed_promise()
        {
            var promise = new Promise<int>();

            var ex = new Exception();
            var errors = 0;

            promise
                .Then(v => v.ToString())
                .Catch(e =>
                {
                    Assert.Equal(ex, e);

                    ++errors;
                });

            promise.Reject(ex);

            Assert.Equal(1, errors);
        }

        [Fact]
        public void exception_thrown_during_transform_rejects_transformed_promise()
        {
            var promise = new Promise<int>();

            const int promisedValue = 15;
            var errors = 0;
            var ex = new Exception();

            promise
                .Then(v => throw ex)
                .Catch(e =>
                {
                    Assert.Equal(ex, e);

                    ++errors;
                });

            promise.Resolve(promisedValue);

            Assert.Equal(1, errors);
        }

        [Fact]
        public void can_chain_promise_and_convert_type_of_value()
        {
            var promise = new Promise<int>();
            var chainedPromise = new Promise<string>();

            const int promisedValue = 15;
            const string chainedPromiseValue = "blah";
            var completed = 0;

            promise
                .Then<string>(v => chainedPromise)
                .Then(v =>
                {
                    Assert.Equal(chainedPromiseValue, v);

                    ++completed;
                });

            promise.Resolve(promisedValue);
            chainedPromise.Resolve(chainedPromiseValue);

            Assert.Equal(1, completed);
        }

        [Fact]
        public void can_chain_promise_and_convert_to_non_value_promise()
        {
            var promise = new Promise<int>();
            var chainedPromise = new Promise();

            const int promisedValue = 15;
            var completed = 0;

            promise
                .Then(v => (IPromise)chainedPromise)
                .Then(() => ++completed);

            promise.Resolve(promisedValue);
            chainedPromise.Resolve();

            Assert.Equal(1, completed);
        }

        [Fact]
        public void exception_thrown_in_chain_rejects_resulting_promise()
        {
            var promise = new Promise<int>();

            var ex = new Exception();
            var errors = 0;

            promise
                .Then(v => throw ex)
                .Catch(e =>
                {
                    Assert.Equal(ex, e);

                    ++errors;
                });

            promise.Resolve(15);

            Assert.Equal(1, errors);
        }

        [Fact]
        public void rejection_of_source_promise_rejects_chained_promise()
        {
            var promise = new Promise<int>();
            var chainedPromise = new Promise<string>();

            var ex = new Exception();
            var errors = 0;

            promise
                .Then<string>(v => chainedPromise)
                .Catch(e =>
                {
                    Assert.Equal(ex, e);

                    ++errors;
                });

            promise.Reject(ex);

            Assert.Equal(1, errors);
        }

        [Fact]
        public void race_is_resolved_when_first_promise_is_resolved_first()
        {
            var promise1 = new Promise<int>();
            var promise2 = new Promise<int>();

            var resolved = 0;

            TestHelpers.VerifyDoesntThrowUnhandledException(() =>
            {
                Promise<int>
                    .Race(promise1, promise2)
                    .Then(i => resolved = i);

                promise1.Resolve(5);

                Assert.Equal(5, resolved);
            });
        }

        [Fact]
        public void race_is_resolved_when_second_promise_is_resolved_first()
        {
            var promise1 = new Promise<int>();
            var promise2 = new Promise<int>();

            var resolved = 0;

            TestHelpers.VerifyDoesntThrowUnhandledException(() =>
            {
                Promise<int>
                    .Race(promise1, promise2)
                    .Then(i => resolved = i);

                promise2.Resolve(12);

                Assert.Equal(12, resolved);
            });
        }

        [Fact]
        public void race_is_rejected_when_first_promise_is_rejected_first()
        {
            var promise1 = new Promise<int>();
            var promise2 = new Promise<int>();

            Exception ex = null;

            TestHelpers.VerifyDoesntThrowUnhandledException(() =>
            {
                Promise<int>
                    .Race(promise1, promise2)
                    .Catch(e => ex = e);

                var expected = new Exception();
                promise1.Reject(expected);

                Assert.Equal(expected, ex);
            });
        }

        [Fact]
        public void race_is_rejected_when_second_promise_is_rejected_first()
        {
            var promise1 = new Promise<int>();
            var promise2 = new Promise<int>();

            Exception ex = null;

            TestHelpers.VerifyDoesntThrowUnhandledException(() =>
            {
                Promise<int>
                    .Race(promise1, promise2)
                    .Catch(e => ex = e);

                var expected = new Exception();
                promise2.Reject(expected);

                Assert.Equal(expected, ex);
            });
        }

        [Fact]
        public void can_resolve_promise_via_resolver_function()
        {
            var promise = new Promise<int>((resolve, reject) => resolve(5));

            var completed = 0;
            promise.Then(v => 
            {
                Assert.Equal(5, v);
                ++completed;
            });

            Assert.Equal(1, completed);
        }

        [Fact]
        public void can_reject_promise_via_reject_function()
        {
            var ex = new Exception();
            var promise = new Promise<int>((resolve, reject) =>
            {
                reject(ex);
            });

            var completed = 0;
            promise.Catch(e =>
            {
                Assert.Equal(ex, e);
                ++completed;
            });

            Assert.Equal(1, completed);
        }

        [Fact]
        public void exception_thrown_during_resolver_rejects_promise()
        {
            var ex = new Exception();
            var promise = new Promise<int>((resolve, reject) => throw ex);

            var completed = 0;
            promise.Catch(e =>
            {
                Assert.Equal(ex, e);
                ++completed;
            });

            Assert.Equal(1, completed);
        }        

        [Fact]
        public void unhandled_exception_is_propagated_via_event()
        {
            var promise = new Promise<int>();
            var ex = new Exception();
            var eventRaised = 0;

            EventHandler<ExceptionEventArgs> handler = (s, e) => 
                {
                    Assert.Equal(ex, e.Exception);

                    ++eventRaised;
                };

            Promise.UnhandledException += handler;

            try 
            {
                promise
                    .Then(a => throw ex)
                    .Done();

                promise.Resolve(5);

                Assert.Equal(1, eventRaised);
            }
            finally
            {
                Promise.UnhandledException -= handler;
            }
        }

        [Fact]
        public void exception_in_done_callback_is_propagated_via_event()
        {
            var promise = new Promise<int>();
            var ex = new Exception();
            var eventRaised = 0;

            EventHandler<ExceptionEventArgs> handler = (s, e) =>
            {
                Assert.Equal(ex, e.Exception);

                ++eventRaised;
            };

            Promise.UnhandledException += handler;

            try
            {
                promise
                    .Done(x => throw ex);

                promise.Resolve(5);

                Assert.Equal(1, eventRaised);
            }
            finally
            {
                Promise.UnhandledException -= handler;
            }
        }

        [Fact]
        public void handled_exception_is_not_propagated_via_event()
        {
            var promise = new Promise<int>();
            var ex = new Exception();
            var eventRaised = 0;

            EventHandler<ExceptionEventArgs> handler = (s, e) => ++eventRaised;

            Promise.UnhandledException += handler;

            try
            {
                promise
                    .Then(a => throw ex)
                    .Catch(_ => 
                    {
                        // Catch the error.
                    })
                    .Done();

                promise.Resolve(5);

                Assert.Equal(0, eventRaised);
            }
            finally
            {
                Promise.UnhandledException -= handler;
            }

        }

        [Fact]
        public void can_handle_Done_onResolved()
        {
            var promise = new Promise<int>();
            var callback = 0;
            const int expectedValue = 5;

            promise.Done(value =>
            {
                Assert.Equal(expectedValue, value);

                ++callback;
            });

            promise.Resolve(expectedValue);

            Assert.Equal(1, callback);
        }

        [Fact]
        public void can_handle_Done_onResolved_with_onReject()
        {
            var promise = new Promise<int>();
            var callback = 0;
            var errorCallback = 0;
            const int expectedValue = 5;

            promise.Done(
                value =>
                {
                    Assert.Equal(expectedValue, value);

                    ++callback;
                },
                ex => ++errorCallback
            );

            promise.Resolve(expectedValue);

            Assert.Equal(1, callback);
            Assert.Equal(0, errorCallback);
        }

        /*todo:
         * Also want a test that exception thrown during Then triggers the error handler.
         * How do Javascript promises work in this regard?
        [Fact]
        public void exception_during_Done_onResolved_triggers_error_hander()
        {
            var promise = new Promise<int>();
            var callback = 0;
            var errorCallback = 0;
            var expectedValue = 5;
            var expectedException = new Exception();

            promise.Done(
                value =>
                {
                    Assert.Equal(expectedValue, value);

                    ++callback;

                    throw expectedException;
                },
                ex =>
                {
                    Assert.Equal(expectedException, ex);

                    ++errorCallback;
                }
            );

            promise.Resolve(expectedValue);

            Assert.Equal(1, callback);
            Assert.Equal(1, errorCallback);
        }
         * */

        [Fact]
        public void exception_during_Then_onResolved_triggers_error_hander()
        {
            var promise = new Promise<int>();
            var callback = 0;
            var errorCallback = 0;
            var expectedException = new Exception();

            promise
                .Then(value => throw expectedException)
                .Done(
                    () => ++callback,
                    ex =>
                    {
                        Assert.Equal(expectedException, ex);

                        ++errorCallback;
                    }
                );

            promise.Resolve(6);

            Assert.Equal(0, callback);
            Assert.Equal(1, errorCallback);
        }

        [Fact]
        public void promises_have_sequential_ids()
        {
            var promise1 = new Promise<int>();
            var promise2 = new Promise<int>();

            Assert.Equal(promise1.Id + 1, promise2.Id);
        }


        [Fact]
        public void finally_is_called_after_resolve()
        {
            var promise = new Promise<int>();
            var callback = 0;

            promise.Finally(() => ++callback);

            promise.Resolve(0);

            Assert.Equal(1, callback);
        }

        [Fact]
        public void finally_is_called_after_reject()
        {
            var promise = new Promise<int>();
            var callback = 0;

            promise.Finally(() => ++callback);

            promise.Reject(new Exception());

            Assert.Equal(1, callback);
        }

        [Fact]
        //tc39
        public void resolved_chain_continues_after_finally()
        {
            var promise = new Promise<int>();
            var callback = 0;
            const int expectedValue = 42;

            promise
                .Finally(() => ++callback)
                .Then((x) =>
                {
                    Assert.Equal(expectedValue, x);
                    ++callback;
                });

            promise.Resolve(expectedValue);

            Assert.Equal(2, callback);
        }

        [Fact]
        //tc39
        public void rejected_chain_rejects_after_finally()
        {
            var promise = new Promise<int>();
            var callback = 0;

            promise
                .Finally(() => ++callback)
                .Catch(_ => ++callback);

            promise.Reject(new Exception());

            Assert.Equal(2, callback);
        }

        [Fact]
        public void rejected_chain_continues_after_ContinueWith_returning_non_value_promise()
        {
            var promise = new Promise<int>();
            var callback = 0;

            promise.ContinueWith(() => 
            {
                ++callback;
                return Promise.Resolved();
            })
            .Then(() => ++callback);

            promise.Reject(new Exception());

            Assert.Equal(2, callback);
        }

        [Fact]
        public void rejected_chain_continues_after_ContinueWith_returning_value_promise()
        {
            var promise = new Promise<int>();
            var callback = 0;
            const int expectedValue = 42;
            promise.ContinueWith(() => 
            {
                ++callback;
                return Promise<int>.Resolved(expectedValue);
            })
            .Then(x => 
            {
                Assert.Equal(expectedValue, x);
                ++callback;
            });

            promise.Reject(new Exception());

            Assert.Equal(2, callback);
        }

        [Fact]
        public void can_chain_promise_generic_after_finally()
        {
            var promise = new Promise<int>();
            const int expectedValue = 5;
            var callback = 0;

            promise.ContinueWith(() =>
            {
                ++callback;
                return Promise<int>.Resolved(expectedValue);
            })
            .Then(x =>
            {
                Assert.Equal(expectedValue, x);
                ++callback;
            });

            promise.Resolve(0);

            Assert.Equal(2, callback);
        }

        [Fact]
        //tc39
        public void can_chain_promise_after_finally()
        {
            var promise = new Promise<int>();
            var callback = 0;

            promise
                .Finally(() => ++callback)
                .Then(_ => ++callback);

            promise.Resolve(0);

            Assert.Equal(2, callback);
        }

        [Fact]
        //tc39 note: "a throw (or returning a rejected promise) in the finally callback will reject the new promise with that rejection reason."
        public void exception_in_finally_callback_is_caught_by_chained_catch()
        {
            //NOTE: Also tests that the new exception is passed thru promise chain

            var promise = new Promise<int>();
            var callback = 0;
            var expectedException = new Exception("Expected");

            promise.Finally(() =>
            {
                ++callback;
                throw expectedException;
            })
            .Catch(ex =>
            {
                Assert.Equal(expectedException, ex);
                ++callback;
            });

            promise.Reject(new Exception());

            Assert.Equal(2, callback);
        }

        [Fact]
        public void exception_in_ContinueWith_callback_returning_non_value_promise_is_caught_by_chained_catch()
        {
            //NOTE: Also tests that the new exception is passed thru promise chain

            var promise = new Promise<int>();
            var callback = 0;
            var expectedException = new Exception("Expected");

            promise.ContinueWith(() =>
            {
                ++callback;
                throw expectedException;
            })
            .Catch(ex =>
            {
                Assert.Equal(expectedException, ex);
                ++callback;
            });

            promise.Reject(new Exception());

            Assert.Equal(2, callback);
        }

        [Fact]
        public void exception_in_ContinueWith_callback_returning_value_promise_is_caught_by_chained_catch()
        {
            // NOTE: Also tests that the new exception is passed through promise chain

            var promise = new Promise<int>();
            var callback = 0;
            var expectedException = new Exception("Expected");

            promise.ContinueWith(new Func<IPromise<int>>(() =>
            {
                ++callback;
                throw expectedException;
            }))
            .Catch(ex =>
            {
                Assert.Equal(expectedException, ex);
                ++callback;
            });

            promise.Reject(new Exception());

            Assert.Equal(2, callback);
        }

        [Fact]
        public void exception_in_reject_callback_is_caught_by_chained_catch()
        {
            var expectedException = new Exception("Expected");
            Exception actualException = null;

            new Promise<object>((res, rej) => rej(new Exception()))
                .Then(
                    _ => Promise<object>.Resolved(null),
                    _ => throw expectedException
                )
                .Catch(ex => actualException = ex);

            Assert.Equal(expectedException, actualException);
        }

        [Fact]
        public void rejected_reject_callback_is_caught_by_chained_catch()
        {
            var expectedException = new Exception("Expected");
            Exception actualException = null;

            new Promise<object>((res, rej) => rej(new Exception()))
                .Then(
                    _ => Promise<object>.Resolved(null),
                    _ => Promise<object>.Rejected(expectedException)
                )
                .Catch(ex => actualException = ex);

            Assert.Equal(expectedException, actualException);
        }
    }
}
