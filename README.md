# C-Sharp-Promise [![Build Status](https://travis-ci.org/Real-Serious-Games/C-Sharp-Promise.svg)](https://travis-ci.org/Real-Serious-Games/C-Sharp-Promise) #

Promises library for C# for management of asynchronous operations. 

Inspired by Javascript promises, but slightly different.

To learn about promises:

- [Promises on Wikpedia](http://en.wikipedia.org/wiki/Futures_and_promises)
- [Spec](https://www.promisejs.org/)
- [Mozilla 
- cs](https://developer.mozilla.org/en/docs/Web/JavaScript/Reference/Global_Objects/Promise)


Getting the DLL
---------------

The DLL can be installed via nuget. Use the Package Manager UI or console in Visual Studio or use nuget from the command line.

See here for instructions on installing a package via nuget: http://docs.nuget.org/docs/start-here/using-the-package-manager-console

The package to search for is *RSG.Promise*.

Getting the Code
----------------

You can get the code by cloning the github repository. You can do this in a UI like SourceTree or you can do it from the command line as follows:

	git clone https://github.com/Real-Serious-Games/C-Sharp-Promise.git

Alternately, to contribute please fork the project in github.

## Creating a Promise for an Async Operation ##

Create a promise before you start the async operation:
	
	var promise = new Promise<string>();

The type of the promise should reflect the result of the async op.

Then initiate your async operation and return the promise to the caller.

Upon completion of the async op the promise is resolved:

	promise.Resolve(myValue);

The promise is rejected on error/exception:

	promise.Reject(myException);

To see it in context, here is an example function that downloads text from a URL. The promise is resolved when the download completes. If there is an error during download, say *unresolved domain name*, then the promise is rejected:

    public IPromise<string> Download(string url)
    {
        var promise = new Promise<string>(); 	// Create promise.
        using (var client = new WebClient())
        {
            client.DownloadStringCompleted += 	// Monitor event for download completed.
                (s, ev) =>
                {
                    if (ev.Error != null)
                    {
                        promise.Reject(ev.Error); 	// Error during download, reject the promise.
                    }
                    else
                    {
                        promise.Resolve(ev.Result); // Downloaded completed successfully, resolve the promise.
                    }
                };

            client.DownloadStringAsync(new Uri(url), null); // Initiate async op.
        }

        return promise; // Return the promise so the caller can await resolution (or error).
    }
 
## Waiting for an Async Operation to Complete ##

The simplest usage is to register a completion handler to be invoked on completion of the async op:

	Download("http://www.google.com")
		.Done(html =>
			Console.WriteLine(html)
		);

This snippet downloads the front page from Google and prints it to the console.

For all but the most trivial applications you will also want to register an error hander:

	Download("http://www.google.com")
		.Catch(exception =>
			Console.WriteLine("An exception occured while downloading!")
		)
		.Done(html =>
			Console.WriteLine(html)
		);

The chain of processing for a promise as soon as an error/exception occurs. In this case when an error occurs the *Catch* handler would be called, but not the *Done* handler. If there is no error, then only *Done* is called.

## Chaining Async Operations ##

Multiple async operations can be chained one after the other using *Then*:

	Download("http://www.google.com")
		.Then(html =>
			return Download(ExtractFirstLink(html)) // Extract the first link and download it. 
		)
		.Catch(exception =>
			Console.WriteLine("An exception occured while downloading!")
		)
		.Done(firstLinkHtml =>
			Console.WriteLine(firstLinkHtml)
		);
 
Here we are chaining another download onto the end of the first download. The first link in the html is extracted and we then download that. *Then* expects the return value to be another promise. The chained promise can have a different *result type*.

## Transforming the Results ##

Sometimes you will want to simply transform or modify the resulting value without chaining another async operation.

	Download("http://www.google.com")
		.Transform(html =>
			return ExtractAllLinks(html)) // Extract all links and return an array of strings.  
		)
		.Done(links =>					  // The input here is an array of strings.
			foreach (var link in links)
			{
				Console.WriteLine(link);
			}
		);

As is demonstrated the type of the value can also be changed during transformation. In the previous snippet a `Promise<string>` is transformed to a `Promise<string[]>`.   

## Promises that are already Resolved/Rejected 

For convenience or testing you will at some point need to create a promise that *starts out* in the resolved or rejected state. This is easy to achieve using *Resolved* and *Rejected* functions:

	var resolvedPromise = Promise<string>.Resolved("some result");

	var rejectedPromise = Promise<string>.Rejected(someException);

## Interfaces ##

The class *Promise<T>* implements the following interfaces: 

- `IPromise<T>` Interface to await promise resolution.
- `IPendingPromise<T>` Interface that can resolve or reject the promise.  

## Combining Multiple Async Operations ##

The *All* function combines multiple async operations. It converts a collection of promises or a variable length parameter list of promises into a single promise that yields a collection. 
 Say that each promise yields a value of type *T*, the resulting promise then yields a collection with values of type *T*.  

Here is an example that extracts links from multiple pages and merges the results:

	var urls = new List<string>();
	urls.Add("www.google.com");
	urls.Add("www.yahoo.com");

	Promise<string[]>
		.All(url => Download(url)) 	// Download each URL.
		.Then(pages =>				// Receives collection of downloaded pages.
			pages.SelectMany(
				page => ExtractAllLinks(page) // Extract links from all pages then flatten to single collection of links.
			)
		)
		.Done(links =>				// Receives the flattened collectioon of links from all pages at once.
		{
			foreach (var link in links)
			{
				Console.WriteLine(link);
			}
		});

The *ThenAll* function does the same thing, but is a more convenient way of chaining:

	promise
		.Then(result => SomeAsyncOperation(result)) // Chain a single async operation
		.ThenAll(result =>
			SomeAsyncOperation1(result),			// Chain multiple sync operations.
			SomeAsyncOperation2(result),
			SomeAsyncOperation3(result)
		)
		.Done(collection => ...);					// Final promise resolves 
													// with a collection of values 
													// when all operations have completed.  


## Racing Asynchronous Operations

The *Race* and *RaceAll* functions are similar to the *All* and *ThenAll* functions, but it is the first async operation that completes that wins the race and it's value resolves the promise.

	promise
		.Then(result => SomeAsyncOperation(result))	// Chain an async operation.
		.ThenRace(result =>
			SomeAsyncOperation1(result),			// Race multiple async operations.
			SomeAsyncOperation2(result),
			SomeAsyncOperation3(result)
		)
		.Done(result => ...);						// The result has come from whichever of
													// the async operations completed first. 

## Chaining Synchronous Actions that have no Result

The *ThenDo* function can be used to chain synchronous operations that yield no result.

	var promise = ...
	promise
		.Then(result => SomeAsyncOperation(result)) // Chain an async operation.
		.ThenDo(result => Console.WriteLine(result))    // Chain a sync operation that yields no result.
		.Done(result => ...);  // Result from previous ascync operation skips over the *Do* and is passed through.


## Examples ##


- Example1
	- Example of downloading text from a URL using a promise.
- Example2
	- Example of a promise that is rejected because of an error during the async operation.
- Example3
	- This example downloads search results from google then transforms the result to extract links.
	- Includes both error handling and a completion handler.
- Example4
	- This example downloads search results from google, extracts the links and follows only a single first link, downloads its then prints the result.
	- Includes both error handling and a completion handler.
- Example5
	- This example downloads search results from google, extracts the links, follows all (absolute) links and combines all async operations in a single operation using the All function.
