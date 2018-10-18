# How to contribute to C-Sharp-Promise

👍🎉 First off, thanks for taking the time to contribute! 🎉👍


## Reporting bugs

Any bug reports are useful, although there are a few things you can do that will
make it easier for maintainers and other users to understand your report,
reproduce the behaviour, and find related reports.

  - **Use a clear and descriptive title** for the issue to identify the problem.
  - **Describe the exact steps which reproduce the problem** in as many details
    as possible. Code examples or links to repos to reproduce the issue are
    always helpful.
  - **Describe the behaviour you observed after following the steps** and point
    out what exactly the problem is with that behaviour.
  - **Explain which behaviour you expected to see instead and why.** Since
    C-Sharp-Promise is designed to replicate [JavaScript promises](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Guide/Using_promises),
    code that works with JavaScript promises but gives a different result or
    breaks with C-Sharp-Promise would be a a good example.
  - **Check the existing open issues on GitHub** to see if someone else has had
    the same problem as you.

Make sure to file bugs as [GitHub issues](https://github.com/Real-Serious-Games/C-Sharp-Promise/issues),
since that way everyone working on the library can see it and potentially help.


## Pull requests

Before we merge pull requests there are a few things we look for to make sure
the code is maintainable and up the same standard as the rest of the library.

  - Make sure you've written comprehensive unit tests for the feature, or
    modify existing tests if the feature changes functionality.
  - Check that your code conforms to the same style as existing code. Ensure that
    your editor is set up to read the [.editorconfig](http://editorconfig.org/)
    file for consistent spacing and line endings. We also try to keep our code
    style consistent with the [Microsoft C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/inside-a-program/coding-conventions)
    and [Framework Design Guidelines](https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/index).
  - Make sure that the [Travis CI build](https://travis-ci.org/Real-Serious-Games/C-Sharp-Promise)
    succeeds. This should run automatically when you create a pull request, but
    should have the same result as building the solution and running all the
    tests locally. We will not accept any pull requests that fail to build or
    contain failing tests.
  - If you have added a new feature, add a section to README.md describing the
    feature and how to use it.

In addition, if your pull request breaks any existing functionality it's
unlikely we will merge it unless there is a very good reason.