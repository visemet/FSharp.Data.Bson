BsonTypeProvider
================

A type provider for [BSON][bson_spec].

You can see the version history [here][release_notes].

Building
--------

  - Simply build `BsonTypeProvider.sln` in Visual Studio, Mono Develop, or Xamarin Studio.
    You can also use the FAKE script:

      * Windows: Run _build.cmd_
      * Mono: Run _build.sh_

Supported platforms
-------------------

  - VS2012 compiling to FSharp.Core 4.3.0.0
  - Mono F# 3.0 compiling to FSharp.Core 4.3.0.0
  - VS2013 compiling to FSharp.Core 4.3.0.0
  - VS2013 compiling to FSharp.Core 4.3.1.0
  - Mono F# 3.1 compiling to FSharp.Core 4.3.0.0
  - Mono F# 3.1 compiling to FSharp.Core 4.3.1.0

Documentation
-------------

The documentation is automatically generated from the `*.fsx` files in the [content folder][docs_content]
and from the comments within the code. If you find a typo, then please [submit a pull request][pull_requests]!

  - The [BSON Type Provider home page][home_page] has more information about the library, examples, etc.
  - The samples from the documentation are included as part of `BsonTypeProvider.Tests.sln`,
    so make sure you build the solution before trying out the samples to ensure that all necessary packages are installed.

Community and Support
---------------------

  - If you have a question about the library, then create an [issue][issues] with the `question` label.
  - If you want to submit a bug or feature request, then create an [issue][issues] with the appropriate label.
  - If you would like to contribute, then feel free to send a [pull request][pull_requests].
  - To discuss more general ideas about the library, its goals and other open-source F# projects,
    please join the [`fsharp-opensource` mailing list][google_group].

License
-------

The library is available under Apache 2.0. For more information see the [license file][license] in the GitHub repository.

  [bson_spec]:     http://bsonspec.org
  [docs_content]:  docs/content
  [google_group]:  http://groups.google.com/group/fsharp-opensource
  [home_page]:     http://visemet.github.io/FSharp.Data.Bson
  [issues]:        https://github.com/visemet/FSharp.Data.Bson/issues
  [license]:       LICENSE.md
  [pull_requests]: https://github.com/visemet/FSharp.Data.Bson/pulls
  [release_notes]: RELEASE_NOTES.md
