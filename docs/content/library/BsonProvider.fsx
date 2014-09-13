(**
# BSON Type Provider

In this article, we look at a type provider that makes it possible to easily...

## Introducing the provider

*)

#I "../../../bin"
#r "FSharp.Data.Bson.Runtime.dll"
#r "FSharp.Data.Bson.dll"
#r "MongoDB.Bson.dll"

open BsonProvider
open BsonProvider.Runtime

type Messages = BsonProvider<"/Users/maxh/Dropbox/messages.bson">

printf "%A\n" <| Messages.GetSamples().[0].Body

(**

## Summary

This article demonstrated the `BsonProvider` type.
The provider infers structure of a BSON file and exposes it in a nice typed way to F# programmers.

*)