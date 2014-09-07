#if INTERACTIVE
#r "../../packages/NUnit.2.6.3/lib/nunit.framework.dll"
#r "../../bin/FSharp.Data.Bson.dll"
#load "../Common/FsUnit.fs"
#else
module BsonProvider.Tests.InferenceTests
#endif

open FsUnit
open System
open System.Globalization
open System.IO
open NUnit.Framework
open MongoDB.Bson
open FSharp.Data
open FSharp.Data.Runtime
open FSharp.Data.Runtime.StructuralTypes
open FSharp.Data.Runtime.StructuralInference
open BsonProvider.Runtime

[<Test>]
let ``Empty document has no properties``() = ()
