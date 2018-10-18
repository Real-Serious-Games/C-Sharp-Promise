using RSG.Promises;
using System;
using System.Linq;
using RSG.Exceptions;
using Xunit;

namespace RSG.Tests
{
    public class Promise_NonGeneric_Tests
    {
        [Fact]
        public void can_resolve_simple_promise()
        {
            var promise = Promise.Resolved();

            var completed = 0;
            promise.Then(() => ++completed);

            Assert.Equal(1, completed);
        }

        [Fact]
        public void can_reject_simple_promise()
        {
            var ex = new Exception();
            var promise = Promise.Rejected(ex);

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
            var promise = new Promise();

            promise.Reject(new Exception());

            Assert.Throws<PromiseStateException>(() =>
                promise.Reject(new Exception())
            );
        }

        [Fact]
        public void exception_is_thrown_for_reject_after_resolve()
        {
            var promise = new Promise();

            promise.Resolve();

            Assert.Throws<PromiseStateException>(() =>
                promise.Reject(new Exception())
            );
        }

        [Fact]
        public void exception_is_thrown_for_resolve_after_reject()
        {
            var promise = new Promise();

            promise.Reject(new Exception());

            Assert.Throws<PromiseStateException>(() => promise.Resolve());
        }

        [Fact]
        public void can_resolve_promise_and_trigger_then_handler()
        {
            var promise = new Promise();

            var completed = 0;

            promise.Then(() => ++completed);

            promise.Resolve();

            Assert.Equal(1, completed);
        }

        [Fact]
        public void exception_is_thrown_for_resolve_after_resolve()
        {
            var promise = new Promise();

            promise.Resolve();

            Assert.Throws<PromiseStateException>(() => promise.Resolve());
        }

        [Fact]
        public void can_resolve_promise_and_trigger_multiple_then_handlers_in_order()
        {
            var promise = new Promise();

            var completed = 0;

            promise.Then(() => Assert.Equal(1, ++completed));
            promise.Then(() => Assert.Equal(2, ++completed));

            promise.Resolve();

            Assert.Equal(2, completed);
        }

        [Fact]
        public void can_resolve_promise_and_trigger_then_handler_with_callback_registration_after_resolve()
        {
            var promise = new Promise();

            var completed = 0;

            promise.Resolve();

            promise.Then(() => ++completed);

            Assert.Equal(1, completed);
        }

        [Fact]
        public void can_reject_promise_and_trigger_error_handler()
        {
            var promise = new Promise();

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
            var promise = new Promise();

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
            var promise = new Promise();

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
            var promise = new Promise();

            promise.Catch(e => throw new Exception("This shouldn't happen"));

            promise.Resolve();
        }

        [Fact]
        public void then_handler_is_not_invoked_for_rejected_promise()
        {
            var promise = new Promise();

            promise.Then(() => throw new Exception("This shouldn't happen"));

            promise.Reject(new Exception("Rejection!"));
        }

        [Fact]
        public void chain_multiple_promises_using_all()
        {
            var promise = new Promise();
            var chainedPromise1 = new Promise();
            var chainedPromise2 = new Promise();

            var completed = 0;

            TestHelpers.VerifyDoesntThrowUnhandledException(() =>
            {
                promise
                    .ThenAll(() => EnumerableExt.FromItems(chainedPromise1, chainedPromise2)
                        .Cast<IPromise>())
                    .Then(() => ++completed);

                Assert.Equal(0, completed);

                promise.Resolve();

                Assert.Equal(0, completed);

                chainedPromise1.Resolve();

                Assert.Equal(0, completed);

                chainedPromise2.Resolve();

                Assert.Equal(1, completed);
            });
        }

        [Fact]
        public void chain_multiple_promises_using_all_that_are_resolved_out_of_order()
        {
            var promise = new Promise();
            var chainedPromise1 = new Promise<int>();
            var chainedPromise2 = new Promise<int>();
            const int chainedResult1 = 10;
            const int chainedResult2 = 15;

            var completed = 0;

            TestHelpers.VerifyDoesntThrowUnhandledException(() =>
            {
                promise
                    .ThenAll(() => EnumerableExt.FromItems(chainedPromise1, chainedPromise2)
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

                promise.Resolve();

                Assert.Equal(0, completed);

                chainedPromise1.Resolve(chainedResult1);

                Assert.Equal(0, completed);

                chainedPromise2.Resolve(chainedResult2);

                Assert.Equal(1, completed);
            });
        }

        [Fact]
        public void chain_multiple_value_promises_using_all_resolved_out_of_order()
        {
            var promise = new Promise();
            var chainedPromise1 = new Promise<int>();
            var chainedPromise2 = new Promise<int>();
            const int chainedResult1 = 10;
            const int chainedResult2 = 15;

            var completed = 0;

            TestHelpers.VerifyDoesntThrowUnhandledException(() =>
            {
                promise
                    .ThenAll(() => EnumerableExt.FromItems(chainedPromise1, chainedPromise2)
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

                promise.Resolve();

                Assert.Equal(0, completed);

                chainedPromise2.Resolve(chainedResult2);

                Assert.Equal(0, completed);

                chainedPromise1.Resolve(chainedResult1);

                Assert.Equal(1, completed);
            });
        }

        [Fact]
        public void combined_promise_is_resolved_when_children_are_resolved()
        {
            var promise1 = new Promise();
            var promise2 = new Promise();

            TestHelpers.VerifyDoesntThrowUnhandledException(() =>
            {
                var all = Promise.All(EnumerableExt.FromItems<IPromise>(promise1, promise2));

                var completed = 0;

                all.Then(() => ++completed);

                promise1.Resolve();
                promise2.Resolve();

                Assert.Equal(1, completed);
            });
        }

        [Fact]
        public void combined_promise_is_rejected_when_first_promise_is_rejected()
        {
            var promise1 = new Promise();
            var promise2 = new Promise();

            TestHelpers.VerifyDoesntThrowUnhandledException(() =>
            {
                var all = Promise.All(EnumerableExt.FromItems<IPromise>(promise1, promise2));

                var errors = 0;
                all.Catch(e => ++errors);

                promise1.Reject(new Exception("Error!"));
                promise2.Resolve();

                Assert.Equal(1, errors);
            });
        }

        [Fact]
        public void combined_promise_is_rejected_when_second_promise_is_rejected()
        {
            var promise1 = new Promise();
            var promise2 = new Promise();

            TestHelpers.VerifyDoesntThrowUnhandledException(() => {
                var all = Promise.All(EnumerableExt.FromItems<IPromise>(promise1, promise2));

                var errors = 0;
                all.Catch(e => { ++errors; });

                promise1.Resolve();
                promise2.Reject(new Exception("Error!"));

                Assert.Equal(1, errors);
            });
        }

        [Fact]
        public void combined_promise_is_rejected_when_both_promises_are_rejected()
        {
            var promise1 = new Promise();
            var promise2 = new Promise();

            TestHelpers.VerifyDoesntThrowUnhandledException(() =>
            {
                var all = Promise.All(EnumerableExt.FromItems<IPromise>(promise1, promise2));

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
                var all = Promise.All(Enumerable.Empty<IPromise>());

                var completed = 0;

                all.Then(() => ++completed);

                Assert.Equal(1, completed);
            });
        }

        [Fact]
        public void combined_promise_is_resolved_when_all_promises_are_already_resolved()
        {
            var promise1 = Promise.Resolved();
            var promise2 = Promise.Resolved();

            TestHelpers.VerifyDoesntThrowUnhandledException(() =>
            {
                var all = Promise.All(EnumerableExt.FromItems(promise1, promise2));

                var completed = 0;

                all.Then(() => ++completed);

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

            var promiseA = new Promise();
            var promise = Promise
                .All(promiseA, Promise.Rejected(exception))
                .Then(() => resolved = true)
                .Catch(ex =>
                {
                    caughtException = ex;
                    rejected = true;
                });
            promiseA.ReportProgress(0.5f);
            promiseA.Resolve();
            
            Assert.Equal(false, resolved);
            Assert.Equal(true, rejected);
            Assert.Equal(exception, caughtException);
        }

        [Fact]
        public void exception_thrown_during_transform_rejects_promise()
        {
            var promise = new Promise();

            var errors = 0;
            var ex = new Exception();

            promise
                .Then(() => throw ex)
                .Catch(e =>
                {
                    Assert.Equal(ex, e);

                    ++errors;
                });

            promise.Resolve();

            Assert.Equal(1, errors);
        }

        [Fact]
        public void can_chain_promise()
        {
            var promise = new Promise();
            var chainedPromise = new Promise();

            var completed = 0;

            promise
                .Then(() => chainedPromise)
                .Then(() => ++completed);

            promise.Resolve();
            chainedPromise.Resolve();

            Assert.Equal(1, completed);
        }

        [Fact]
        public void can_chain_promise_and_convert_to_promise_that_yields_a_value()
        {
            var promise = new Promise();
            var chainedPromise = new Promise<string>();
            const string chainedPromiseValue = "some-value";

            var completed = 0;

            promise
                .Then(() => chainedPromise)
                .Then(v => 
                {
                    Assert.Equal(chainedPromiseValue, v);

                    ++completed;
                });

            promise.Resolve();
            chainedPromise.Resolve(chainedPromiseValue);

            Assert.Equal(1, completed);
        }

        [Fact]
        public void exception_thrown_in_chain_rejects_resulting_promise()
        {
            var promise = new Promise();

            var ex = new Exception();
            var errors = 0;

            promise
                .Then(() => throw ex)
                .Catch(e =>
                {
                    Assert.Equal(ex, e);

                    ++errors;
                });

            promise.Resolve();

            Assert.Equal(1, errors);
        }

        [Fact]
        public void rejection_of_source_promise_rejects_chained_promise()
        {
            var promise = new Promise();
            var chainedPromise = new Promise();

            var ex = new Exception();
            var errors = 0;

            promise
                .Then(() => chainedPromise)
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
            var promise1 = new Promise();
            var promise2 = new Promise();

            var completed = 0;

            TestHelpers.VerifyDoesntThrowUnhandledException(() =>
            {
                Promise
                    .Race(promise1, promise2)
                    .Then(() => ++completed);

                promise1.Resolve();

                Assert.Equal(1, completed);
            });
        }

        [Fact]
        public void race_is_resolved_when_second_promise_is_resolved_first()
        {
            var promise1 = new Promise();
            var promise2 = new Promise();

            var completed = 0;

            TestHelpers.VerifyDoesntThrowUnhandledException(() =>
            {
                Promise
                    .Race(promise1, promise2)
                    .Then(() => ++completed);

                promise2.Resolve();

                Assert.Equal(1, completed);
            });
        }

        [Fact]
        public void race_is_rejected_when_first_promise_is_rejected_first()
        {
            var promise1 = new Promise();
            var promise2 = new Promise();

            Exception ex = null;

            TestHelpers.VerifyDoesntThrowUnhandledException(() =>
            {
                Promise
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
            var promise1 = new Promise();
            var promise2 = new Promise();

            Exception ex = null;

            TestHelpers.VerifyDoesntThrowUnhandledException(() =>
            {
                Promise
                    .Race(promise1, promise2)
                    .Catch(e => ex = e);

                var expected = new Exception();
                promise2.Reject(expected);

                Assert.Equal(expected, ex);
            });
        }

        [Fact]
        public void sequence_with_no_operations_is_directly_resolved()
        {
            var completed = 0;

            Promise
                .Sequence()
                .Then(() => ++completed);

            Assert.Equal(1, completed);
        }

        [Fact]
        public void sequenced_is_not_resolved_when_operation_is_not_resolved()
        {
            var completed = 0;

            Promise
                .Sequence(() => new Promise())
                .Then(() => ++completed);

            Assert.Equal(0, completed);
        }

        [Fact]
        public void sequence_is_resolved_when_operation_is_resolved()
        {
            var completed = 0;

            Promise
                .Sequence(Promise.Resolved)
                .Then(() => ++completed);

            Assert.Equal(1, completed);
        }

        [Fact]
        public void sequence_is_unresolved_when_some_operations_are_unresolved()
        {
            var completed = 0;

            Promise
                .Sequence(
                    Promise.Resolved,
                    () => new Promise()
                )
                .Then(() => ++completed);

            Assert.Equal(0, completed);
        }

        [Fact]
        public void sequence_is_resolved_when_all_operations_are_resolved()
        {
            var completed = 0;

            Promise
                .Sequence(Promise.Resolved, Promise.Resolved)
                .Then(() => ++completed);

            Assert.Equal(1, completed);
        }

        [Fact]
        public void sequenced_operations_are_run_in_order_is_directly_resolved()
        {
            var order = 0;

            Promise
                .Sequence(
                    () =>
                    {
                        Assert.Equal(1, ++order);
                        return Promise.Resolved();
                    },
                    () =>
                    {
                        Assert.Equal(2, ++order);
                        return Promise.Resolved();
                    },
                    () =>
                    {
                        Assert.Equal(3, ++order);
                        return Promise.Resolved();
                    }
                );

            Assert.Equal(3, order);
        }

        [Fact]
        public void exception_thrown_in_sequence_rejects_the_promise()
        {
            var errored = 0;
            var completed = 0;
            var ex = new Exception();

            Promise
                .Sequence(() => throw ex)
                .Then(() => ++completed)
                .Catch(e =>
                {
                    Assert.Equal(ex, e);
                    ++errored;
                });

            Assert.Equal(1, errored);
            Assert.Equal(0, completed);
        }

        [Fact]
        public void exception_thrown_in_sequence_stops_following_operations_from_being_invoked()
        {
            var completed = 0;

            Promise
                .Sequence(
                    () => 
                    {
                        ++completed;
                        return Promise.Resolved();
                    },
                    () => throw new Exception(),
                    () =>
                    {
                        ++completed;
                        return Promise.Resolved();
                    }
                );

            Assert.Equal(1, completed);
        }

        [Fact]
        public void can_resolve_promise_via_resolver_function()
        {
            var promise = new Promise((resolve, reject) =>
            {
                resolve();
            });

            var completed = 0;
            promise.Then(() =>
            {
                ++completed;
            });

            Assert.Equal(1, completed);
        }

        [Fact]
        public void can_reject_promise_via_reject_function()
        {
            var ex = new Exception();
            var promise = new Promise((resolve, reject) =>
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
        public void exception_thrown_during_resolver_rejects_proimse()
        {
            var ex = new Exception();
            var promise = new Promise((resolve, reject) => throw ex);

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
            var promise = new Promise();
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
                    .Then(() => throw ex)
                    .Done();

                promise.Resolve();

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
            var promise = new Promise();
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
                    .Done(() => throw ex);

                promise.Resolve();

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
            var promise = new Promise();
            var ex = new Exception();
            var eventRaised = 0;

            EventHandler<ExceptionEventArgs> handler = (s, e) => ++eventRaised;

            Promise.UnhandledException += handler;

            try
            {
                promise
                    .Then(() => throw ex)
                    .Catch(_ =>
                    {
                        // Catch the error.
                    })
                    .Done();

                promise.Resolve();

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
            var promise = new Promise();
            var callback = 0;

            promise.Done(() => ++callback);

            promise.Resolve();

            Assert.Equal(1, callback);
        }

        [Fact]
        public void can_handle_Done_onResolved_with_onReject()
        {
            var promise = new Promise();
            var callback = 0;
            var errorCallback = 0;

            promise.Done(
                () => ++callback,
                ex => ++errorCallback
            );

            promise.Resolve();

            Assert.Equal(1, callback);
            Assert.Equal(0, errorCallback);
        }

        /*todo:
         * Also want a test that exception thrown during Then triggers the error handler.
         * How do Javascript promises work in this regard?
        [Fact]
        public void exception_during_Done_onResolved_triggers_error_hander()
        {
            var promise = new Promise();
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
            var promise = new Promise();
            var callback = 0;
            var errorCallback = 0;
            var expectedException = new Exception();

            promise
                .Then(() => throw expectedException)
                .Done(
                    () => ++callback,
                    ex =>
                    {
                        Assert.Equal(expectedException, ex);

                        ++errorCallback;
                    }
                );

            promise.Resolve();

            Assert.Equal(0, callback);
            Assert.Equal(1, errorCallback);
        }

        [Fact]
        public void inner_exception_handled_by_outer_promise()
        {
            var promise = new Promise();
            var errorCallback = 0;
            var expectedException = new Exception();

            var eventRaised = 0;

            EventHandler<ExceptionEventArgs> handler = (s, e) => ++eventRaised;

            Promise.UnhandledException += handler;

            try
            {
                promise
                    .Then(() => Promise.Resolved().Then(() => throw expectedException))
                    .Catch(ex =>
                    {
                        Assert.Equal(expectedException, ex);

                        ++errorCallback;
                    });

                promise.Resolve();

                // No "done" in the chain, no generic event handler should be called
                Assert.Equal(0, eventRaised);

                // Instead the catch should have got the exception
                Assert.Equal(1, errorCallback);
            }
            finally
            {
                Promise.UnhandledException -= handler;
            }
        }

        [Fact]
        public void inner_exception_handled_by_outer_promise_with_results()
        {
            var promise = new Promise<int>();
            var errorCallback = 0;
            var expectedException = new Exception();

            var eventRaised = 0;

            EventHandler<ExceptionEventArgs> handler = (s, e) => ++eventRaised;

            Promise.UnhandledException += handler;

            try
            {
                promise
                    .Then(_ => Promise<int>.Resolved(5).Then(__ => throw expectedException))
                    .Catch(ex =>
                    {
                        Assert.Equal(expectedException, ex);

                        ++errorCallback;
                    });

                promise.Resolve(2);

                // No "done" in the chain, no generic event handler should be called
                Assert.Equal(0, eventRaised);

                // Instead the catch should have got the exception
                Assert.Equal(1, errorCallback);
            }
            finally
            {
                Promise.UnhandledException -= handler;
            }
        }

        [Fact]
        public void promises_have_sequential_ids()
        {
            var promise1 = new Promise();
            var promise2 = new Promise();

            Assert.Equal(promise1.Id + 1, promise2.Id);
        }


        [Fact]
        public void finally_is_called_after_resolve()
        {
            var promise = new Promise();
            var callback = 0;

            promise.Finally(() => ++callback);

            promise.Resolve();

            Assert.Equal(1, callback);
        }

        [Fact]
        public void finally_is_called_after_reject()
        {
            var promise = new Promise();
            var callback = 0;

            promise.Finally(() => ++callback);

            promise.Reject(new Exception());

            Assert.Equal(1, callback);
        }

        [Fact]
        public void resolved_chain_continues_after_finally()
        {
            var promise = new Promise();
            var callback = 0;

            promise.Finally(() => ++callback)
            .Then(() => ++callback);

            promise.Resolve();

            Assert.Equal(2, callback);
        }

        [Fact]
        public void rejected_chain_rejects_after_finally()
        {
            var promise = new Promise();
            var callback = 0;

            promise.Finally(() => ++callback)
            .Catch(_ => ++callback);

            promise.Reject(new Exception());

            Assert.Equal(2, callback);
        }

        [Fact]
        public void rejected_chain_continues_after_ContinueWith_returning_non_value_promise() {
            var promise = new Promise();
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
        public void rejected_chain_continues_after_ContinueWith_returning_value_promise() {
            var promise = new Promise();
            var callback = 0;
            const string expectedValue = "foo";

            promise.ContinueWith(() => {
                ++callback;
                return Promise<string>.Resolved("foo");
            })
            .Then(x => {
                Assert.Equal(expectedValue, x);
                ++callback;
            });

            promise.Reject(new Exception());

            Assert.Equal(2, callback);
        }

        [Fact]
        //tc39 note: "a throw (or returning a rejected promise) in the finally callback will reject the new promise with that rejection reason."
        public void exception_in_finally_callback_is_caught_by_chained_catch()
        {
            //NOTE: Also tests that the new exception is passed thru promise chain

            var promise = new Promise();
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

            var promise = new Promise();
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
            //NOTE: Also tests that the new exception is passed through promise chain

            var promise = new Promise();
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
        public void can_chain_promise_after_ContinueWith()
        {
            var promise = new Promise();
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

            promise.Resolve();

            Assert.Equal(2, callback);
        }
    }
}
