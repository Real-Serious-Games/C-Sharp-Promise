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
        /// Handle completion of the promise.
        /// </summary>
        void Done(Action onCompleted);

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
        IPromise ThenDo(Action action);

        /// <summary>
        /// Chains another asynchronous operation that yields multiple promises.
        /// Converts to a single promise.
        /// </summary>
        IPromise ThenAll(Func<IEnumerable<IPromise>> chain);

        /// <summary>
        /// Chains another asynchronous operation that yields multiple chained promises.
        /// Converts to a single promist that yields values.
        /// </summary>
        IPromise<IEnumerable<ConvertedT>> ThenAll<ConvertedT>(Func<IEnumerable<IPromise<ConvertedT>>> chain);

        /// <summary>
        /// Returns a promise that resolves when all of the promises in the enumerable argument have resolved.
        /// Returns a promise of a collection of the resolved results.
        /// </summary>
        IPromise ThenAll(params IPromise[] promises);

        /// <summary>
        /// Returns a promise that resolves when all of the promises in the enumerable argument have resolved.
        /// Returns a promise of a collection of the resolved results.
        /// </summary>
        IPromise ThenAll(IEnumerable<IPromise> promises);

        /// <summary>
        /// Chain a number of operations using promises.
        /// Takes a number of functions each of which starts an async operation and yields a promise.
        /// </summary>
        IPromise ThenSequence(params Func<IPromise>[] fns);

        /// <summary>
        /// Chain a sequence of operations using promises.
        /// Takes a collection of functions each of which starts an async operation and yields a promise.
        /// </summary>
        IPromise ThenSequence(IEnumerable<Func<IPromise>> fns);

        /// <summary>
        /// Takes a function that yields an enumerable of promises.
        /// Returns a promise that resolves when the first of the promises has resolved.
        /// </summary>
        IPromise ThenRace(Func<IEnumerable<IPromise>> chain); //todo: totest
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
        /// Handle completion of the promise.
        /// </summary>
        public void Done(Action onCompleted)
        {
            Argument.NotNull(() => onCompleted);

            if (CurState == PromiseState.Pending)
            {
                // Promise is in flight, queue handler for possible call later.
                if (completedHandlers == null)
                {
                    completedHandlers = new List<Action>();
                }
                completedHandlers.Add(onCompleted);
            }
            else if (CurState == PromiseState.Resolved)
            {
                // Promise has already been resolved, immediately call handler.
                onCompleted();
            }
        }

        /// <summary>
        /// Chains another asynchronous operation. 
        /// </summary>
        public IPromise Then(Func<IPromise> chain)
        {
            Argument.NotNull(() => chain);

            var resultPromise = new Promise();
            
            Catch(e => resultPromise.Reject(e));
            Done(() =>
            {
                try
                {
                    var chainedPromise = chain();
                    chainedPromise.Catch(e => resultPromise.Reject(e));
                    chainedPromise.Done(() => resultPromise.Resolve());
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

            Catch(e => resultPromise.Reject(e));
            Done(() =>
            {
                try
                {
                    var chainedPromise = chain();
                    chainedPromise.Catch(e => resultPromise.Reject(e));
                    chainedPromise.Done(chainedValue => resultPromise.Resolve(chainedValue));
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
        public IPromise ThenDo(Action action)
        {
            return Then(() =>
            {
                action();
                return this;
            });
        }

        /// <summary>
        /// Chains another asynchronous operation that yields multiple promises.
        /// Converts to a single promise.
        /// </summary>
        public IPromise ThenAll(Func<IEnumerable<IPromise>> chain)
        {
            Argument.NotNull(() => chain);

            var resultPromise = new Promise();

            Catch(e => resultPromise.Reject(e));
            Done(() =>
            {
                try
                {
                    var chainedPromise = Promise.All(chain());
                    chainedPromise.Catch(e => resultPromise.Reject(e));
                    chainedPromise.Done(() => resultPromise.Resolve());
                }
                catch (Exception ex)
                {
                    resultPromise.Reject(ex);
                }
            });

            return resultPromise;
        }

        /// <summary>
        /// Chains another asynchronous operation that yields multiple chained promises.
        /// Converts to a single promist that yields values.
        /// </summary>
        public IPromise<IEnumerable<ConvertedT>> ThenAll<ConvertedT>(Func<IEnumerable<IPromise<ConvertedT>>> chain)
        {
            Argument.NotNull(() => chain);

            var resultPromise = new Promise<IEnumerable<ConvertedT>>();

            Catch(e => resultPromise.Reject(e));
            Done(() =>
            {
                try
                {
                    var chainedPromise = Promise<ConvertedT>.All(chain());
                    chainedPromise.Catch(e => resultPromise.Reject(e));
                    chainedPromise.Done(chainedValue => resultPromise.Resolve(chainedValue));
                }
                catch (Exception ex)
                {
                    resultPromise.Reject(ex);
                }
            });

            return resultPromise;
        }

        /// <summary>
        /// Returns a promise that resolves when all of the promises in the enumerable argument have resolved.
        /// Returns a promise of a collection of the resolved results.
        /// </summary>
        public IPromise ThenAll(params IPromise[] promises)
        {
            return ThenAll((IEnumerable<IPromise>)promises); // Cast is required to force use of the other All function.
        }

        /// <summary>
        /// Returns a promise that resolves when all of the promises in the enumerable argument have resolved.
        /// Returns a promise of a collection of the resolved results.
        /// </summary>
        public IPromise ThenAll(IEnumerable<IPromise> promises)
        {
            return Promise.All(promises);
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
                    .Catch(ex => {
                        if (resultPromise.CurState == PromiseState.Pending)
                        {
                            // If a promise errorred and the result promise is still pending, reject it.
                            resultPromise.Reject(ex);
                        }
                    })
                    .Done(() =>                     
                    {
                        --remainingCount;
                        if (remainingCount <= 0)
                        {
                            // This will never happen if any of the promises errorred.
                            resultPromise.Resolve();
                        }
                    });
            });

            return resultPromise;
        }

        /// <summary>
        /// Chain a number of operations using promises.
        /// Takes a number of functions each of which starts an async operation and yields a promise.
        /// </summary>
        public IPromise ThenSequence(params Func<IPromise>[] fns)
        {
            return ThenSequence((IEnumerable<Func<IPromise>>)fns);
        }

        /// <summary>
        /// Chain a sequence of operations using promises.
        /// Takes a collection of functions each of which starts an async operation and yields a promise.
        /// </summary>
        public IPromise ThenSequence(IEnumerable<Func<IPromise>> fns)
        {
            return Then(() => Sequence(fns));
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
            return fns
                .Aggregate(
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
        public IPromise ThenRace(Func<IEnumerable<IPromise>> chain) //todo: totest
        {
            return Then(() => Promise.Race(chain()));
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
        public static IPromise Race(IEnumerable<IPromise> promises) //todo: totest
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
                    .Done(() =>
                    {
                        if (resultPromise.CurState == PromiseState.Pending)
                        {
                            resultPromise.Resolve();
                        }
                    });
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