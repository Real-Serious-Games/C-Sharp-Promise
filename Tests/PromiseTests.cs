using Moq;
using RSG.Promise;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace RSG.Promise.Tests
{
    public class PromiseTests
    {
        [Fact]
        public void can_resolve_simple_promise()
        {
            var promisedValue = 5;
            var promise = Promise<int>.Resolved(promisedValue);

            var completed = 0;
            promise.Done(v =>
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
        public void can_resolve_promise_and_trigger_completed_handler()
        {
            var promise = new Promise<int>();

            var completed = 0;

            promise.Done(() => ++completed);

            promise.Resolve(1);

            Assert.Equal(1, completed);
        }

        [Fact]
        public void exception_is_thrown_for_resolve_after_resolve()
        {
            var promise = new Promise<int>();

            promise.Resolve(5);

            Assert.Throws<ApplicationException>(() =>
                promise.Resolve(5)
            );
        }

        [Fact]
        public void can_resolve_promise_and_trigger_multiple_completed_handlers()
        {
            var promise = new Promise<int>();

            var completed1 = 0;
            var completed2 = 0;

            promise.Done(() => ++completed1);
            promise.Done(() => ++completed2);

            promise.Resolve(1);

            Assert.Equal(1, completed1);
            Assert.Equal(1, completed2);
        }

        [Fact]
        public void can_resolve_promise_and_trigger_completed_handler_with_registration_after_resolve()
        {
            var promise = new Promise<int>();

            var completed = 0;

            promise.Resolve(1);

            promise.Done(() => ++completed);

            Assert.Equal(1, completed);
        }

        [Fact]
        public void can_resolve_promise_and_trigger_multiple_completed_handlers_with_registration_after_resolve()
        {
            var promise = new Promise<int>();

            var completed1 = 0;
            var completed2 = 0;

            promise.Resolve(1);

            promise.Done(() => ++completed1);
            promise.Done(() => ++completed2);            

            Assert.Equal(1, completed1);
            Assert.Equal(1, completed2);
        }

        [Fact]
        public void can_resolve_with_value_and_trigger_completed_handler()
        {
            var promise = new Promise<int>();

            var completed = 0;

            promise.Done(v =>
            {
                Assert.Equal(-5, v);
                ++completed;
            });

            promise.Resolve(-5);

            Assert.Equal(1, completed);
        }

        [Fact]
        public void can_resolve_with_value_and_trigger_multiple_completed_handlers()
        {
            var promise = new Promise<int>();

            var completed1 = 0;
            var completed2 = 0;

            promise.Done(v => 
            {
                Assert.Equal(5, v); 
                ++completed1;
            });
            promise.Done(v =>
            {
                Assert.Equal(5, v);
                ++completed2;
            });

            promise.Resolve(5);

            Assert.Equal(1, completed1);
            Assert.Equal(1, completed2);
        }

        [Fact]
        public void can_resolve_with_value_and_trigger_completed_handler_with_registration_after_resolve()
        {
            var promise = new Promise<int>();

            var completed = 0;

            promise.Done(v =>
            {
                Assert.Equal(21, v);
                ++completed;
            });

            promise.Resolve(21);

            Assert.Equal(1, completed);
        }

        [Fact]
        public void can_resolve_with_value_and_trigger_multiple_completed_handlers_with_registration_after_resolve()
        {
            var promise = new Promise<int>();

            var completed1 = 0;
            var completed2 = 0;

            promise.Resolve(11);

            promise.Done(v =>
            {
                Assert.Equal(11, v);
                ++completed1;
            });
            promise.Done(v =>
            {
                Assert.Equal(11, v);
                ++completed2;
            });

            Assert.Equal(1, completed1);
            Assert.Equal(1, completed2);
        }

        [Fact]
        public void can_reject_promise_and_trigger_error_handler()
        {
            var promise = new Promise<int>();

            var ex = new ApplicationException();
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
        public void exception_is_thrown_for_reject_after_reject()
        {
            var promise = new Promise<int>();

            promise.Reject(new ApplicationException());

            Assert.Throws<ApplicationException>(() =>
                promise.Reject(new ApplicationException())
            );
        }

        [Fact]
        public void exception_is_thrown_for_reject_after_resolve()
        {
            var promise = new Promise<int>();

            promise.Resolve(5);

            Assert.Throws<ApplicationException>(() =>
                promise.Reject(new ApplicationException())
            );
        }

        [Fact]
        public void exception_is_thrown_for_resolve_after_reject()
        {
            var promise = new Promise<int>();

            promise.Reject(new ApplicationException());

            Assert.Throws<ApplicationException>(() =>
                promise.Resolve(5)                
            );
        }

        [Fact]
        public void can_reject_promise_and_trigger_multiple_error_handlers()
        {
            var promise = new Promise<int>();

            var ex = new ApplicationException();
            var completed1 = 0;
            var completed2 = 0;
            promise.Catch(e => 
            {
                Assert.Equal(ex, e);
                ++completed1;
            });
            promise.Catch(e =>
            {
                Assert.Equal(ex, e);
                ++completed2;
            });

            promise.Reject(ex);

            Assert.Equal(1, completed1);
            Assert.Equal(1, completed2);
        }

        [Fact]
        public void can_reject_promise_and_trigger_error_handler_with_registration_after_reject()
        {
            var promise = new Promise<int>();

            var ex = new ApplicationException();
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
        public void can_reject_promise_and_trigger_multiple_error_handlers_with_registration_after_reject()
        {
            var promise = new Promise<int>();

            var ex = new ApplicationException();
            promise.Reject(ex);

            var completed1 = 0;
            var completed2 = 0;
            promise.Catch(e =>
            {
                Assert.Equal(ex, e);
                ++completed1;
            });
            promise.Catch(e =>
            {
                Assert.Equal(ex, e);
                ++completed2;
            });

            Assert.Equal(1, completed1);
            Assert.Equal(1, completed2);
        }

        [Fact]
        public void error_handler_is_not_invoked_for_resolved_promised()
        {
            var promise = new Promise<int>();

            promise.Catch(e =>
            {
                throw new ApplicationException("This shouldn't happen");
            });

            promise.Resolve(5);
        }

        [Fact]
        public void completed_handler_is_not_invoked_for_rejected_promise()
        {
            var promise = new Promise<int>();

            promise.Done(() =>
            {
                throw new ApplicationException("This shouldn't happen");
            });
            promise.Done(v =>
            {
                throw new ApplicationException("This shouldn't happen");
            });

            promise.Reject(new ApplicationException("Rejection!"));
        }

        [Fact]
        public void chain_multiple_promises()
        {
            var promise = new Promise<string>();
            var chainedPromise1 = new Promise<int>();
            var chainedPromise2 = new Promise<int>();
            var chainedResult1 = 10;
            var chainedResult2 = 15;

            var completed = 0;

            promise
                .ThenAll(i => LinqExts.FromItems(chainedPromise1, chainedPromise2).Cast<IPromise<int>>())
                .Done(result =>
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
        }


        [Fact]
        public void chain_multiple_promises_resolved_out_of_order()
        {
            var promise = new Promise<string>();
            var chainedPromise1 = new Promise<int>();
            var chainedPromise2 = new Promise<int>();
            var chainedResult1 = 10;
            var chainedResult2 = 15;

            var completed = 0;

            promise
                .ThenAll(i => LinqExts.FromItems(chainedPromise1, chainedPromise2).Cast<IPromise<int>>())
                .Done(result =>
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
        }

        [Fact]
        public void chain_multiple_promises_and_convert_to_non_value_promise()
        {
            var promise = new Promise<string>();
            var chainedPromise1 = new Promise();
            var chainedPromise2 = new Promise();

            var completed = 0;

            promise
                .ThenAll(i => LinqExts.FromItems(chainedPromise1, chainedPromise2).Cast<IPromise>())
                .Done(() =>
                {
                    ++completed;
                });

            Assert.Equal(0, completed);

            promise.Resolve("hello");

            Assert.Equal(0, completed);

            chainedPromise1.Resolve();

            Assert.Equal(0, completed);

            chainedPromise2.Resolve();

            Assert.Equal(1, completed);
        }

        [Fact]
        public void combined_promise_is_resolved_when_children_are_resolved()
        {
            var promise1 = new Promise<int>();
            var promise2 = new Promise<int>();

            var all = Promise<int>.All(LinqExts.FromItems<IPromise<int>>(promise1, promise2));

            var completed = 0;

            all.Done(v =>
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
        }

        [Fact]
        public void combined_promise_is_rejected_when_first_promise_is_rejected()
        {
            var promise1 = new Promise<int>();
            var promise2 = new Promise<int>();

            var all = Promise<int>.All(LinqExts.FromItems<IPromise<int>>(promise1, promise2));

            all.Done(() =>
            {
                throw new ApplicationException("Shouldn't happen");
            });
            all.Done(v =>
            {
                throw new ApplicationException("Shouldn't happen");
            });

            var errors = 0;
            all.Catch(e =>
            {
                ++errors;
            });

            promise1.Reject(new ApplicationException("Error!"));
            promise2.Resolve(2);

            Assert.Equal(1, errors);
        }

        [Fact]
        public void combined_promise_is_rejected_when_second_promise_is_rejected()
        {
            var promise1 = new Promise<int>();
            var promise2 = new Promise<int>();

            var all = Promise<int>.All(LinqExts.FromItems<IPromise<int>>(promise1, promise2));

            all.Done(() =>
            {
                throw new ApplicationException("Shouldn't happen");
            });
            all.Done(v =>
            {
                throw new ApplicationException("Shouldn't happen");
            });

            var errors = 0;
            all.Catch(e =>
            {
                ++errors;
            });

            promise1.Resolve(2);
            promise2.Reject(new ApplicationException("Error!"));

            Assert.Equal(1, errors);
        }

        [Fact]
        public void combined_promise_is_rejected_when_both_promises_are_rejected()
        {
            var promise1 = new Promise<int>();
            var promise2 = new Promise<int>();

            var all = Promise<int>.All(LinqExts.FromItems<IPromise<int>>(promise1, promise2));

            all.Done(() =>
            {
                throw new ApplicationException("Shouldn't happen");
            });
            all.Done(v =>
            {
                throw new ApplicationException("Shouldn't happen");
            });

            var errors = 0;
            all.Catch(e =>
            {
                ++errors;
            });

            promise1.Reject(new ApplicationException("Error!"));
            promise2.Reject(new ApplicationException("Error!"));

            Assert.Equal(1, errors);
        }

        [Fact]
        public void combined_promise_is_resolved_if_there_are_no_children()
        {
            var promise1 = new Promise<int>();
            var promise2 = new Promise<int>();

            var all = Promise<int>.All(LinqExts.Empty<IPromise<int>>());

            var completed = 0;

            all.Done(v =>
            {
                ++completed;

                Assert.Empty(v);
            });

            Assert.Equal(1, completed);
        }


        [Fact]
        public void can_transform_promise_value()
        {
            var promise = new Promise<int>();

            var promisedValue = 15;
            var completed = 0;

            promise
                .Transform(v => v.ToString())
                .Done(v =>
                {
                    Assert.Equal(promisedValue.ToString(), v);

                    ++completed;
                });

            promise.Resolve(promisedValue);

            Assert.Equal(1, completed);           
        }

        [Fact]
        public void rejection_of_source_promise_rejects_resulting_promise()
        {
            var promise = new Promise<int>();

            var ex = new Exception();
            var errors = 0;

            var transformedPromise = promise
                .Transform(v => v.ToString())
                .Catch(e =>
                {
                    Assert.Equal(ex, e);

                    ++errors;
                });

            promise.Reject(ex);

            Assert.Equal(1, errors);
        }

        [Fact]
        public void exception_thrown_during_transform_rejects_promise()
        {
            var promise = new Promise<int>();

            var promisedValue = 15;
            var errors = 0;
            var ex = new Exception();

            var transformedPromise = promise
                .Transform<string>(v => 
                {
                    throw ex;
                })
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

            var promisedValue = 15;
            var chainedPromiseValue = "blah";
            var completed = 0;

            promise
                .Then(v => chainedPromise)
                .Done(v =>
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

            var promisedValue = 15;
            var completed = 0;

            promise
                .Then(v => chainedPromise)
                .Done(() =>
                {
                    ++completed;
                });

            promise.Resolve(promisedValue);
            chainedPromise.Resolve();

            Assert.Equal(1, completed);
        }

        [Fact]
        public void exception_thrown_in_chain_rejects_resulting_promise()
        {
            var promise = new Promise<int>();
            var chainedPromise = new Promise<string>();

            var ex = new Exception();
            var errors = 0;

            var transformedPromise = promise
                .Then<IPromise<string>>(v =>
                {
                    throw ex;
                })
                .Catch(e =>
                {
                    Assert.Equal(ex, e);

                    ++errors;
                });

            promise.Resolve(15);

            Assert.Equal(1, errors);
        }

        [Fact]
        public void rejection_of_source_promises_rejects_resulting_promise()
        {
            var promise = new Promise<int>();
            var chainedPromise = new Promise<string>();

            var ex = new Exception();
            var errors = 0;

            var transformedPromise = promise
                .Then(v => chainedPromise)
                .Catch(e =>
                {
                    Assert.Equal(ex, e);

                    ++errors;
                });

            promise.Reject(ex);

            Assert.Equal(1, errors);
        }

        [Fact]
        public void rejection_of_chained_promises_rejects_resulting_promise()
        {
            var promise = new Promise<int>();
            var chainedPromise = new Promise<string>();

            var ex = new Exception();
            var errors = 0;

            var transformedPromise = promise
                .Then(v => chainedPromise)
                .Catch(e =>
                {
                    Assert.Equal(ex, e);

                    ++errors;
                });

            promise.Resolve(5);
            chainedPromise.Reject(ex);

            Assert.Equal(1, errors);
        }

        [Fact]
        public void can_invoke_do_callback()
        {
            var promise = new Promise<int>();
            var invoked = 0;
            promise.ThenDo(i => ++invoked);

            promise.Resolve(5);

            Assert.Equal(1, invoked);
        }

        [Fact]
        public void can_invoke_multiple_do_callbacks_in_order()
        {
            var promise = new Promise<int>();
            var order = 0;
            promise
                .ThenDo(i => Assert.Equal(1, ++order))
                .ThenDo(i => Assert.Equal(2, ++order))
                .ThenDo(i => Assert.Equal(3, ++order));

            promise.Resolve(5);

            Assert.Equal(3, order);
        }

        [Fact]
        public void do_callback_is_not_invoked_when_promise_is_rejected()
        {
            var promise = new Promise<int>();
            var invoked = 0;
            promise.ThenDo(i => ++invoked);

            promise.Reject(new ApplicationException());

            Assert.Equal(0, invoked);
        }

        [Fact]
        public void race_is_resolved_when_first_promise_is_resolved_first()
        {
            var promise1 = new Promise<int>();
            var promise2 = new Promise<int>();

            var resolved = 0;

            Promise<int>
                .Race(promise1, promise2)
                .Done(i => resolved = i);

            promise1.Resolve(5);

            Assert.Equal(5, resolved);
        }

        [Fact]
        public void race_is_resolved_when_second_promise_is_resolved_first()
        {
            var promise1 = new Promise<int>();
            var promise2 = new Promise<int>();

            var resolved = 0;

            Promise<int>
                .Race(promise1, promise2)
                .Done(i => resolved = i);

            promise2.Resolve(12);

            Assert.Equal(12, resolved);
        }

        [Fact]
        public void race_is_rejected_when_first_promise_is_rejected_first()
        {
            var promise1 = new Promise<int>();
            var promise2 = new Promise<int>();

            Exception ex = null;

            Promise<int>
                .Race(promise1, promise2)
                .Catch(e => ex = e);

            var expected = new Exception();
            promise1.Reject(expected);

            Assert.Equal(expected, ex);
        }

        [Fact]
        public void race_is_rejected_when_second_promise_is_rejected_first()
        {
            var promise1 = new Promise<int>();
            var promise2 = new Promise<int>();

            Exception ex = null;

            Promise<int>
                .Race(promise1, promise2)
                .Catch(e => ex = e);

            var expected = new Exception();
            promise2.Reject(expected);

            Assert.Equal(expected, ex);
        }

        [Fact]
        public void can_resolve_promise_via_resolver_function()
        {
            var promise = new Promise<int>((resolve, reject) =>
            {
                resolve(5);
            });

            var completed = 0;
            promise.Done(v => 
            {
                Assert.Equal(5, v);
                ++completed;
            });

            Assert.Equal(1, completed);
        }

        [Fact]
        public void can_reject_promise_via_resolver_function()
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
        public void exception_thrown_during_resolver_rejects_proimse()
        {
            var ex = new Exception();
            var promise = new Promise<int>((resolve, reject) =>
            {
                throw ex;
            });

            var completed = 0;
            promise.Catch(e =>
            {
                Assert.Equal(ex, e);
                ++completed;
            });

            Assert.Equal(1, completed);
        }
    }
}
