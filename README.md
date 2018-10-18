# C-Sharp-Promise [![Build Status](https://travis-ci.org/Real-Serious-Games/C-Sharp-Promise.svg)](https://travis-ci.org/Real-Serious-Games/C-Sharp-Promise) [![NuGet](https://img.shields.io/nuget/v/RSG.Promise.svg)](https://www.nuget.org/packages/RSG.Promise/) #

<a href="https://promisesaplus.com/">
    <img src="https://promisesaplus.com/assets/logo-small.png" alt="Promises/A+ logo"
         title="Promises/A+ 1.0 compliant" align="right" />
</a>

Promises library for C# for management of asynchronous operations.

Inspired by JavaScript promises, but slightly different.

Used by [Real Serious Games](https://github.com/Real-Serious-Games/C-Sharp-Promise) for building serious games and simulations on Unity3d.

If you are interested in using promises for game development and Unity please see [this article](http://www.what-could-possibly-go-wrong.com/promises-for-game-development/).

## Recent Updates

- v3.0 (15 Feburary 2018)
    - *Finally* has been modified to work in a way consistent to [Promise.prototype.finally()](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Promise/finally) in JavaScript.
    - Added support for reporting progress in a promise.
    - Signed assembly with a strong name.
    - Errors throw custom exception types rather than generic ones.
    - Modified some overloads of *Then* that didn't make sense.
- v2.0 (4 December 2017)
    - *Then* functions chained after a *Catch* are now run after the exception is handled rather than being terminated
    - *Catch* can return a value which will be passed into the next *Then*
    - The *onResolved* callback of *Then* can now also return a value which is passed to the next promise in the same way
    - Added *elapsedUpdates* property to the TimeData struct used by PromiseTimer
- v1.3 (28 October 2017)
    - Added Cancel method to PromiseTimer
    - Implemented an overload of Promise.All that works on Tuples of multiple types
    - Implemented Finally method
    - Removed dependency on RSG.Toolkit
- v1.2 (8 March 2015)
    - *Transform* function has been renamed to *Then* (another overload of *Then*).

## Projects using this library
- **[RestClient for Unity 🤘](https://github.com/proyecto26/RestClient)**

## Contents

<!-- START doctoc generated TOC please keep comment here to allow auto update -->
<!-- DON'T EDIT THIS SECTION, INSTEAD RE-RUN doctoc TO UPDATE -->
**Table of Contents**  *generated with [DocToc](https://github.com/thlorenz/doctoc)*

- [Understanding Promises](#understanding-promises)
- [Promises/A+ Spec](#promisesa-spec)
- [Getting the DLL](#getting-the-dll)
- [Getting the Code](#getting-the-code)
- [Creating a Promise for an Async Operation](#creating-a-promise-for-an-async-operation)
- [Creating a Promise, Alternate Method](#creating-a-promise-alternate-method)
- [Waiting for an Async Operation to Complete](#waiting-for-an-async-operation-to-complete)
- [Chaining Async Operations](#chaining-async-operations)
- [Transforming the Results](#transforming-the-results)
- [Error Handling](#error-handling)
- [Unhandled Errors](#unhandled-errors)
- [Progress reporting](#progress-reporting)
- [Promises that are already Resolved/Rejected](#promises-that-are-already-resolvedrejected)
- [Interfaces](#interfaces)
- [Combining Multiple Async Operations](#combining-multiple-async-operations)
- [Chaining Multiple Async Operations](#chaining-multiple-async-operations)
- [Racing Asynchronous Operations](#racing-asynchronous-operations)
- [Chaining Synchronous Actions that have no Result](#chaining-synchronous-actions-that-have-no-result)
- [Promises that have no Results (a non-value promise)](#promises-that-have-no-results-a-non-value-promise)
- [Convert a value promise to a non-value promise](#convert-a-value-promise-to-a-non-value-promise)
- [Running a Sequence of Operations](#running-a-sequence-of-operations)
- [Combining Parallel and Sequential Operations](#combining-parallel-and-sequential-operations)
- [Weighted averaging of progress on multiple promises](#weighted-averaging-of-progress-on-multiple-promises)
- [PromiseTimer class](#promisetimer-class)
  - [PromiseTimer.WaitFor](#promisetimerwaitfor)
  - [PromiseTimer.WaitUntil](#promisetimerwaituntil)
  - [PromiseTimer.WaitWhile](#promisetimerwaitwhile)
  - [TimeData struct](#timedata-struct)
- [Examples](#examples)

<!-- END doctoc generated TOC please keep comment here to allow auto update -->

## Understanding Promises

To learn about promises:

- [Promises on Wikpedia](http://en.wikipedia.org/wiki/Futures_and_promises)
- [Good overview](https://www.promisejs.org/)
- [Mozilla](https://developer.mozilla.org/en/docs/Web/JavaScript/Reference/Global_Objects/Promise)

## Promises/A+ Spec

This promise library conforms to the [Promises/A+ Spec](https://promisesaplus.com/) (at least, as far as is possible with C#):

## Getting the DLL

The DLL can be installed via nuget. Use the Package Manager UI or console in Visual Studio or use nuget from the command line.

See here for instructions on installing a package via nuget: http://docs.nuget.org/docs/start-here/using-the-package-manager-console

The package to search for is *RSG.Promise*.

## Getting the Code

You can get the code by cloning the github repository. You can do this in a UI like SourceTree or you can do it from the command line as follows:

    git clone https://github.com/Real-Serious-Games/C-Sharp-Promise.git

Alternately, to contribute please fork the project in github.

## Creating a Promise for an Async Operation ##

Reference the DLL and import the namespace:
```cs
using RSG;
```
Create a promise before you start the async operation:
```cs
var promise = new Promise<string>();
```
The type of the promise should reflect the result of the async op.

Then initiate your async operation and return the promise to the caller.

Upon completion of the async op the promise is resolved:
```cs
promise.Resolve(myValue);
```
The promise is rejected on error/exception:
```cs
promise.Reject(myException);
```
To see it in context, here is an example function that downloads text from a URL. The promise is resolved when the download completes. If there is an error during download, say *unresolved domain name*, then the promise is rejected:

```cs
public IPromise<string> Download(string url)
{
    var promise = new Promise<string>();    // Create promise.
    using (var client = new WebClient())
    {
        client.DownloadStringCompleted +=   // Monitor event for download completed.
            (s, ev) =>
            {
                if (ev.Error != null)
                {
                    promise.Reject(ev.Error);   // Error during download, reject the promise.
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
```

## Creating a Promise, Alternate Method

There is another way to create a promise that replicates the JavaScript convention of passing a *resolver* function into the constructor. The resolver function is passed functions that resolve or reject the promise. This allows you to express the previous example like this:

```cs
var promise = new Promise<string>((resolve, reject) =>
{
    using (var client = new WebClient())
    {
        client.DownloadStringCompleted +=   // Monitor event for download completed.
            (s, ev) =>
            {
                if (ev.Error != null)
                {
                    reject(ev.Error);       // Error during download, reject the promise.
                }
                else
                {
                    resolve(ev.Result);     // Downloaded completed successfully, resolve the promise.
                }
            };

        client.DownloadStringAsync(new Uri(url), null); // Initiate async op.
    }
});
```

## Waiting for an Async Operation to Complete ##

The simplest usage is to register a completion handler to be invoked on completion of the async op:
```cs
Download("http://www.google.com")
    .Then(html =>
        Console.WriteLine(html)
    );
```

This snippet downloads the front page from Google and prints it to the console.

For all but the most trivial applications you will also want to register an error hander:
```cs
Download("http://www.google.com")
    .Then(html =>
        Console.WriteLine(html)
    )
    .Catch(exception =>
        Console.WriteLine("An exception occured while downloading!")
    );
```

The chain of processing for a promise ends as soon as an error/exception occurs. In this case when an error occurs the *Catch* handler would be called, but not the *Done* handler. If there is no error, then only *Done* is called.

## Chaining Async Operations ##

Multiple async operations can be chained one after the other using *Then*:
```cs
Download("http://www.google.com")
    .Then(html =>
        return Download(ExtractFirstLink(html)) // Extract the first link and download it.
    )
    .Then(firstLinkHtml =>
        Console.WriteLine(firstLinkHtml)
    )
    .Catch(exception =>
        Console.WriteLine("An exception occured while downloading!")
    );
```

Here we are chaining another download onto the end of the first download. The first link in the html is extracted and we then download that. *Then* expects the return value to be another promise. The chained promise can have a different *result type*.

## Transforming the Results ##

Sometimes you will want to simply transform or modify the resulting value without chaining another async operation.
```cs
Download("http://www.google.com")
    .Then(html =>
        return ExtractAllLinks(html))   // Extract all links and return an array of strings.
    )
    .Then(links =>                      // The input here is an array of strings.
        foreach (var link in links)
        {
            Console.WriteLine(link);
        }
    );
```

As is demonstrated the type of the value can also be changed during transformation. In the previous snippet a `Promise<string>` is transformed to a `Promise<string[]>`.

## Error Handling

An error raised in a callback aborts the function and all subsequent callbacks in the chain:
```cs
promise.Then(v => Something())   // <--- An error here aborts all subsequent callbacks...
    .Then(v => SomethingElse())
    .Then(v => AnotherThing())
    .Catch(e => HandleError(e))  // <--- Until the error handler is invoked here.
```

## Unhandled Errors

When `Catch` is omitted exceptions go silently unhandled. This is an acknowledged issue with the Promises pattern.

We handle this in a similar way to the JavaScript [Q](http://documentup.com/kriskowal/q) library. The `Done` method is used to terminate a chain, it registers a default catch handler that propagates unhandled exceptions to a default error handling mechanism that can be hooked into by the user.

Terminating a Promise chain using `Done`:
```cs
promise.Then(v => Something())
    .Then(v => SomethingElse())
    .Then(v => AnotherThing())
    .Done();    // <--- Terminate the pipeline and propagate unhandled exceptions.
```

To use the `Done` you must apply the following rule: When you get to the end of a chain of promises, you should either return the last promise or end the chain by calling `Done`.

To hook into the unhandled exception stream:
```cs
Promise.UnhandledException += Promise_UnhandledException;
```

Then forward the exceptions to your own logging system:
```cs
private void Promise_UnhandledException(object sender, ExceptionEventArgs e)
{
    Log.Error(e.Exception, "An unhandled promises exception occured!");
}
```

## Progress reporting

Promises can additionally report their progress towards completion, allowing the implementor to give the user feedback on the asynchronous operation. The general convention is to report progress as a value from 0 to 1.

For this, you can either call `Progress` in the promise definition chain or add a third parameter to the `Then` method.

Listening for progress reporting from a promise using `Progress`:
```cs
var promise = new Promise();
promise.Progress((progress) => Log.Info("Current progress is " + (100f * progress) + "%"));
```

Listening for progress on a `Then` call:
```cs
var promiseA = new Promise();
var promiseB = new Promise();
promise
    .Then(() => promiseB, null, (progress) => Log.Info("promiseA made progress: " + progress))
    .Progress(progress => Log.Info("promiseB made progress: " + progress));
```

In order to report progress for a promise, you need to call the `ReportProgress` method:
```cs
var promise = new Promise();
promise.ReportProgress(0.5f); // Report a 50% completion
```

## Promises that are already Resolved/Rejected

For convenience or testing you will at some point need to create a promise that *starts out* in the resolved or rejected state. This is easy to achieve using *Resolved* and *Rejected* functions:
```cs
var resolvedPromise = Promise<string>.Resolved("some result");

var rejectedPromise = Promise<string>.Rejected(someException);
```

## Interfaces ##

The class *Promise<T>* implements the following interfaces:

- `IPromise<T>` Interface to await promise resolution.
- `IPendingPromise<T>` Interface that can resolve or reject the promise.

## Combining Multiple Async Operations ##

The *All* function combines multiple async operations to run in parallel. It converts a collection of promises or a variable length parameter list of promises into a single promise that yields a collection.

Say that each promise yields a value of type *T*, the resulting promise then yields a collection with values of type *T*.

Here is an example that extracts links from multiple pages and merges the results:
```cs
var urls = new List<string>();
urls.Add("www.google.com");
urls.Add("www.yahoo.com");

Promise<string[]>
    .All(urls.Select(url => Download(url)))  // Download each URL.
    .Then(pages =>              // Receives collection of downloaded pages.
        pages.SelectMany(
            page => ExtractAllLinks(page) // Extract links from all pages then flatten to single collection of links.
        )
    )
    .Done(links =>              // Receives the flattened collection of links from all pages at once.
    {
        foreach (var link in links)
        {
            Console.WriteLine(link);
        }
    });
```

When listening for progress events in an All operation, the progress that you will receive will be the average of all the progress values reported by all the given promises.

## Chaining Multiple Async Operations

The *ThenAll* function is a convenient way of chaining multiple promise onto an existing promise:
```cs
promise
    .Then(result => SomeAsyncOperation(result)) // Chain a single async operation
    .ThenAll(result =>                          // Chain multiple async operations.
        new IPromise<string>[]                  // Return an enumerable of promises.
        {
            SomeAsyncOperation1(result),
            SomeAsyncOperation2(result),
            SomeAsyncOperation3(result)
        }
    )
    .Done(collection => ...);                   // Final promise resolves
                                                // with a collection of values
                                                // when all operations have completed.
```

## Racing Asynchronous Operations

The *Race* and *ThenRace* functions are similar to the *All* and *ThenAll* functions, but it is the first async operation that completes that wins the race and it's value resolves the promise.
```cs
promise
    .Then(result => SomeAsyncOperation(result)) // Chain an async operation.
    .ThenRace(result =>                         // Race multiple async operations.
        new IPromise<string>[]                  // Return an enumerable of promises.
        {
            SomeAsyncOperation1(result),
            SomeAsyncOperation2(result),
            SomeAsyncOperation3(result)
        }
    )
    .Done(result => ...);                       // The result has come from whichever of
                                                // the async operations completed first.
```

When listening for progress events in a race operation, the progress that you will receive will be the maximum of those reported by all the given promises.

## Chaining Synchronous Actions that have no Result

The *Then* function can be used to chain synchronous operations that yield no result.
```cs
var promise = ...
promise
    .Then(result => SomeAsyncOperation(result)) 	// Chain an async operation.
    .Then(result => Console.WriteLine(result))    	// Chain a sync operation that yields no result.
    .Done(result => ...);  // Result from previous ascync operation skips over the *Do* and is passed through.
```

## Promises that have no Results (a non-value promise)

What about a promise that has no result? This represents an asynchronous operation that promises only to complete, it doesn't promise to yield any value as a result. I call this a non-value promise, as opposed to a value promise, which is a promise that does yield a value. This might seem like a curiousity but it is actually very useful for sequencing visual effects.

`Promise` is very similar to `Promise<T>` and implements the similar interfaces: `IPromise` and `IPendingPromise`.

`Promise<T>` functions that affect the resulting value have no relevance for the non-value promise and have been removed.

As an example consider the chaining of animation and sound effects as we often need to do in *game development*:
```cs
RunAnimation("Foo")                         // RunAnimation returns a promise that
    .Then(() => RunAnimation("Bar"))        // is resolved when the animation is complete.
    .Then(() => PlaySound("AnimComplete"));
```

## Convert a value promise to a non-value promise

From time to time you might want to convert a value promise to a non-value promise or vice versa. Both `Promise` and `Promise<T>` have overloads of `Then` and `ThenAll` that do this conversion. You just need to return the appropriate type of promise (for `Then`) or enumerable of promises (for `ThenAll`).

As an example consider a recursive link extractor and file downloader function:
```cs
public IPromise DownloadAll(string url)
{
    return DownloadURL(url)                 // Yields a value, the HTML text downloaded.
        .Then(html => ExtractLinks(html))   // Convert HTML into an enumerable of links.
        .ThenAll(links =>                   // Process each link.
        {
            // Determine links that should be followed, then follow them.
            var linksToFollow = links.Where(link => IsLinkToFollow(link));
            var linksFollowing = linksToFollow.Select(link => DownloadAll(link));

            // Determine links that are files to be downloaded, then download them.
            var linksToDownload = links.Where(link => IsLinkToDownload(link));
            var linksDownloading = linksToDownload.Select(link => DownloadFile(link));

            // Return an enumerable of promises.
            // This combines the recursive link following and any files we want to download.
            // Because we are returning an enumerable of non-value promises, the resulting
            // chained promises is also non-value.
            return linksToFollow.Concat(linksDownloading);
        });
}
```

Usage:
```cs
DownloadAll("www.somewhere.com")
    .Done(() =>
        Console.WriteLine("Recursive download completed.");
    );
```

## Running a Sequence of Operations

The `Sequence` and `ThenSequence` functions build a single promise that wraps multiple sequential operations that will be invoked one after the other.

Multiple promise-yielding functions are provided as input, these are chained one after the other and wrapped in a single promise that is resolved once the sequence has completed.
```cs
var sequence = Promise.Sequence(
    () => RunAnimation("Foo"),
    () => RunAnimation("Bar"),
    () => PlaySound("AnimComplete")
);
```

The inputs can also be passed as a collection:
```cs
var operations = ...
var sequence = Promise.Sequence(operations);
```

This might be used, for example, to play a variable length collection of animations based on data:
```cs
var animationNames = ... variable length array of animation names loaded from data...
var animations = animationNames.Select(animName => (Func<IPromise>)(() => RunAnimation(animName)));
var sequence = Promise.Sequence(animations);
sequence
    .Done(() =>
    {
        // All animations have completed in sequence.
    });
```

Unfortunately we find that we have reached the limits of what is possible with C# type inference, hence the use of the ugly cast `(Func<IPromise>)`.

The cast can easily be removed by converting the inner anonymous function to an actual function which I'll call `PrepAnimation`:
```cs
private Func<IPromise> PrepAnimation(string animName)
{
    return () => RunAnimation(animName);
}

var animations = animationNames.Select(animName => PrepAnimation(animName));
var sequence = Promise.Sequence(animations);
sequence
    .Done(() =>
    {
        // All animations have completed in sequence.
    });
```

Holy cow... we've just careened into [functional programming](http://en.wikipedia.org/wiki/Functional_programming) territory, herein lies very powerful and expressive programming techniques.

## Combining Parallel and Sequential Operations

We can easily combine sequential and parallel operations to build very expressive logic.
```cs
Promise.Sequence(               // Play operations 1 and 2 sequently.
    () => Promise.All(          // Operation 1: Play animation and sound at same time.
        RunAnimation("Foo"),
        PlaySound("Bar")
    ),
    () => Promise.All(
        RunAnimation("One"),    // Operation 2: Play animation and sound at same time.
        PlaySound("Two")
    )
);
```

I'm starting to feel like we are defining behavior trees.

## Weighted averaging of progress on multiple promises

If you have a promise that comprises a sequence of other promises, you might want to report the total progress for these, and even give more weight to the progress of some promise over another. In this example, we are first downloading an asset from some URL and then we are loading the downloaded asset into memory. We consider that the time it takes to download the asset will be an 80% of the total time, while the time to load it into memory is a 20%:
```cs
var promise = new Promise();

Download(url)
    .Progress((downloadProgress) => promise.ReportProgress(0.8f * downloadProgress))
    .Then((asset) => LoadAssetIntoMemory(asset))
    .Progress((loadProgress) => promise.ReportProgress(0.8f + 0.2f * loadProgress))
    .Then(() => promise.Resolve())
    .Catch((ex) => promise.Reject(ex));

return promise;
```

## PromiseTimer class

The promise timer is not part of the Promises/A+ standard but is a utility that makes it possible to create promises that check if a condition is met each time the promise timer is updated. A common usage of this is in games where the promise timer is updated each frame.

To use it, create an instance of the promise timer and call its `Update` method in your main loop:
```cs
class Example
{
    private IPromiseTimer promiseTimer;

    Example()
    {
        promiseTimer = new PromiseTimer();
    }

    // Run once for every frame - equivilant to Update() in Unity
    void MainLoop()
    {
        // deltaTime is equal to the time since the last MainLoop
        promiseTimer.Update(Time.deltaTime);

        // Process your other logic here
    }
}
```

Note that usually it is best to call `PromiseTimer.Update` *before* your other logic, otherwise you may have unintended behaviour such as promises that are supposed to take a very short time resolving in the same update loop as they were created in.

### PromiseTimer.WaitFor

This method creates a promise that resolves after the specified amount of time in seconds has passed. Time is calculated as the sum of the delta values passed into `PromiseTimer.Update`
```cs
IPromise LogAfterFourSeconds()
{
    return promiseTimer.WaitFor(4f)
        .Then(() => Console.Log("4 seconds have passed!"));
}
```

### PromiseTimer.WaitUntil

WaitUntil takes a predicate to check each update and resolves once the predicate returns true. This predicate function is passed a `TimeData` object, which just contains the most recent frame's `deltaTime` and `elapsedTime` which is the total amount of time since the promise was created.
```cs
IPromise FadeOut(float duration)
{
    return promiseTimer.WaitUntil(timeData =>
    {
        // Here we are using the amount of elapsed time to calculate what the current
        // fade value should be (between 0 and 1).
        // Since we're fading our we should be going from 0 (not faded) to 1 (full)
        var fadeAmount = Mathf.Clamp01(timeData.elapsedTime / duration);
        SetFadeValue(fadeAmount);

        // Resolve the promsie once we've finished.
        return fadeAmount >= 1f;
    });
}
```

### PromiseTimer.WaitWhile

WaitWhile is exactly the same as WaitUntil except that it resolves when its predicate function returns false. Think of WaitUntil as running *until* its predicate returns true, and WaitWhile as running *while* its predicate returns true, stopping when it is false.

### TimeData struct

TimeData is passed to you as a paramter when using either PromiseTimer.WaitUntil or PromiseTimer.WaitWhile. It contains the following public fields:

- elapsedTime
    - The amount of time that has elapsed since the pending promise started running
- deltaTime
    - The amount of time since the last time the pending promise was updated.
- elapsedUpdates
    - The amount of times that PromiseTimer.Update() has been called since the pending promise started running

## Examples


- Example1
    - Example of downloading text from a URL using a promise.
- Example2
    - Example of a promise that is rejected because of an error during
    - the async operation.
- Example3
    - This example downloads search results from google then transforms the result to extract links.
    - Includes both error handling and a completion handler.
- Example4
    - This example downloads search results from google, extracts the links and follows only a single first link, downloads its then prints the result.
    - Includes both error handling and a completion handler.
- Example5
    - This example downloads search results from google, extracts the links, follows all (absolute) links and combines all async operations in a single operation using the `All` function.
