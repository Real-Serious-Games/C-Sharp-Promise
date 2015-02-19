using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RSG.Promise
{
    /// <summary>
    /// Implements a C# promise.
    /// https://developer.mozilla.org/en/docs/Web/JavaScript/Reference/Global_Objects/Promise
    /// </summary>
    public interface IPromise<PromisedT>
    {
        /// <summary>
        /// Catch any execption that is thrown while the promise is being resolved.
        /// </summary>
        IPromise<PromisedT> Catch(Action<Exception> onError);

        /// <summary>
        /// Complete the promise. Adds a defualt error handler.
        /// </summary>
        void Done();

        /// <summary>
        /// Chains another asynchronous operation. 
        /// May also change the type of value that is being fulfilled.
        /// </summary>
        IPromise<ConvertedT> Then<ConvertedT>(Func<PromisedT, IPromise<ConvertedT>> chain);

        /// <summary>
        /// Chains another asynchronous operation. 
        /// Converts to a promisse that has no result.
        /// </summary>
        IPromise Then(Func<PromisedT, IPromise> chain);

        /// <summary>
        /// Return a new promise with a different value.
        /// May also change the type of the value.
        /// </summary>
        IPromise<ConvertedT> Transform<ConvertedT>(Func<PromisedT, ConvertedT> transform);

        /// <summary>
        /// Chain a synchronous action.
        /// The callback receives the promised value and returns no value.
        /// The callback is invoked when the promise is resolved, after the callback the chain continues.
        /// </summary>
        IPromise<PromisedT> Then(Action<PromisedT> action);

        /// <summary>
        /// Chain an enumerable of promises, all of which must resolve.
        /// Returns a promise for a collection of the resolved results.
        /// The resulting promise is resolved when all of the promises have resolved.
        /// It is rejected as soon as any of the promises have been rejected.
        /// </summary>
        IPromise<IEnumerable<ConvertedT>> ThenAll<ConvertedT>(Func<PromisedT, IEnumerable<IPromise<ConvertedT>>> chain);

        /// <summary>
        /// Chain an enumerable of promises, all of which must resolve.
        /// Converts to a non-value promise.
        /// The resulting promise is resolved when all of the promises have resolved.
        /// It is rejected as soon as any of the promises have been rejected.
        /// </summary>
        IPromise ThenAll(Func<PromisedT, IEnumerable<IPromise>> chain);

        /// <summary>
        /// Takes a function that yields an enumerable of promises.
        /// Returns a promise that resolves when the first of the promises has resolved.
        /// Yields the value from the first promise that has resolved.
        /// </summary>
        IPromise<ConvertedT> ThenRace<ConvertedT>(Func<PromisedT, IEnumerable<IPromise<ConvertedT>>> chain);

        /// <summary>
        /// Takes a function that yields an enumerable of promises.
        /// Converts to a non-value promise.
        /// Returns a promise that resolves when the first of the promises has resolved.
        /// Yields the value from the first promise that has resolved.
        /// </summary>
        IPromise ThenRace(Func<PromisedT, IEnumerable<IPromise>> chain);
    }

    /// <summary>
    /// Interface for a promise that can be rejected or resolved.
    /// </summary>
    public interface IPendingPromise<PromisedT>
    {
        /// <summary>
        /// Reject the promise with an exception.
        /// </summary>
        void Reject(Exception ex);

        /// <summary>
        /// Resolve the promise with a particular value.
        /// </summary>
        void Resolve(PromisedT value);
    }

    /// <summary>
    /// Specifies the state of a promise.
    /// </summary>
    public enum PromiseState
    {
        Pending,    // The promise is in-flight.
        Rejected,   // The promise has been rejected.
        Resolved    // The promise has been resolved.
    };

    /// <summary>
    /// Implements a C# promise.
    /// https://developer.mozilla.org/en/docs/Web/JavaScript/Reference/Global_Objects/Promise
    /// </summary>
    public class Promise<PromisedT> : IPromise<PromisedT>, IPendingPromise<PromisedT>
    {
        /// <summary>
        /// The exception when the promise is rejected.
        /// </summary>
        private Exception rejectionException;

        /// <summary>
        /// The value when the promises is resolved.
        /// </summary>
        private PromisedT resolveValue;

        /// <summary>
        /// Error handler.
        /// </summary>
        private List<Action<Exception>> errorHandlers;

        /// <summary>
        /// Completed handlers that accept a value.
        /// </summary>
        private List<Action<PromisedT>> valueCompletedHandlers;

        /// <summary>
        /// Tracks the current state of the promise.
        /// </summary>
        public PromiseState CurState { get; private set; }

        public Promise()
        {
            this.CurState = PromiseState.Pending;
        }

        public Promise(Action<Action<PromisedT>, Action<Exception>> resolver)
        {
            this.CurState = PromiseState.Pending;

            try
            {
                resolver(
                    // Resolve
                    value => Resolve(value),

                    // Reject
                    ex => Reject(ex)
                );
            }
            catch (Exception ex)
            {
                Reject(ex);
            }
        }

        /// <summary>
        /// Helper function clear out all handlers after resolution or rejection.
        /// </summary>
        private void ClearHandlers()
        {
            errorHandlers = null;
            valueCompletedHandlers = null;
        }

        /// <summary>
        /// Reject the promise with an exception.
        /// </summary>
        public void Reject(Exception ex)
        {
            Argument.NotNull(() => ex);

            if (CurState != PromiseState.Pending)
            {
                throw new ApplicationException("Attempt to reject a promise that is already in state: " + CurState + ", a promise can only be rejected when it is still in state: " + PromiseState.Pending);
            }

            rejectionException = ex;

            CurState = PromiseState.Rejected;

            if (errorHandlers != null)
            {
                errorHandlers.Each(handler => handler(rejectionException));
            }

            ClearHandlers();
        }


        /// <summary>
        /// Resolve the promise with a particular value.
        /// </summary>
        public void Resolve(PromisedT value)
        {
            if (CurState != PromiseState.Pending)
            {
                throw new ApplicationException("Attempt to resolve a promise that is already in state: " + CurState + ", a promise can only be resolved when it is still in state: " + PromiseState.Pending);
            }

            resolveValue = value;

            CurState = PromiseState.Resolved;

            if (valueCompletedHandlers != null)
            {
                valueCompletedHandlers.Each(handler => handler(resolveValue));
            }
           
            ClearHandlers();
        }

        /// <summary>
        /// Catch any execption that is thrown while the promise is being resolved.
        /// </summary>
        public IPromise<PromisedT> Catch(Action<Exception> onError)
        {
            Argument.NotNull(() => onError);

            if (CurState == PromiseState.Pending)
            {
                // Promise is in flight, queue handler for possible call later.
                if (errorHandlers == null)
                {
                    errorHandlers = new List<Action<Exception>>();
                }

                errorHandlers.Add(onError);
            }
            else if (CurState == PromiseState.Rejected)
            {
                // Promise has already been rejected, immediately call handler.
                onError(rejectionException);
            }

            return this;
        }

        /// <summary>
        /// Complete the promise. Adds a default error handler.
        /// </summary>
        public void Done()
        {
        }

        /// <summary>
        /// Chains another asynchronous operation. 
        /// May also change the type of value that is being fulfilled.
        /// </summary>
        public IPromise<ConvertedT> Then<ConvertedT>(Func<PromisedT, IPromise<ConvertedT>> chain)
        {
            Argument.NotNull(() => chain);

            var resultPromise = new Promise<ConvertedT>();

            this.Catch(e => resultPromise.Reject(e))
                .Then(v =>
                {
                    try
                    {
                        chain(v)
                            .Catch(e => resultPromise.Reject(e))
                            .Then(chainedValue => resultPromise.Resolve(chainedValue))
                            .Done();
                    }
                    catch (Exception ex)
                    {
                        resultPromise.Reject(ex);
                    }
                });
            
            return resultPromise;
        }

        /// <summary>
        /// Chains another asynchronous operation. 
        /// Converts to a promisse that has no result.
        /// </summary>
        public IPromise Then(Func<PromisedT, IPromise> chain)
        {
            Argument.NotNull(() => chain);

            var resultPromise = new Promise();

            this.Catch(e => resultPromise.Reject(e))
                .Then(v =>
                {
                    try
                    {
                        chain(v)
                            .Catch(e => resultPromise.Reject(e))
                            .Then(() => resultPromise.Resolve())
                            .Done();
                    }
                    catch (Exception ex)
                    {
                        resultPromise.Reject(ex);
                    }
                });

            return resultPromise;
        }

        /// <summary>
        /// Return a new promise with a different value.
        /// May also change the type of the value.
        /// </summary>
        public IPromise<ConvertedT> Transform<ConvertedT>(Func<PromisedT, ConvertedT> transform)
        {
            Argument.NotNull(() => transform);

            var resultPromise = new Promise<ConvertedT>();

            this.Catch(e => resultPromise.Reject(e))
                .Then(v =>
                {
                    try
                    {
                        var transformedValue = transform(v);
                        resultPromise.Resolve(transformedValue);
                    }
                    catch (Exception ex)
                    {
                        resultPromise.Reject(ex);
                    }
                });

            return resultPromise;
        }

        /// <summary>
        /// Chain a synchronous action.
        /// The callback receives the promised value and returns no value.
        /// The callback is invoked when the promise is resolved, after the callback the chain continues.
        /// </summary>
        public IPromise<PromisedT> Then(Action<PromisedT> action)
        {
            Argument.NotNull(() => action);

            if (CurState == PromiseState.Pending)
            {
                // Promise is in flight, queue handler for possible call later.
                if (valueCompletedHandlers == null)
                {
                    valueCompletedHandlers = new List<Action<PromisedT>>();
                }
                valueCompletedHandlers.Add(action);
            }
            else if (CurState == PromiseState.Resolved)
            {
                // Promise has already been rejected, immediately call handler.
                action(resolveValue);
            }
            return this;
        }

        /// <summary>
        /// Chain an enumerable of promises, all of which must resolve.
        /// Returns a promise for a collection of the resolved results.
        /// The resulting promise is resolved when all of the promises have resolved.
        /// It is rejected as soon as any of the promises have been rejected.
        /// </summary>
        public IPromise<IEnumerable<ConvertedT>> ThenAll<ConvertedT>(Func<PromisedT, IEnumerable<IPromise<ConvertedT>>> chain)
        {
            return Then(value => Promise<ConvertedT>.All(chain(value)));
        }

        /// <summary>
        /// Chain an enumerable of promises, all of which must resolve.
        /// Converts to a non-value promise.
        /// The resulting promise is resolved when all of the promises have resolved.
        /// It is rejected as soon as any of the promises have been rejected.
        /// </summary>
        public IPromise ThenAll(Func<PromisedT, IEnumerable<IPromise>> chain)
        {
            return Then(value => Promise.All(chain(value)));
        }

        /// <summary>
        /// Returns a promise that resolves when all of the promises in the enumerable argument have resolved.
        /// Returns a promise of a collection of the resolved results.
        /// </summary>
        public static IPromise<IEnumerable<PromisedT>> All(params IPromise<PromisedT>[] promises)
        {
            return All((IEnumerable<IPromise<PromisedT>>)promises); // Cast is required to force use of the other All function.
        }

        /// <summary>
        /// Returns a promise that resolves when all of the promises in the enumerable argument have resolved.
        /// Returns a promise of a collection of the resolved results.
        /// </summary>
        public static IPromise<IEnumerable<PromisedT>> All(IEnumerable<IPromise<PromisedT>> promises)
        {
            var promisesArray = promises.ToArray();
            if (promisesArray.Length == 0)
            {
                return Promise<IEnumerable<PromisedT>>.Resolved(LinqExts.Empty<PromisedT>());
            }

            var remainingCount = promisesArray.Length;
            var results = new PromisedT[remainingCount];
            var resultPromise = new Promise<IEnumerable<PromisedT>>();

            promisesArray.Each((promise, index) =>
            {
                promise
                    .Catch(ex =>
                    {
                        if (resultPromise.CurState == PromiseState.Pending)
                        {
                            // If a promise errorred and the result promise is still pending, reject it.
                            resultPromise.Reject(ex);
                        }
                    })
                    .Then(result =>
                    {
                        results[index] = result;

                        --remainingCount;
                        if (remainingCount <= 0)
                        {
                            // This will never happen if any of the promises errorred.
                            resultPromise.Resolve(results);
                        }
                    })
                    .Done();
            });

            return resultPromise;
        }

        /// <summary>
        /// Takes a function that yields an enumerable of promises.
        /// Returns a promise that resolves when the first of the promises has resolved.
        /// Yields the value from the first promise that has resolved.
        /// </summary>
        public IPromise<ConvertedT> ThenRace<ConvertedT>(Func<PromisedT, IEnumerable<IPromise<ConvertedT>>> chain)
        {
            return Then(value => Promise<ConvertedT>.Race(chain(value)));
        }

        /// <summary>
        /// Takes a function that yields an enumerable of promises.
        /// Converts to a non-value promise.
        /// Returns a promise that resolves when the first of the promises has resolved.
        /// Yields the value from the first promise that has resolved.
        /// </summary>
        public IPromise ThenRace(Func<PromisedT, IEnumerable<IPromise>> chain)
        {
            return Then(value => Promise.Race(chain(value)));
        }

        /// <summary>
        /// Returns a promise that resolves when the first of the promises in the enumerable argument have resolved.
        /// Returns the value from the first promise that has resolved.
        /// </summary>
        public static IPromise<PromisedT> Race(params IPromise<PromisedT>[] promises)
        {
            return Race((IEnumerable<IPromise<PromisedT>>)promises); // Cast is required to force use of the other function.
        }

        /// <summary>
        /// Returns a promise that resolves when the first of the promises in the enumerable argument have resolved.
        /// Returns the value from the first promise that has resolved.
        /// </summary>
        public static IPromise<PromisedT> Race(IEnumerable<IPromise<PromisedT>> promises)
        {
            var promisesArray = promises.ToArray();
            if (promisesArray.Length == 0)
            {
                throw new ApplicationException("At least 1 input promise must be provided for Race");
            }

            var resultPromise = new Promise<PromisedT>();

            promisesArray.Each((promise, index) =>
            {
                promise
                    .Catch(ex =>
                    {
                        if (resultPromise.CurState == PromiseState.Pending)
                        {
                            // If a promise errorred and the result promise is still pending, reject it.
                            resultPromise.Reject(ex);
                        }
                    })
                    .Then(result =>
                    {
                        if (resultPromise.CurState == PromiseState.Pending)
                        {
                            resultPromise.Resolve(result);
                        }
                    })
                    .Done();
            });

            return resultPromise;
        }

        /// <summary>
        /// Convert a simple value directly into a resolved promise.
        /// </summary>
        public static IPromise<PromisedT> Resolved(PromisedT promisedValue)
        {
            var promise = new Promise<PromisedT>();
            promise.Resolve(promisedValue);
            return promise;
        }

        /// <summary>
        /// Convert an exception directly into a rejected promise.
        /// </summary>
        public static IPromise<PromisedT> Rejected(Exception ex)
        {
            Argument.NotNull(() => ex);

            var promise = new Promise<PromisedT>();
            promise.Reject(ex);
            return promise;
        }
    }
}