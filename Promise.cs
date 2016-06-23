using RSG.Promises;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RSG
{
	/// <summary>
	/// Implements a C# promise.
	/// https://developer.mozilla.org/en/docs/Web/JavaScript/Reference/Global_Objects/Promise
	/// </summary>
	public interface IPromise<PromisedT>: IPromiseBase
	{
		/// <summary>
		/// Set the name of the promise, useful for debugging.
		/// </summary>
		new IPromise<PromisedT> WithName(string name);

		/// <summary>
		/// Completes the promise. 
		/// onResolved is called on successful completion.
		/// onRejected is called on error.
		/// </summary>
		void Done(Action<PromisedT> onResolved, Action<Exception> onRejected);

		/// <summary>
		/// Completes the promise. 
		/// onResolved is called on successful completion.
		/// Adds a default error handler.
		/// </summary>
		void Done(Action<PromisedT> onResolved);

		/// <summary>
		/// Handle errors for the promise. 
		/// </summary>
		new IPromise<PromisedT> Catch(Action<Exception> onRejected);

		/// <summary>
		/// Add a resolved callback that chains a value promise (optionally converting to a different value type).
		/// </summary>
		IPromise<ConvertedT> Then<ConvertedT>(Func<PromisedT, IPromise<ConvertedT>> onResolved);

		/// <summary>
		/// Add a resolved callback that chains a non-value promise.
		/// </summary>
		IPromise Then(Func<PromisedT, IPromise> onResolved);

		/// <summary>
		/// Add a resolved callback.
		/// </summary>
		IPromise<PromisedT> Then(Action<PromisedT> onResolved);

		/// <summary>
		/// Add a resolved callback and a rejected callback.
		/// The resolved callback chains a value promise (optionally converting to a different value type).
		/// </summary>
		IPromise<ConvertedT> Then<ConvertedT>(Func<PromisedT, IPromise<ConvertedT>> onResolved, Action<Exception> onRejected);

		/// <summary>
		/// Add a resolved callback and a rejected callback.
		/// The resolved callback chains a non-value promise.
		/// </summary>
		IPromise Then(Func<PromisedT, IPromise> onResolved, Action<Exception> onRejected);

		/// <summary>
		/// Add a resolved callback and a rejected callback.
		/// </summary>
		IPromise<PromisedT> Then(Action<PromisedT> onResolved, Action<Exception> onRejected);

		/// <summary>
		/// Return a new promise with a different value.
		/// May also change the type of the value.
		/// </summary>
		IPromise<ConvertedT> Then<ConvertedT>(Func<PromisedT, ConvertedT> transform);

		/// <summary>
		/// Return a new promise with a different value.
		/// May also change the type of the value.
		/// </summary>
		[Obsolete("Use Then instead")]
		IPromise<ConvertedT> Transform<ConvertedT>(Func<PromisedT, ConvertedT> transform);

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
	public interface IPendingPromise<PromisedT> : IRejectable
	{
		/// <summary>
		/// Resolve the promise with a particular value.
		/// </summary>
		void Resolve(PromisedT value);
	}

	/// <summary>
	/// Implements a C# promise.
	/// https://developer.mozilla.org/en/docs/Web/JavaScript/Reference/Global_Objects/Promise
	/// </summary>
	public class Promise<PromisedT> : Promise_Base, IPromise<PromisedT>, IPendingPromise<PromisedT>
	{
		/// <summary>
		/// The value when the promises is resolved.
		/// </summary>
		private PromisedT resolveValue;

		/// <summary>
		/// Completed handlers that accept a value.
		/// </summary>
		private List<Action<PromisedT>> resolveCallbacks;
		private List<IRejectable> resolveRejectables;

        public Promise() : base()
        { }

		public Promise(Action<Action<PromisedT>, Action<Exception>> resolver) : this()
		{
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
		/// Add a resolve handler for this promise.
		/// </summary>
		private void AddResolveHandler(Action<PromisedT> onResolved, IRejectable rejectable)
		{
			if (resolveCallbacks == null)
			{
				resolveCallbacks = new List<Action<PromisedT>>();
			}

			if (resolveRejectables == null)
			{
				resolveRejectables = new List<IRejectable>();
			}

			resolveCallbacks.Add(onResolved);
			resolveRejectables.Add(rejectable);
		}

		/// <summary>
		/// Helper function clear out all handlers after resolution or rejection.
		/// </summary>
		protected override void ClearHandlers()
		{
            base.ClearHandlers();
			resolveCallbacks = null;
			resolveRejectables = null;
		}

		/// <summary>
		/// Invoke all resolve handlers.
		/// </summary>
		private void InvokeResolveHandlers(PromisedT value)
		{
			if (resolveCallbacks != null)
			{
				for (int i = 0, maxI = resolveCallbacks.Count; i < maxI; i++) {
					InvokeHandler(resolveCallbacks[i], resolveRejectables[i], value);
				}
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

			if (Promise.EnablePromiseTracking)
			{
				Promise.pendingPromises.Remove(this);
			}

			InvokeResolveHandlers(value);
		}

		/// <summary>
		/// Completes the promise. 
		/// onResolved is called on successful completion.
		/// onRejected is called on error.
		/// </summary>
		public void Done(Action<PromisedT> onResolved, Action<Exception> onRejected)
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
		public void Done(Action<PromisedT> onResolved)
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
        public void Done(Action onResolved, Action<Exception> onRejected)
        {
            Then((x) => { onResolved(); }, onRejected)
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
            Then((x) => { onResolved(); })
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
        public IPromise<PromisedT> WithName(string name)
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
		public IPromise<PromisedT> Catch(Action<Exception> onRejected)
		{
//            Argument.NotNull(() => onRejected);

			var resultPromise = new Promise<PromisedT>();
			resultPromise.WithName(Name);

			Action<PromisedT> resolveHandler = v =>
			{
				resultPromise.Resolve(v);
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
		public IPromise<ConvertedT> Then<ConvertedT>(Func<PromisedT, IPromise<ConvertedT>> onResolved)
		{
			return Then(onResolved, null);
		}

		/// <summary>
		/// Add a resolved callback that chains a non-value promise.
		/// </summary>
		public IPromise Then(Func<PromisedT, IPromise> onResolved)
		{
			return Then(onResolved, null);
		}

        IPromiseBase IPromiseBase.Then(Func<IPromise> onResolved)
        {
            return Then((x) => { onResolved(); }, null);
        }

		/// <summary>
		/// Add a resolved callback.
		/// </summary>
		public IPromise<PromisedT> Then(Action<PromisedT> onResolved)
		{
			return Then(onResolved, null);
		}

        IPromiseBase IPromiseBase.Then(Action onResolved)
        {
            return Then((x) => { onResolved(); }, null);
        }

        /// <summary>
        /// Add a resolved callback and a rejected callback.
        /// The resolved callback chains a value promise (optionally converting to a different value type).
        /// </summary>
        public IPromise<ConvertedT> Then<ConvertedT>(Func<PromisedT, IPromise<ConvertedT>> onResolved, Action<Exception> onRejected)
		{
			// This version of the function must supply an onResolved.
			// Otherwise there is now way to get the converted value to pass to the resulting promise.
//            Argument.NotNull(() => onResolved); 

			var resultPromise = new Promise<ConvertedT>();
			resultPromise.WithName(Name);

			Action<PromisedT> resolveHandler = v =>
			{
				onResolved(v)
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
		public IPromise Then(Func<PromisedT, IPromise> onResolved, Action<Exception> onRejected)
		{
			var resultPromise = new Promise();
			resultPromise.WithName(Name);

			Action<PromisedT> resolveHandler = v =>
			{
				if (onResolved != null)
				{
					onResolved(v)
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

        IPromiseBase IPromiseBase.Then(Func<IPromise> onResolved, Action<Exception> onRejected)
        {
            return Then((x) => { onResolved(); }, onRejected);
        }

        /// <summary>
        /// Add a resolved callback and a rejected callback.
        /// </summary>
        public IPromise<PromisedT> Then(Action<PromisedT> onResolved, Action<Exception> onRejected)
		{
			var resultPromise = new Promise<PromisedT>();
			resultPromise.WithName(Name);

			Action<PromisedT> resolveHandler = v =>
			{
				if (onResolved != null)
				{
					onResolved(v);
				}

				resultPromise.Resolve(v);
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

        IPromiseBase IPromiseBase.Then(Action onResolved, Action<Exception> onRejected)
        {
            return Then((x) => { onResolved(); }, onRejected);
        }

        /// <summary>
        /// Return a new promise with a different value.
        /// May also change the type of the value.
        /// </summary>
        public IPromise<ConvertedT> Then<ConvertedT>(Func<PromisedT, ConvertedT> transform)
		{
//            Argument.NotNull(() => transform);
			return Then(value => Promise<ConvertedT>.Resolved(transform(value)));
		}

		/// <summary>
		/// Return a new promise with a different value.
		/// May also change the type of the value.
		/// </summary>
		[Obsolete("Use Then instead")]
		public IPromise<ConvertedT> Transform<ConvertedT>(Func<PromisedT, ConvertedT> transform)
		{
//            Argument.NotNull(() => transform);
			return Then(value => Promise<ConvertedT>.Resolved(transform(value)));
		}

		/// <summary>
		/// Helper function to invoke or register resolve/reject handlers.
		/// </summary>
		private void ActionHandlers(IRejectable resultPromise, Action<PromisedT> resolveHandler, Action<Exception> rejectHandler)
		{
			if (CurState == PromiseState.Resolved)
			{
				InvokeHandler(resolveHandler, resultPromise, resolveValue);
			}
			else if (CurState == PromiseState.Rejected)
			{
				InvokeHandler(rejectHandler, resultPromise, rejectionException);
			}
			else
			{
				AddResolveHandler(resolveHandler, resultPromise);
				AddRejectHandler(rejectHandler, resultPromise);
			}
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

        IPromiseBase IPromiseBase.ThenAll(Func<IEnumerable<IPromise>> chain)
        {
            return ThenAll((x) => { return chain(); });
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
				return Promise<IEnumerable<PromisedT>>.Resolved(EnumerableExt.Empty<PromisedT>());
			}

			var remainingCount = promisesArray.Length;
			var results = new PromisedT[remainingCount];
			var resultPromise = new Promise<IEnumerable<PromisedT>>();
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
//            Argument.NotNull(() => ex);

			var promise = new Promise<PromisedT>();
			promise.Reject(ex);
			return promise;
		}
	}
}