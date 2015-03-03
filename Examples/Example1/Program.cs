using RSG;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

//
// Example of downloading text from a URL using a promise.
//
namespace Example
{
    class Program
    {
        static void Main(string[] args)
        {
            var running = true;

            Download("http://www.google.com")   // Schedule an async operation.
                .Then(result =>                 // Use Done to register a callback to handle completion of the async operation.
                {
                    Console.WriteLine("Async operation completed.");
                    Console.WriteLine(result.Substring(0, 250) + "...");
                    running = false;
                })
                .Done();

            Console.WriteLine("Waiting");

            while (running)
            {
                Thread.Sleep(10);
            }

            Console.WriteLine("Exiting");
        }

        /// <summary>
        /// Download text from a URL.
        /// A promise is returned that is resolved when the download has completed.
        /// The promise is rejected if an error occurs during download.
        /// </summary>
        static IPromise<string> Download(string url)
        {
            Console.WriteLine("Downloading " + url + " ...");

            var promise = new Promise<string>();
            using (var client = new WebClient())
            {
                client.DownloadStringCompleted +=
                    (s, ev) =>
                    {
                        if (ev.Error != null)
                        {
                            Console.WriteLine("An error occurred... rejecting the promise.");

                            // Error during download, reject the promise.
                            promise.Reject(ev.Error);
                        }
                        else
                        {
                            Console.WriteLine("... Download completed.");

                            // Downloaded completed successfully, resolve the promise.
                            promise.Resolve(ev.Result);
                        }
                    };

                client.DownloadStringAsync(new Uri(url), null);
            }
            return promise;
        }
    }
}
