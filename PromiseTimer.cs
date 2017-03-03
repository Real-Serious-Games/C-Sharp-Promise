using System;
using System.Collections.Generic;

namespace RSG
{

	public class PromiseCancelledException: Exception {
		/// <summary>
		/// Just create the exception
		/// </summary>
		public PromiseCancelledException()
		{

		}

		/// <summary>
		/// Create the exception with description
		/// </summary>
		/// <param name="message">Exception description</param>
		public PromiseCancelledException(String message) : base(message)
		{

		}
	}

    /// <summary>
    /// A class that wraps a pending promise with it's predicate and time data
    /// </summary>
    internal class PredicateWait
    {
        /// <summary>
        /// Predicate for resolving the promise
        /// </summary>
        public Func<TimeData, bool> predicate;

        /// <summary>
        /// The time the promise was started
        /// </summary>
        public float timeStarted;

        /// <summary>
        /// The pending promise which is an interface for a promise that can be rejected or resolved.
        /// </summary>
        public IPendingPromise pendingPromise;

        /// <summary>
        /// The time data specific to this pending promise. Includes elapsed time and delta time.
        /// </summary>
        public TimeData timeData;
    }

    /// <summary>
    /// Time data specific to a particular pending promise.
    /// </summary>
    public struct TimeData
    {
        /// <summary>
        /// The amount of time that has elapsed since the pending promise started running
        /// </summary>
        public float elapsedTime;

        /// <summary>
        /// The amount of time since the last time the pending promise was updated.
        /// </summary>
        public float deltaTime;
    }

    public interface IPromiseTimer
    {
        /// <summary>
        /// Resolve the returned promise once the time has elapsed
        /// </summary>
        IPromise WaitFor(float seconds);

        /// <summary>
        /// Resolve the returned promise once the predicate evaluates to true
        /// </summary>
        IPromise WaitUntil(Func<TimeData, bool> predicate);

        /// <summary>
        /// Resolve the returned promise once the predicate evaluates to false
        /// </summary>
        IPromise WaitWhile(Func<TimeData, bool> predicate);

        /// <summary>
        /// Update all pending promises. Must be called for the promises to progress and resolve at all.
        /// </summary>
        void Update(float deltaTime);

		/// <summary>
		/// Cancel a waiting promise and reject it immediately.
		/// </summary>
		bool Cancel(IPromise promise);
    }

    public class PromiseTimer : IPromiseTimer
    {
        /// <summary>
        /// The current running total for time that this PromiseTimer has run for
        /// </summary>
        private float curTime;

        /// <summary>
        /// Currently pending promises
        /// </summary>
        private List<PredicateWait> waiting = new List<PredicateWait>();

        /// <summary>
        /// Resolve the returned promise once the time has elapsed
        /// </summary>
        public IPromise WaitFor(float seconds)
        {
            return WaitUntil(t => t.elapsedTime >= seconds);
        }

        /// <summary>
        /// Resolve the returned promise once the predicate evaluates to false
        /// </summary>
        public IPromise WaitWhile(Func<TimeData, bool> predicate)
        {
            return WaitUntil(t => !predicate(t));
        }

        /// <summary>
        /// Resolve the returned promise once the predicate evalutes to true
        /// </summary>
        public IPromise WaitUntil(Func<TimeData, bool> predicate)
        {
            var promise = new Promise();

            var wait = new PredicateWait()
            {
                timeStarted = curTime,
                pendingPromise = promise,
                timeData = new TimeData(),
                predicate = predicate
            };

            waiting.Add(wait);

            return promise;
        }

		public bool Cancel(IPromise promise)
		{
			var wait = waiting.Find(w => w.pendingPromise.Id.Equals(promise.Id));

			if (wait == null)
			{
				return false;
			}

			wait.pendingPromise.Reject(new PromiseCancelledException("Promise was cancelled by user."));
			waiting.Remove(wait);

			return true;
		}

        /// <summary>
        /// Update all pending promises. Must be called for the promises to progress and resolve at all.
        /// </summary>
        public void Update(float deltaTime)
        {
            curTime += deltaTime;

            int i = 0;
            while (i < waiting.Count)
            {
                var wait = waiting[i];

                var newElapsedTime = curTime - wait.timeStarted;
                wait.timeData.deltaTime = newElapsedTime - wait.timeData.elapsedTime;
                wait.timeData.elapsedTime = newElapsedTime;

                bool result;
                try
                {
                    result = wait.predicate(wait.timeData);
                }
                catch (Exception ex)
                {
                    wait.pendingPromise.Reject(ex);
                    waiting.RemoveAt(i);
                    continue;
                }

                if (result)
                {
                    wait.pendingPromise.Resolve();
                    waiting.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }
        }
    }
}

