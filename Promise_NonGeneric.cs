﻿using RSG.Promises;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RSG
{
	/// <summary>
	/// Implements a non-generic C# promise, this is a promise that simply resolves without delivering a value.
	/// https://developer.mozilla.org/en/docs/Web/JavaScript/Reference/Global_Objects/Promise
	/// </summary>
	public interface IPromise : IPromiseBase
	{
		/// <summary>
		/// Set the name of the promise, useful for debugging.
		/// </summary>
		new IPromise WithName(string name);

		/// <summary>
		/// Completes the promise. 
		/// onResolved is called on successful completion.
		/// onRejected is called on error.
		/// </summary>
		void Done(Action onResolved, Action<Exception> onRejected);

		/// <summary>
		/// Completes the promise. 
		/// onResolved is called on successful completion.
		/// Adds a default error handler.
		/// </summary>
		void Done(Action onResolved);

		/// <summary>
		/// Handle errors for the promise. 
		/// </summary>
		new IPromise Catch(Action<Exception> onRejected);

		/// <summary>
		/// Add a resolved callback that chains a value promise (optionally converting to a different value type).
		/// </summary>
		IPromise<ConvertedT> Then<ConvertedT>(Func<IPromise<ConvertedT>> onResolved);

		/// <summary>
		/// Add a resolved callback that chains a non-value promise.
		/// </summary>
		IPromise Then(Func<IPromise> onResolved);

		/// <summary>
		/// Add a resolved callback.
		/// </summary>
		IPromise Then(Action onResolved);

		/// <summary>
		/// Add a resolved callback and a rejected callback.
		/// The resolved callback chains a value promise (optionally converting to a different value type).
		/// </summary>
		IPromise<ConvertedT> Then<ConvertedT>(Func<IPromise<ConvertedT>> onResolved, Action<Exception> onRejected);

		/// <summary>
		/// Add a resolved callback and a rejected callback.
		/// The resolved callback chains a non-value promise.
		/// </summary>
		IPromise Then(Func<IPromise> onResolved, Action<Exception> onRejected);

		/// <summary>
		/// Add a resolved callback and a rejected callback.
		/// </summary>
		IPromise Then(Action onResolved, Action<Exception> onRejected);

		/// <summary>
		/// Chain an enumerable of promises, all of which must resolve.
		/// The resulting promise is resolved when all of the promises have resolved.
		/// It is rejected as soon as any of the promises have been rejected.
		/// </summary>
		new IPromise ThenAll(Func<IEnumerable<IPromise>> chain);

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

        /// <summary>
        /// Add a finally callback.
        /// Finally callbacks will always be called, even if any preceding promise is rejected, or encounters an error.
        /// </summary>
        new IPromise Finally(Action onComplete);

        /// <summary>
        /// Add a finally callback.
        /// Finally callbacks will always be called, even if any preceding promise is rejected, or encounters an error.
        /// </summary>
        IPromise<ConvertedT> Finally<ConvertedT>(Func<IPromise<ConvertedT>> onComplete);
    }

	/// <summary>
	/// Interface for a promise that can be rejected or resolved.
	/// </summary>
	public interface IPendingPromise : IRejectable
	{
		/// <summary>
		/// Resolve the promise with a particular value.
		/// </summary>
		void Resolve();
	}

	/// <summary>
	/// Implements a non-generic C# promise, this is a promise that simply resolves without delivering a value.
	/// https://developer.mozilla.org/en/docs/Web/JavaScript/Reference/Global_Objects/Promise
	/// </summary>
	public class Promise : Promise_Base, IPromise, IPendingPromise
	{
		/// <summary>
		/// Set to true to enable tracking of promises.
		/// </summary>
		public static bool EnablePromiseTracking = false;

		/// <summary>
		/// Event raised for unhandled errors.
		/// For this to work you have to complete your promises with a call to Done().
		/// </summary>
		public static event EventHandler<ExceptionEventArgs> UnhandledException
		{
			add { unhandlerException += value; }
			remove { unhandlerException -= value; }
		}
		private static EventHandler<ExceptionEventArgs> unhandlerException;

		/// <summary>
		/// Id for the next promise that is created.
		/// </summary>
		internal static int nextPromiseId = 0;

		/// <summary>
		/// Information about pending promises.
		/// </summary>
		internal static HashSet<IPromiseInfo> pendingPromises = new HashSet<IPromiseInfo>();

		/// <summary>
		/// Information about pending promises, useful for debugging.
		/// This is only populated when 'EnablePromiseTracking' is set to true.
		/// </summary>
		public static IEnumerable<IPromiseInfo> GetPendingPromises()
		{
			return pendingPromises;
		}

		/// <summary>
		/// Represents a handler invoked when the promise is resolved.
		/// </summary>
		public struct ResolveHandler
		{
			/// <summary>
			/// Callback fn.
			/// </summary>
			public Action callback;

			/// <summary>
			/// The promise that is rejected when there is an error while invoking the handler.
			/// </summary>
			public IRejectable rejectable;
		}

		/// <summary>
		/// Completed handlers that accept no value.
		/// </summary>
		private List<ResolveHandler> resolveHandlers;

		public Promise() : base()
		{ }

		public Promise(Action<Action, Action<Exception>> resolver): this()
		{
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
		/// Add a resolve handler for this promise.
		/// </summary>
		private void AddResolveHandler(Action onResolved, IRejectable rejectable)
		{
			if (resolveHandlers == null)
			{
				resolveHandlers = new List<ResolveHandler>();
			}

			resolveHandlers.Add(new ResolveHandler()
			{
				callback = onResolved,
				rejectable = rejectable
			});
		}

		/// <summary>
		/// Invoke a single error handler.
		/// </summary>
		private void InvokeRejectHandler(Action<Exception> callback, IRejectable rejectable, Exception value)
		{
//            Argument.NotNull(() => callback);
//            Argument.NotNull(() => rejectable);

			try
			{
				callback(value);
			}
			catch (Exception ex)
			{
				rejectable.Reject(ex);
			}
		}

		/// <summary>
		/// Invoke a single resolve handler.
		/// </summary>
		private void InvokeResolveHandler(Action callback, IRejectable rejectable)
		{
//            Argument.NotNull(() => callback);
//            Argument.NotNull(() => rejectable);

			try
			{
				callback();
			}
			catch (Exception ex)
			{
				rejectable.Reject(ex);
			}
		}

		/// <summary>
		/// Helper function clear out all handlers after resolution or rejection.
		/// </summary>
		override protected void ClearHandlers()
		{
			base.ClearHandlers();
			resolveHandlers = null;
		}

		/// <summary>
		/// Invoke all resolve handlers.
		/// </summary>
		private void InvokeResolveHandlers()
		{
			if (resolveHandlers != null)
			{
				resolveHandlers.Each(handler => InvokeResolveHandler(handler.callback, handler.rejectable));
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

			if (EnablePromiseTracking)
			{
				pendingPromises.Remove(this);
			}

			InvokeResolveHandlers();
		}

		/// <summary>
		/// Completes the promise. 
		/// onResolved is called on successful completion.
		/// onRejected is called on error.
		/// </summary>
		public void Done(Action onResolved, Action<Exception> onRejected)
		{
			Then(onResolved, onRejected)
				.Catch(ex =>
					Promise.PropagateUnhandledException(this, ex)
				);
		}

		/// <summary>
		/// Completes the promise. 
		/// onResolved is called on successful completion.
		/// Adds a default error handler.
		/// </summary>
		public void Done(Action onResolved)
		{
			Then(onResolved)
				.Catch(ex => 
					Promise.PropagateUnhandledException(this, ex)
				);
		}

		/// <summary>
		/// Completes the promise. 
		/// onResolved is called on successful completion.
		/// onRejected is called on error.
		/// </summary>
		void IPromiseBase.Done(Action<PromiseResult> onResolved, Action<Exception> onRejected)
		{
			Then(() => { onResolved(PromiseResult.None); }, onRejected)
				.Catch(ex =>
					Promise.PropagateUnhandledException(this, ex)
				);
		}

		/// <summary>
		/// Completes the promise. 
		/// onResolved is called on successful completion.
		/// Adds a default error handler.
		/// </summary>
		void IPromiseBase.Done(Action<PromiseResult> onResolved)
		{
			Then(() => { onResolved(PromiseResult.None); })
				.Catch(ex =>
					Promise.PropagateUnhandledException(this, ex)
				);
		}

		/// <summary>
		/// Complete the promise. Adds a defualt error handler.
		/// </summary>
		public void Done()
		{
			Catch(ex =>
				Promise.PropagateUnhandledException(this, ex)
			);
		}

		/// <summary>
		/// Set the name of the promise, useful for debugging.
		/// </summary>
		public IPromise WithName(string name)
		{
			this.Name = name;
			return this;
		}

		IPromiseBase IPromiseBase.WithName(string name)
		{
			return WithName(name);
		}

		/// <summary>
		/// Handle errors for the promise. 
		/// </summary>
		public IPromise Catch(Action<Exception> onRejected)
		{
//            Argument.NotNull(() => onRejected);

			var resultPromise = new Promise();
			resultPromise.WithName(Name);

			Action resolveHandler = () =>
			{
				resultPromise.Resolve();
			};

			Action<Exception> rejectHandler = ex =>
			{
				onRejected(ex);

				resultPromise.Reject(ex);
			};

			ActionHandlers(resultPromise, resolveHandler, rejectHandler);

			return resultPromise;
		}

		IPromiseBase IPromiseBase.Catch(Action<Exception> onRejected)
		{
			return Catch(onRejected);
		}

		/// <summary>
		/// Add a resolved callback that chains a value promise (optionally converting to a different value type).
		/// </summary>
		public IPromise<ConvertedT> Then<ConvertedT>(Func<IPromise<ConvertedT>> onResolved)
		{
			return Then(onResolved, null);
		}

		/// <summary>
		/// Add a resolved callback that chains a non-value promise.
		/// </summary>
		public IPromise Then(Func<IPromise> onResolved)
		{
			return Then(onResolved, null);
		}

		IPromiseBase IPromiseBase.Then(Func<PromiseResult, IPromise> onResolved)
		{
			return Then(() => { onResolved(PromiseResult.None); });
		}

		/// <summary>
		/// Add a resolved callback.
		/// </summary>
		public IPromise Then(Action onResolved)
		{
			return Then(onResolved, null);
		}

		IPromiseBase IPromiseBase.Then(Action<PromiseResult> onResolved)
		{
			return Then(() => { onResolved(PromiseResult.None); });
		}

		/// <summary>
		/// Add a resolved callback and a rejected callback.
		/// The resolved callback chains a value promise (optionally converting to a different value type).
		/// </summary>
		public IPromise<ConvertedT> Then<ConvertedT>(Func<IPromise<ConvertedT>> onResolved, Action<Exception> onRejected)
		{
			// This version of the function must supply an onResolved.
			// Otherwise there is now way to get the converted value to pass to the resulting promise.
//            Argument.NotNull(() => onResolved);

			var resultPromise = new Promise<ConvertedT>();
			resultPromise.WithName(Name);

			Action resolveHandler = () =>
			{
				onResolved()
					.Then(
						// Should not be necessary to specify the arg type on the next line, but Unity (mono) has an internal compiler error otherwise.
						(ConvertedT chainedValue) => resultPromise.Resolve(chainedValue),
						ex => resultPromise.Reject(ex)
					);
			};

			Action<Exception> rejectHandler = ex =>
			{
				if (onRejected != null)
				{
					onRejected(ex);
				}

				resultPromise.Reject(ex);
			};

			ActionHandlers(resultPromise, resolveHandler, rejectHandler);

			return resultPromise;
		}

		/// <summary>
		/// Add a resolved callback and a rejected callback.
		/// The resolved callback chains a non-value promise.
		/// </summary>
		public IPromise Then(Func<IPromise> onResolved, Action<Exception> onRejected)
		{
			var resultPromise = new Promise();
			resultPromise.WithName(Name);

			Action resolveHandler = () =>
			{
				if (onResolved != null)
				{
					onResolved()
						.Then(
							() => resultPromise.Resolve(),
							ex => resultPromise.Reject(ex)
						);
				}
				else
				{
					resultPromise.Resolve();
				}
			};

			Action<Exception> rejectHandler = ex =>
			{
				if (onRejected != null)
				{
					onRejected(ex);
				}

				resultPromise.Reject(ex);
			};

			ActionHandlers(resultPromise, resolveHandler, rejectHandler);

			return resultPromise;
		}

		IPromiseBase IPromiseBase.Then(Func<PromiseResult, IPromise> onResolved, Action<Exception> onRejected)
		{
			return Then(() => { onResolved(PromiseResult.None); }, onRejected);
		}

		/// <summary>
		/// Add a resolved callback and a rejected callback.
		/// </summary>
		public IPromise Then(Action onResolved, Action<Exception> onRejected)
		{
			var resultPromise = new Promise();
			resultPromise.WithName(Name);

			Action resolveHandler = () =>
			{
				if (onResolved != null)
				{
					onResolved();
				}

				resultPromise.Resolve();
			};

			Action<Exception> rejectHandler = ex =>
			{
				if (onRejected != null)
				{
					onRejected(ex);
				}

				resultPromise.Reject(ex);
			};

			ActionHandlers(resultPromise, resolveHandler, rejectHandler);

			return resultPromise;
		}

		IPromiseBase IPromiseBase.Then(Action<PromiseResult> onResolved, Action<Exception> onRejected)
		{
			return Then(() => { onResolved(PromiseResult.None); }, onRejected);
		}

		/// <summary>
		/// Helper function to invoke or register resolve/reject handlers.
		/// </summary>
		private void ActionHandlers(IRejectable resultPromise, Action resolveHandler, Action<Exception> rejectHandler)
		{
			if (CurState == PromiseState.Resolved)
			{
				InvokeResolveHandler(resolveHandler, resultPromise);
			}
			else if (CurState == PromiseState.Rejected)
			{
				InvokeRejectHandler(rejectHandler, resultPromise, rejectionException);
			}
			else
			{
				AddResolveHandler(resolveHandler, resultPromise);
				AddRejectHandler(rejectHandler, resultPromise);
			}
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

		IPromiseBase IPromiseBase.ThenAll(Func<IEnumerable<IPromise>> chain)
		{
			return ThenAll(chain);
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
			resultPromise.WithName("All");

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
			resultPromise.WithName("Race");

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
//            Argument.NotNull(() => ex);

			var promise = new Promise();
			promise.Reject(ex);
			return promise;
		}

		/// <summary>
		/// Raises the UnhandledException event.
		/// </summary>
		internal static void PropagateUnhandledException(object sender, Exception ex)
		{
			if (unhandlerException != null)
			{
				unhandlerException(sender, new ExceptionEventArgs(ex));
			}
		}

        public IPromise Finally(Action onComplete)
        {
            Promise promise = new Promise();

            Promise.Race(
                this.Then(() => { promise.Resolve(); }),
                this.Catch((e) => { promise.Resolve(); })
            );

            return promise.Then(onComplete);
        }

        public IPromise<ConvertedT> Finally<ConvertedT>(Func<IPromise<ConvertedT>> onComplete)
        {
            Promise promise = new Promise();

            Promise.Race(
                this.Then(() => { promise.Resolve(); }),
                this.Catch((e) => { promise.Resolve(); })
            );

            return promise.Then(() => { return onComplete(); });
        }

        IPromiseBase IPromiseBase.Finally(Action onComplete)
        {
            return Finally(onComplete);
        }
    }
}