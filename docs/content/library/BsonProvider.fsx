(**

  # BSON Type Provider

  This article demonstrates how to use the BSON type provider to access
  `.bson` files in a statically typed way.

  The BSON type provider provides statically typed access to `BsonDocument`s.
  It takes as input a sequence of sample documents (e.g. the output from the
  `mongodump` utility). The generated type can then be used to read files
  with the same structure. If the loaded file does not match the structure of
  the samples, then a runtime error may occur (e.g. when accessing a
  nonexistent field).

  ## Introducing the provider

  The type provider is located in the `FSharp.Data.Bson.dll` assembly.
  Assuming the assembly is located in the `../../../bin` directory, we can
  load it in F# Interactive as follows:

 *)

#I "../../../bin"
#r "FSharp.Data.Bson.Runtime.dll"
#r "FSharp.Data.Bson.dll"
#r "MongoDB.Bson.dll"

open BsonProvider

(**

  ### Inferring a type from the samples

  The `BsonProvider<...>` takes a `string` as its first static parameter,
  representing the path to a file containing BSON. An absolute path can be
  specified, or a path relative to the current working directory.

  The following loads some zip code data using the provider:

 *)

type ZipCode = BsonProvider<"../data/zipcodes.bson">

let zip0 = ZipCode.GetSamples().[0]

zip0.Id        |> ignore
zip0.City      |> ignore
zip0.Loc       |> ignore
zip0.Pop       |> ignore
zip0.State     |> ignore
zip0.BsonValue |> ignore

(**

  The generated type has multiple properties:
    - `Id` of type `string`, corresponding to the `_id` field
    - `City` of type `string`, corresponding to the `city` field
    - `Loc` of type `float[]`, corresponding to the `loc` field
    - `Pop` of type `int`, corresponding to the `pop` field
    - `State` of type `string`, corresponding to the `state` field
    - `BsonValue` of type `BsonValue`, corresponding to the underlying
      `BsonDocument`

  The provider successfully infers the type from the samples, and exposes
  the various fields as properties (using a PascalCase name to follow
  standard .NET naming conventions).

 *)

(**

  ### Inferring a record type

  The fields need not be of a primitive type for the inference to work.
  In the case of a field containing an array of documents, a unifying type
  for the `BsonArray` is generated. If a certain property is missing from
  one document, but present in another, then it is inferred as optional.

 *)

type Student = BsonProvider<"../data/students.bson">

let (|Homework|_|) (score:Student.Score) =
    match score.Type with
    | "homework" -> Some score.Score
    | _ -> None

let homeworkAvgs =
    Student.GetSamples()
    |> Seq.ofArray
    |> Seq.map (fun student -> student.Scores)
    |> Seq.map (Array.choose (|Homework|_|))
    |> Seq.filter (fun homeworks -> homeworks.Length > 0)
    |> Seq.map (Array.average)
    |> Seq.toArray

(**

  ## Summary

  This article demonstrated the `BsonProvider` type. The provider infers
  the structure of a `.bson` file and exposes it to F# programmers in a
  nicely typed way.

*)
