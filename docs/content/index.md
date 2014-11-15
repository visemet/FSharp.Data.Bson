Bson Provider
=============

The Bson Provider library (`FSharp.Data.Bson.dll`) contains a type provider
for working with [BSON][bson_spec].

Library license
---------------

The library is available under Apache 2.0. For more information see the
[license file][license] in the GitHub repository.

  - - -

Accessing the library
---------------------

Check out the [repository hosted on GitHub][repo].

  - - -

Using the library
-----------------

The BSON type provider provides statically typed access to `BsonDocument`s.
It takes as input a sequence of sample documents (e.g. the output from the
`mongodump` utility). The generated type can then be used to read files
with the same structure.

  * [BSON Type Provider][bson_provider] - discusses the `BsonProvider<...>` type

  - - -

Community and Support
---------------------

  - If you have a question about the library, then create an [issue][issues]
    with the `question` label.

  - If you want to submit a bug or feature request, then create an
    [issue][issues] with the appropriate label.

  - If you would like to contribute, then feel free to send a
    [pull request][pull_requests].

  - To discuss more general ideas about the library, its goals and other
    open-source F# projects, please join the
    [`fsharp-opensource` mailing list][google_group].

  [bson_provider]: library/BsonProvider.html
  [bson_spec]:     http://bsonspec.org
  [issues]:        https://github.com/visemet/FSharp.Data.Bson/issues
  [google_group]:  http://groups.google.com/group/fsharp-opensource
  [license]:       https://github.com/visemet/FSharp.Data.Bson/blob/master/LICENSE.md
  [pull_requests]: https://github.com/visemet/FSharp.Data.Bson/pulls
  [repo]:          https://github.com/visemet/FSharp.Data.Bson
