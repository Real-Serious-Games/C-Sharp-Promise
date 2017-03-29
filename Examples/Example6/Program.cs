using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using RSG;

//
// Example of downloading text from a URL using a promise.
//

namespace Example
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var running = true;

            PromiseTask(() => DownloadAsync("http://www.google.com"))
                .Catch(exception =>
                       {
                           Console.WriteLine("Async operation errorred.");
                           Console.WriteLine(exception.Message);
                           running = false;
                       })
                .Done(result =>
                      {
                          Console.WriteLine("Google download completed.");
                          running = false;
                      });

            Console.WriteLine("Waiting");

            while(running)
            {
                Thread.Sleep(10);
            }

            Console.WriteLine("Exiting");
            Console.ReadLine();
        }

        /// <summary>
        ///     Download text from a URL.
        ///     A promise is returned that is resolved when the download has completed.
        ///     The promise is rejected if an error occurs during download.
        /// </summary>
        private static async Task<IPromise<string>> DownloadAsync(string url)
        {
            Console.WriteLine("Downloading " + url + " ...");

            var promise = new Promise<string>();

            using(var client = new WebClient())
            {
                var task = client.DownloadStringTaskAsync(new Uri(url));
                try
                {
                    // Downloaded completed successfully, resolve the promise.
                    promise.Resolve(await task);
                    Console.WriteLine("... Download completed.");
                } catch(Exception ex)
                {
                    Console.WriteLine("An error occurred... rejecting the promise.");

                    // Error during download, reject the promise.
                    promise.Reject(ex);
                }
            }

            return promise;
        }

        /// <summary>
        ///     Wraps awaitable async tasks in a Promise
        /// </summary>
        public static IPromise<T> PromiseTask<T>(Func<Task<IPromise<T>>> asyncTask) where T : class
        {
            var promise = new Promise<T>();
            
            // Wait for the async task to complete then resolve or reject the promise.
            asyncTask().ContinueWith(result =>
                                {
                                    result.Result
                                        .Then(res => { promise.Resolve(res); })
                                        .Catch(err => { promise.Reject(err); });
                                });

            return promise;
        }
    }
}