using RSG;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

//
// Example of downloading text from a URL using a promise.
//
namespace Example
{
    class Program
    {
        static void Main(string[] args)
        {
            PromiseTask(DownloadGoogleAsync).Then(res =>
            {
                Console.WriteLine("Google has been downloaded: " + res);
            });

            Console.WriteLine("This line will be written before Google has been downloaded");

            Console.ReadLine();
        }

        private async static Task<IPromise<string>> DownloadGoogleAsync()
        {
            var promise = new Promise<string>();

            Console.WriteLine("Starting download");

            var result = await new WebClient().DownloadStringTaskAsync(new Uri("https://google.com/"));

            promise.Resolve(result);

            Console.WriteLine("Finished download");

            return promise;
        }

        public static IPromise<T> PromiseTask<T>(Func<Task<IPromise<T>>> func) where T : class
        {
            var promise = new Promise<T>();

            func().ContinueWith(result =>
            {
                result.Result.Then(res =>
                {
                    promise.Resolve(res);
                }).Catch(err =>
                {
                    promise.Reject(err);
                });

            });

            return promise;
        }

    }
}
