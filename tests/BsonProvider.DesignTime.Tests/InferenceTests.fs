#if INTERACTIVE
#r "../../packages/NUnit.2.6.3/lib/nunit.framework.dll"
#r "../../bin/FSharp.Data.Bson.DesignTime.dll"
#load "../Common/FsUnit.fs"
#else
module FSharp.Data.DesignTime.Tests.InferenceTests
#endif

open FsUnit
open System
open System.Globalization
open System.IO
open NUnit.Framework
open FSharp.Data
open FSharp.Data.Runtime
open FSharp.Data.Runtime.StructuralTypes
open FSharp.Data.Runtime.StructuralInference
open ProviderImplementation
open ProviderImplementation.ProvidedTypes

/// A collection containing just one type
let SimpleCollection typ =
  InferedType.Collection([ typeTag typ], Map.ofSeq [typeTag typ, (InferedMultiplicity.Multiple, typ)])

let culture = TextRuntime.GetCulture ""

let toRecord fields = InferedType.Record(None, fields, false)

let inferTypesFromValues = true

[<Test>]
let ``Finds common subtype of numeric types (decimal)``() =
  let source = JsonValue.Parse """[ 10, 10.23 ]"""
  let expected = SimpleCollection(InferedType.Primitive(typeof<decimal>, None, false))
  let actual = JsonInference.inferType inferTypesFromValues culture "" source
  actual |> shouldEqual expected

[<Test>]
let ``Finds common subtype of numeric types (int64)``() =
  let source = JsonValue.Parse """[ 10, 2147483648 ]"""
  let expected = SimpleCollection(InferedType.Primitive(typeof<int64>, None, false))
  let actual = JsonInference.inferType inferTypesFromValues culture "" source
  actual |> shouldEqual expected

[<Test>]
let ``Infers heterogeneous type of InferedType.Primitives``() =
  let source = JsonValue.Parse """[ 1,true ]"""
  let expected =
    InferedType.Collection
        ([ InferedTypeTag.Number; InferedTypeTag.Boolean ],
         [ InferedTypeTag.Number, (Single, InferedType.Primitive(typeof<Bit1>, None, false))
           InferedTypeTag.Boolean, (Single, InferedType.Primitive(typeof<bool>, None, false)) ] |> Map.ofList)
  let actual = JsonInference.inferType inferTypesFromValues culture "" source
  actual |> shouldEqual expected

[<Test>]
let ``Infers heterogeneous type of InferedType.Primitives and nulls``() =
  let source = JsonValue.Parse """[ 1,true,null ]"""
  let expected =
    InferedType.Collection
        ([ InferedTypeTag.Number; InferedTypeTag.Boolean; InferedTypeTag.Null ],
         [ InferedTypeTag.Null, (Single, InferedType.Null)
           InferedTypeTag.Number, (Single, InferedType.Primitive(typeof<Bit1>, None, false))
           InferedTypeTag.Boolean, (Single, InferedType.Primitive(typeof<bool>, None, false)) ] |> Map.ofList)
  let actual = JsonInference.inferType inferTypesFromValues culture "" source
  actual |> shouldEqual expected

[<Test>]
let ``Finds common subtype of numeric types (float)``() =
  let source = JsonValue.Parse """[ 10, 10.23, 79228162514264337593543950336 ]"""
  let expected = SimpleCollection(InferedType.Primitive(typeof<float>, None, false))
  let actual = JsonInference.inferType inferTypesFromValues culture "" source
  actual |> shouldEqual expected

[<Test>]
let ``Infers heterogeneous type of InferedType.Primitives and records``() =
  let source = JsonValue.Parse """[ {"a":0}, 1,2 ]"""
  let expected =
    InferedType.Collection
        ([ InferedTypeTag.Record None; InferedTypeTag.Number ],
         [ InferedTypeTag.Number, (Multiple, InferedType.Primitive(typeof<int>, None, false))
           InferedTypeTag.Record None,
             (Single, toRecord [ { Name="a"; Type=InferedType.Primitive(typeof<Bit0>, None, false) } ]) ] |> Map.ofList)
  let actual = JsonInference.inferType inferTypesFromValues culture "" source
  actual |> shouldEqual expected

[<Test>]
let ``Merges types in a collection of collections``() =
  let source = JsonValue.Parse """[ [{"a":true,"c":0},{"b":1,"c":0}], [{"b":1.1,"c":0}] ]"""
  let expected =
    [ { Name = "a"; Type = InferedType.Primitive(typeof<bool>, None, true) }
      { Name = "c"; Type = InferedType.Primitive(typeof<Bit0>, None, false) }
      { Name = "b"; Type = InferedType.Primitive(typeof<decimal>, None, true) } ]
    |> toRecord
    |> SimpleCollection
    |> SimpleCollection
  let actual = JsonInference.inferType inferTypesFromValues culture "" source
  actual |> shouldEqual expected

[<Test>]
let ``Unions properties of records in a collection``() =
  let source = JsonValue.Parse """[ {"a":1, "b":""}, {"a":1.2, "c":true} ]"""
  let expected =
    [ { Name = "a"; Type = InferedType.Primitive(typeof<decimal>, None, false) }
      { Name = "b"; Type = InferedType.Null }
      { Name = "c"; Type = InferedType.Primitive(typeof<bool>, None, true) } ]
    |> toRecord
    |> SimpleCollection
  let actual = JsonInference.inferType inferTypesFromValues culture "" source
  actual |> shouldEqual expected

[<Test>]
let ``Null should make string optional``() =
  let source = JsonValue.Parse """[ {"a":null}, {"a":"b"} ]"""
  let expected =
    [ { Name = "a"; Type = InferedType.Primitive(typeof<string>, None, true) } ]
    |> toRecord
    |> SimpleCollection
  let actual = JsonInference.inferType inferTypesFromValues culture "" source
  actual |> shouldEqual expected

[<Test>]
let ``Null is not a valid value of DateTime``() =
  let actual =
    subtypeInfered false InferedType.Null (InferedType.Primitive(typeof<DateTime>, None, false))
  let expected = InferedType.Primitive(typeof<DateTime>, None, true)
  actual |> shouldEqual expected

[<Test>]
let ``Infers mixed fields of a a record as heterogeneous type with nulls (1.)``() =
  let source = JsonValue.Parse """[ {"a":null}, {"a":123} ]"""
  let expected =
    [ { Name = "a"; Type = InferedType.Primitive(typeof<int>, None, true) } ]
    |> toRecord
    |> SimpleCollection
  let actual = JsonInference.inferType inferTypesFromValues culture "" source
  actual |> shouldEqual expected

[<Test>]
let ``Null makes a record optional``() =
  let source = JsonValue.Parse """[ {"a":null}, {"a":{"b": 1}} ]"""
  let expected =
    [ { Name = "a"; Type = InferedType.Record(Some "a", [{ Name = "b"; Type = InferedType.Primitive(typeof<Bit1>, None, false) }], true) } ]
    |> toRecord
    |> SimpleCollection
  let actual = JsonInference.inferType inferTypesFromValues culture "" source
  actual |> shouldEqual expected

[<Test>]
let ``Infers mixed fields of a record as heterogeneous type``() =
  let source = JsonValue.Parse """[ {"a":"hi"}, {"a":2} , {"a":2147483648} ]"""
  let cases =
    Map.ofSeq [ InferedTypeTag.String, InferedType.Primitive(typeof<string>, None, false)
                InferedTypeTag.Number, InferedType.Primitive(typeof<int64>, None, false) ]
  let expected =
    [ { Name = "a"; Type = InferedType.Heterogeneous cases }]
    |> toRecord
    |> SimpleCollection
  let actual = JsonInference.inferType inferTypesFromValues culture "" source
  actual |> shouldEqual expected

[<Test>]
let ``Infers mixed fields of a record as heterogeneous type with nulls (2.)``() =
  let source = JsonValue.Parse """[ {"a":null}, {"a":2} , {"a":3} ]"""
  let expected =
    [ { Name = "a"; Type = InferedType.Primitive(typeof<int>, None, true) }]
    |> toRecord
    |> SimpleCollection
  let actual = JsonInference.inferType inferTypesFromValues culture "" source
  actual |> shouldEqual expected

[<Test>]
let ``Inference of multiple nulls works``() =
  let source = JsonValue.Parse """[0, [{"a": null}, {"a":null}]]"""
  let prop = { Name = "a"; Type = InferedType.Null }
  let expected =
    InferedType.Collection
        ([ InferedTypeTag.Number; InferedTypeTag.Collection ],
         [ InferedTypeTag.Collection, (Single, SimpleCollection(toRecord [prop]))
           InferedTypeTag.Number, (Single, InferedType.Primitive(typeof<Bit0>, None, false)) ] |> Map.ofList)
  let actual = JsonInference.inferType inferTypesFromValues culture "" source
  actual |> shouldEqual expected

//to be able to test units of measures we have to compare the typenames with strings
let prettyTypeName (t:Type) =
  t.ToString()
   .Replace("System.", null)
   .Replace("[]", null)
   .Replace("[", "<")
   .Replace("]", ">")
   .Replace("String", "string")
   .Replace("Double", "float")
   .Replace("Decimal", "decimal")
   .Replace("Int32", "int")
   .Replace("Int64", "int64")
   .Replace("Boolean", "bool")
   .Replace("DateTime", "date")

[<Test>]
let ``Doesn't infer 12-002 as a date``() =
  // a previous version inferred a IntOrStringOrDateTime
  let source = JsonValue.Parse """[ "12-002", "001", "2012-selfservice" ]"""
  let expected =
    InferedType.Collection
        ([ InferedTypeTag.String; InferedTypeTag.Number],
         [ InferedTypeTag.String, (Multiple, InferedType.Primitive(typeof<string>, None, false))
           InferedTypeTag.Number, (Single, InferedType.Primitive(typeof<Bit1>, None, false)) ] |> Map.ofList)
  let actual = JsonInference.inferType inferTypesFromValues culture "" source
  actual |> shouldEqual expected

[<Test>]
let ``Doesn't infer ad3mar as a date``() =
  StructuralInference.inferPrimitiveType CultureInfo.InvariantCulture "ad3mar"
  |> shouldEqual typeof<string>
