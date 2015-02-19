using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RSG.Promise
{
    /// <summary>
    /// Implements a non-generic C# promise, this is a promise that simply resolves without delivering a value.
    /// https://developer.mozilla.org/en/docs/Web/JavaScript/Reference/Global_Objects/Promise
    /// </summary>
    public interface IPromise
    {
        /// <summary>
        /// Catch any execption that is thrown while the promise is being resolved.
        /// </summary>
        IPromise Catch(Action<Exception> onError);

        /// <summary>
        /// Complete the promise. Adds a defualt error handler.
        /// </summary>
        void Done();

        /// <summary>
        /// Chains another asynchronous operation. 
        /// May also change the type of value that is being fulfilled.
        /// </summary>
        IPromise Then(Func<IPromise> chain);

        /// <summary>
        /// Chains another asynchronous operation. 
        /// May convert to a promise that yields a value.
        /// </summary>
        IPromise<ConvertedT> Then<ConvertedT>(Func<IPromise<ConvertedT>> chain);

        /// <summary>
        /// Chain a synchronous action.
        /// The callback receives the promised value and returns no value.
        /// The callback is invoked when the promise is resolved, after the callback the chain continues.
        /// </summary>
        IPromise Then(Action action);

        /// <summary>
        /// Chain an enumerable of promises, all of which must resolve.
        /// The resulting promise is resolved when all of the promises have resolved.
        /// It is rejected as soon as any of the promises have been rejected.
        /// </summary>
        IPromise ThenAll(Func<IEnumerable<IPromise>> chain);

        /// <summary>
        /// Chain an enumerable of promises, all of which must resolve.
        /// Converts to a non-value promise.
        /// The resulting promise is resolved when all of the promises have resolved.
        /// It is rejected as soon as any of the promises have been rejected.
        /// </summary>
        IPromise<IEnumerable<ConvertedT>> ThenAll<ConvertedT>(Func<IEnumerable<IPromise<ConvertedT>>> chain);

        /// <summary>
        /// Chain a sequence of operations using promises.
        /// Reutrn a collection of functions each of which starts an async operation and yields a promise.
        /// Each function will be called and each promise resolved in turn.
        /// The resulting promise is resolved after each promise is resolved in sequence.
        /// </summary>
        IPromise ThenSequence(Func<IEnumerable<Func<IPromise>>> chain);

        /// <summary>
        /// Takes a function that yields an enumerable of promises.
        /// Returns a promise that resolves when the first of the promises has resolved.
        /// </summary>
        IPromise ThenRace(Func<IEnumerable<IPromise>> chain);

        /// <summary>
        /// Takes a function that yields an enumerable of promises.
        /// Converts to a value promise.
        /// Returns a promise that resolves when the first of the promises has resolved.
        /// </summary>
        IPromise<ConvertedT> ThenRace<ConvertedT>(Func<IEnumerable<IPromise<ConvertedT>>> chain);
    }

    /// <summary>
    /// Interface for a promise that can be rejected or resolved.
    /// </summary>
    public interface IPendingPromise
    {
        /// <summary>
        /// Reject the promise with an exception.
        /// </summary>
        void Reject(Exception ex);

        /// <summary>
        /// Resolve the promise with a particular value.
        /// </summary>
        void Resolve();
    }

    /// <summary>
    /// Implements a non-generic C# promise, this is a promise that simply resolves without delivering a value.
    /// https://developer.mozilla.org/en/docs/Web/JavaScript/Reference/Global_Objects/Promise
    /// </summary>
    public class Promise : IPromise, IPendingPromise
    {
        /// <summary>
        /// The exception when the promise is rejected.
        /// </summary>
        private Exception rejectionException;

        /// <summary>
        /// Error handlers.
        /// </summary>
        private List<Action<Exception>> errorHandlers;

        /// <summary>
        /// Completed handlers that accept no value.
        /// </summary>
        private List<Action> completedHandlers;

        /// <summary>
        /// Tracks the current state of the promise.
        /// </summary>
        public PromiseState CurState { get; private set; }

        public Promise()
        {
            this.CurState = PromiseState.Pending;
        }

        public Promise(Action<Action, Action<Exception>> resolver)
        {
            this.CurState = PromiseState.Pending;

            try
            {
                resolver(
                    // Resolve
                    () => Resolve(),

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
            completedHandlers = null;
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
        public void Resolve()
        {
            if (CurState != PromiseState.Pending)
            {
                throw new ApplicationException("Attempt to resolve a promise that is already in state: " + CurState + ", a promise can only be resolved when it is still in state: " + PromiseState.Pending);
            }

            CurState = PromiseState.Resolved;

            if (completedHandlers != null)
            {
                completedHandlers.Each(handler => handler());
            }
            
            ClearHandlers();
        }

        /// <summary>
        /// Catch any execption that is thrown while the promise is being resolved.
        /// </summary>
        public IPromise Catch(Action<Exception> onError)
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
        /// Complete the promise. Adds a defualt error handler.
        /// </summary>
        public void Done()
        {
        }

        /// <summary>
        /// Chains another asynchronous operation. 
        /// </summary>
        public IPromise Then(Func<IPromise> chain)
        {
            Argument.NotNull(() => chain);

            var resultPromise = new Promise();

           this.Catch(e => resultPromise.Reject(e))
               .Then(() =>
                {
                    try
                    {
                        chain()
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
        /// Chains another asynchronous operation. 
        /// May convert to a promise that yields a value.
        /// </summary>
        public IPromise<ConvertedT> Then<ConvertedT>(Func<IPromise<ConvertedT>> chain)
        {
            Argument.NotNull(() => chain);

            var resultPromise = new Promise<ConvertedT>();

            this.Catch(e => resultPromise.Reject(e))
                .Then(() =>
                {
                    try
                    {
                        chain()
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
        /// Chain a synchronous action.
        /// The callback receives the promised value and returns no value.
        /// The callback is invoked when the promise is resolved, after the callback the chain continues.
        /// </summary>
        public IPromise Then(Action action)
        {
            Argument.NotNull(() => action);

            if (CurState == PromiseState.Pending)
            {
                // Promise is in flight, queue handler for possible call later.
                if (completedHandlers == null)
                {
                    completedHandlers = new List<Action>();
                }
                completedHandlers.Add(action);
            }
            else if (CurState == PromiseState.Resolved)
            {
                // Promise has already been resolved, immediately call handler.
                action();
            }

            return this;
        }

        /// <summary>
        /// Chain an enumerable of promises, all of which must resolve.
        /// The resulting promise is resolved when all of the promises have resolved.
        /// It is rejected as soon as any of the promises have been rejected.
        /// </summary>
        public IPromise ThenAll(Func<IEnumerable<IPromise>> chain)
        {
            return Then(() => Promise.All(chain()));
        }

        /// <summary>
        /// Chain an enumerable of promises, all of which must resolve.
        /// Converts to a non-value promise.
        /// The resulting promise is resolved when all of the promises have resolved.
        /// It is rejected as soon as any of the promises have been rejected.
        /// </summary>
        public IPromise<IEnumerable<ConvertedT>> ThenAll<ConvertedT>(Func<IEnumerable<IPromise<ConvertedT>>> chain)
        {
            return Then(() => Promise<ConvertedT>.All(chain()));
        }

        /// <summary>
        /// Returns a promise that resolves when all of the promises in the enumerable argument have resolved.
        /// Returns a promise of a collection of the resolved results.
        /// </summary>
        public static IPromise All(params IPromise[] promises)
        {
            return All((IEnumerable<IPromise>)promises); // Cast is required to force use of the other All function.
        }

        /// <summary>
        /// Returns a promise that resolves when all of the promises in the enumerable argument have resolved.
        /// Returns a promise of a collection of the resolved results.
        /// </summary>
        public static IPromise All(IEnumerable<IPromise> promises)
        {
            var promisesArray = promises.ToArray();
            if (promisesArray.Length == 0)
            {
                return Promise.Resolved();
            }

            var remainingCount = promisesArray.Length;
            var resultPromise = new Promise();

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
                    .Then(() =>
                    {
                        --remainingCount;
                        if (remainingCount <= 0)
                        {
                            // This will never happen if any of the promises errorred.
                            resultPromise.Resolve();
                        }
                    })
                    .Done();
            });

            return resultPromise;
        }

        /// <summary>
        /// Chain a sequence of operations using promises.
        /// Reutrn a collection of functions each of which starts an async operation and yields a promise.
        /// Each function will be called and each promise resolved in turn.
        /// The resulting promise is resolved after each promise is resolved in sequence.
        /// </summary>
        public IPromise ThenSequence(Func<IEnumerable<Func<IPromise>>> chain)
        {
            return Then(() => Sequence(chain()));
        }

        /// <summary>
        /// Chain a number of operations using promises.
        /// Takes a number of functions each of which starts an async operation and yields a promise.
        /// </summary>
        public static IPromise Sequence(params Func<IPromise>[] fns)
        {
            return Sequence((IEnumerable<Func<IPromise>>)fns);
        }

        /// <summary>
        /// Chain a sequence of operations using promises.
        /// Takes a collection of functions each of which starts an async operation and yields a promise.
        /// </summary>
        public static IPromise Sequence(IEnumerable<Func<IPromise>> fns)
        {
            return fns.Aggregate(
                Promise.Resolved(),
                (prevPromise, fn) =>
                {
                    return prevPromise.Then(() => fn());
                }
            );
        }

        /// <summary>
        /// Takes a function that yields an enumerable of promises.
        /// Returns a promise that resolves when the first of the promises has resolved.
        /// </summary>
        public IPromise ThenRace(Func<IEnumerable<IPromise>> chain)
        {
            return Then(() => Promise.Race(chain()));
        }

        /// <summary>
        /// Takes a function that yields an enumerable of promises.
        /// Converts to a value promise.
        /// Returns a promise that resolves when the first of the promises has resolved.
        /// </summary>
        public IPromise<ConvertedT> ThenRace<ConvertedT>(Func<IEnumerable<IPromise<ConvertedT>>> chain)
        {
            return Then(() => Promise<ConvertedT>.Race(chain()));
        }

        /// <summary>
        /// Returns a promise that resolves when the first of the promises in the enumerable argument have resolved.
        /// Returns the value from the first promise that has resolved.
        /// </summary>
        public static IPromise Race(params IPromise[] promises)
        {
            return Race((IEnumerable<IPromise>)promises); // Cast is required to force use of the other function.
        }

        /// <summary>
        /// Returns a promise that resolves when the first of the promises in the enumerable argument have resolved.
        /// Returns the value from the first promise that has resolved.
        /// </summary>
        public static IPromise Race(IEnumerable<IPromise> promises)
        {
            var promisesArray = promises.ToArray();
            if (promisesArray.Length == 0)
            {
                throw new ApplicationException("At least 1 input promise must be provided for Race");
            }

            var resultPromise = new Promise();

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
                    .Then(() =>
                    {
                        if (resultPromise.CurState == PromiseState.Pending)
                        {
                            resultPromise.Resolve();
                        }
                    })
                    .Done();
            });

            return resultPromise;
        }

        /// <summary>
        /// Convert a simple value directly into a resolved promise.
        /// </summary>
        public static IPromise Resolved()
        {
            var promise = new Promise();
            promise.Resolve();
            return promise;
        }

        /// <summary>
        /// Convert an exception directly into a rejected promise.
        /// </summary>
        public static IPromise Rejected(Exception ex)
        {
            Argument.NotNull(() => ex);

            var promise = new Promise();
            promise.Reject(ex);
            return promise;
        }
    }
}