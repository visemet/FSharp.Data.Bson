(* Copyright (c) 2013-2014 Tomas Petricek and Gustavo Guerra
 * Copyright (c) 2014 Max Hirschhorn
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *)

#if INTERACTIVE
#I "../../packages/NUnit.2.6.3/lib"
#I "../../bin"
#r "nunit.framework.dll"
#r "FSharp.Data.Bson.DesignTime.dll"
#else
module BsonProvider.DesignTime.Tests.InferenceTests
#endif

open System
open System.Globalization
open System.IO
open MongoDB.Bson
open NUnit.Framework
open FSharp.Data
open FSharp.Data.Runtime
open FSharp.Data.Runtime.StructuralTypes
open FSharp.Data.Runtime.StructuralInference
open ProviderImplementation
open ProviderImplementation.ProvidedTypes
open BsonProvider.ProviderImplementation

let shouldEqual (x:'a) (y:'a) = Assert.AreEqual(x, y)

/// A collection containing just one type
let SimpleCollection typ =
    InferedType.Collection([typeTag typ], Map.ofSeq [typeTag typ, (InferedMultiplicity.Multiple, typ)])

let toRecord fields = InferedType.Record(None, fields, false)

let primitiveProperty<'T> name optional =
    { Name = name
      Type = InferedType.Primitive(typeof<'T>, None, optional) }

[<Test>]
let ``Infer type of empty document``() =
    let source = BsonDocument()
    let expected = InferedType.Record(None, [], false)
    let actual = BsonInference.inferType "" source
    actual |> shouldEqual expected

[<Test>]
let ``Infer type of empty array``() =
    let source = BsonArray()
    let expected = InferedType.Collection([], Map.empty)
    let actual = BsonInference.inferType "" source
    actual |> shouldEqual expected

[<Test>]
let ``Infer type of bool field``() =
    let source =
        BsonDocument("field", BsonBoolean false)

    let expected =
        [ primitiveProperty<bool> "field" false ]
        |> toRecord

    let actual = BsonInference.inferType "" source
    actual |> shouldEqual expected

[<Test>]
let ``Infer type of int field``() =
    let source =
        BsonDocument("field", BsonInt32 0)

    let expected =
        [ primitiveProperty<int> "field" false ]
        |> toRecord

    let actual = BsonInference.inferType "" source
    actual |> shouldEqual expected

[<Test>]
let ``Infer type of int64 field``() =
    let source =
        BsonDocument("field", BsonInt64 0L)

    let expected =
        [ primitiveProperty<int64> "field" false ]
        |> toRecord

    let actual = BsonInference.inferType "" source
    actual |> shouldEqual expected

[<Test>]
let ``Infer type of float field``() =
    let source =
        BsonDocument("field", BsonDouble 0.0)

    let expected =
        [ primitiveProperty<float> "field" false ]
        |> toRecord

    let actual = BsonInference.inferType "" source
    actual |> shouldEqual expected

[<Test>]
let ``Infer type of string field``() =
    let source =
        BsonDocument("field", BsonString "0")

    let expected =
        [ primitiveProperty<string> "field" false ]
        |> toRecord

    let actual = BsonInference.inferType "" source
    actual |> shouldEqual expected

[<Test>]
let ``An omitted field makes the type optional``() =
    let source =
        BsonArray [ BsonDocument("field", BsonInt32 0)
                    BsonDocument() ]

    let expected =
        [ primitiveProperty<int> "field" true ]
        |> toRecord
        |> SimpleCollection

    let actual = BsonInference.inferType "" source
    actual |> shouldEqual expected

[<Test>]
let ``An explicit null value makes the type optional``() =
    let source =
        BsonArray [ BsonDocument("field", BsonInt32 0)
                    BsonDocument("field", BsonNull.Value) ]

    let expected =
        [ primitiveProperty<int> "field" true ]
        |> toRecord
        |> SimpleCollection

    let actual = BsonInference.inferType "" source
    actual |> shouldEqual expected

[<Test>]
let ``An explicit undefined value makes the type optional``() =
    let source =
        BsonArray [ BsonDocument("field", BsonInt32 0)
                    BsonDocument("field", BsonUndefined.Value) ]

    let expected =
        [ primitiveProperty<int> "field" true ]
        |> toRecord
        |> SimpleCollection

    let actual = BsonInference.inferType "" source
    actual |> shouldEqual expected

[<Test>]
let ``An empty string does not make the type optional``() =
    let source =
        BsonArray [ BsonDocument("field", BsonString "0")
                    BsonDocument("field", BsonString "") ]

    let expected =
        [ primitiveProperty<string> "field" false ]
        |> toRecord
        |> SimpleCollection

    let actual = BsonInference.inferType "" source
    actual |> shouldEqual expected

[<Test>]
let ``A NaN value does not make the type optional``() =
    let source =
        BsonArray [ BsonDocument("field", BsonDouble 0.0)
                    BsonDocument("field", BsonDouble nan) ]

    let expected =
        [ primitiveProperty<float> "field" false ]
        |> toRecord
        |> SimpleCollection

    let actual = BsonInference.inferType "" source
    actual |> shouldEqual expected

[<Test>]
let ``Infer common subtype (int64) of numeric field``() =
    let source =
        BsonArray [ BsonDocument("field", BsonInt32 0)
                    BsonDocument("field", BsonInt64 0L) ]

    let expected =
        [ primitiveProperty<int64> "field" false ]
        |> toRecord
        |> SimpleCollection

    let actual = BsonInference.inferType "" source
    actual |> shouldEqual expected

[<Test>]
let ``Infer common subtype (float) of numeric field``() =
    let source =
        BsonArray [ BsonDocument("field", BsonInt32 0)
                    BsonDocument("field", BsonDouble 0.0) ]

    let expected =
        [ primitiveProperty<float> "field" false ]
        |> toRecord
        |> SimpleCollection

    let actual = BsonInference.inferType "" source
    actual |> shouldEqual expected

[<Test>]
let ``Infer heterogeneous type of mixed field``() =
    let source =
        BsonArray [ BsonDocument("field", BsonInt32 0)
                    BsonDocument("field", BsonString "0") ]

    let cases =
        [ InferedTypeTag.Number, InferedType.Primitive(typeof<int>, None, false)
          InferedTypeTag.String, InferedType.Primitive(typeof<string>, None, false) ]
        |> Map.ofList

    let expected =
        [ { Name = "field"; Type = InferedType.Heterogeneous cases }]
        |> toRecord
        |> SimpleCollection

    let actual = BsonInference.inferType "" source
    actual |> shouldEqual expected

[<Test>]
let ``Infer type of int array``() =
    let x : BsonValue list = [] // avoid ambiguous constructor
    let empty = BsonArray x
    let single = BsonArray [ BsonInt32 0 ]
    let multiple = BsonArray [ BsonInt32 0; BsonInt32 0 ]
    let source =
        BsonArray [ BsonDocument([ BsonElement("field_single", single)
                                   BsonElement("field_multiple", multiple)
                                   BsonElement("field_optional_single", empty) ])
                    BsonDocument([ BsonElement("field_single", single)
                                   BsonElement("field_multiple", multiple)
                                   BsonElement("field_optional_single", single) ]) ]
    let expected =
        [ { InferedProperty.Name = "field_single"
            Type = InferedType.Collection([InferedTypeTag.Number],
                                          Map.ofList [InferedTypeTag.Number, (InferedMultiplicity.Single, InferedType.Primitive(typeof<int>, None, false))]) }
          { InferedProperty.Name = "field_multiple"
            Type = InferedType.Collection([InferedTypeTag.Number],
                                          Map.ofList [InferedTypeTag.Number, (InferedMultiplicity.Multiple, InferedType.Primitive(typeof<int>, None, false))]) }
          { InferedProperty.Name = "field_optional_single"
            Type = InferedType.Collection([InferedTypeTag.Number],
                                          Map.ofList [InferedTypeTag.Number, (InferedMultiplicity.OptionalSingle, InferedType.Primitive(typeof<int>, None, false))]) } ]
        |> toRecord
        |> SimpleCollection
    let actual = BsonInference.inferType "" source
    actual |> shouldEqual expected

(* [<Test>]
let ``Infer type of empty array as field``() =
    let source =
        BsonArray [ BsonDocument("array", BsonArray())
                    BsonDocument("array", BsonArray([BsonInt32 10]))
                    BsonDocument("array", BsonArray([BsonInt32 10])) ]
    let expected =
        [ { InferedProperty.Name = "array"
            Type = InferedType.Collection([InferedTypeTag.Number],
                                          Map.ofList [InferedTypeTag.Number, (InferedMultiplicity.Single, InferedType.Primitive(typeof<int>, None, false))]) } ]
        |> toRecord
        |> SimpleCollection
    let actual = BsonInference.inferType "" source
    actual |> shouldEqual expected *)

[<Test>]
let ``Infer type of omitted fields as optional``() =
    let source =
            BsonArray [ BsonDocument([ BsonElement("a", BsonBoolean true)
                                       BsonElement("c", BsonInt32 0) ])
                        BsonDocument([ BsonElement("b", BsonInt32 1)
                                       BsonElement("c", BsonInt32 0) ]) ]
    let expected =
        [ { InferedProperty.Name = "a"; Type = InferedType.Primitive(typeof<bool>, None, true) }
          { Name = "c"; Type = InferedType.Primitive(typeof<int32>, None, false) }
          { Name = "b"; Type = InferedType.Primitive(typeof<int32>, None, true) } ]
        |> toRecord
        |> SimpleCollection
    let actual = BsonInference.inferType "" source
    actual |> shouldEqual expected

[<Test>]
let ``Find common subtype of numeric values (int64)``() =
    let source =
        BsonArray [ BsonInt32 10 :> BsonValue
                    BsonInt64 10L :> BsonValue ]

    let expected = SimpleCollection(InferedType.Primitive(typeof<int64>, None, false))
    let actual = BsonInference.inferType "" source
    actual |> shouldEqual expected

[<Test>]
let ``Find common subtype of numeric values (float)``() =
    let source =
        BsonArray [ BsonInt32 10 :> BsonValue
                    BsonDouble 10.23 :> BsonValue ]

    let expected = SimpleCollection(InferedType.Primitive(typeof<float>, None, false))
    let actual = BsonInference.inferType "" source
    actual |> shouldEqual expected

[<Test>]
let ``Find common subtype of all numeric values (float)``() =
    let source =
        BsonArray [ BsonInt32 10 :> BsonValue
                    BsonDouble 10.23 :> BsonValue
                    BsonInt64 10L :> BsonValue ]

    let expected = SimpleCollection(InferedType.Primitive(typeof<float>, None, false))
    let actual = BsonInference.inferType "" source
    actual |> shouldEqual expected

[<Test>]
let ``Infers heterogeneous type of InferedType.Primitives``() =
    let source =
        BsonArray [ BsonInt32 1 :> BsonValue
                    BsonBoolean true :> BsonValue ]

    let expected =
        InferedType.Collection
                ([ InferedTypeTag.Number; InferedTypeTag.Boolean ],
                 [ InferedTypeTag.Number, (Single, InferedType.Primitive(typeof<int>, None, false))
                   InferedTypeTag.Boolean, (Single, InferedType.Primitive(typeof<bool>, None, false)) ] |> Map.ofList)

    let actual = BsonInference.inferType "" source
    actual |> shouldEqual expected

[<Test>]
let ``Infers heterogeneous type of InferedType.Primitives and nulls``() =
    let source =
        BsonArray [ BsonInt32 1 :> BsonValue
                    BsonBoolean true :> BsonValue
                    BsonNull.Value :> BsonValue ]

    let expected =
        InferedType.Collection
                ([ InferedTypeTag.Number; InferedTypeTag.Boolean; InferedTypeTag.Null ],
                 [ InferedTypeTag.Null, (Single, InferedType.Null)
                   InferedTypeTag.Number, (Single, InferedType.Primitive(typeof<int>, None, false))
                   InferedTypeTag.Boolean, (Single, InferedType.Primitive(typeof<bool>, None, false)) ] |> Map.ofList)

    let actual = BsonInference.inferType "" source
    actual |> shouldEqual expected

[<Test>]
let ``Infers heterogeneous type of InferedType.Primitives and records``() =
    let source =
        BsonArray [ BsonDocument("a", BsonInt32 0) :> BsonValue
                    BsonInt32 1 :> BsonValue
                    BsonInt32 2 :> BsonValue ]

    let prop = { InferedProperty.Name="a"; Type=InferedType.Primitive(typeof<int>, None, false) }
    let expected =
        InferedType.Collection
                ([ InferedTypeTag.Record None; InferedTypeTag.Number ],
                 [ InferedTypeTag.Number, (Multiple, InferedType.Primitive(typeof<int>, None, false))
                   InferedTypeTag.Record None, (Single, toRecord [ prop ]) ] |> Map.ofList)

    let actual = BsonInference.inferType "" source
    actual |> shouldEqual expected

[<Test>]
let ``Merges types in a collection of collections``() =
    let source =
        BsonArray [
            BsonArray [ BsonDocument([ BsonElement("a", BsonBoolean true)
                                       BsonElement("c", BsonInt32 0) ])
                        BsonDocument([ BsonElement("b", BsonInt32 1)
                                       BsonElement("c", BsonInt32 0) ]) ]
            BsonArray [ BsonDocument([ BsonElement("b", BsonDouble 1.1)
                                       BsonElement("c", BsonInt32 0) ]) ]
        ]

    let expected =
        [ { InferedProperty.Name = "a"; Type = InferedType.Primitive(typeof<bool>, None, true) }
          { Name = "c"; Type = InferedType.Primitive(typeof<int32>, None, false) }
          { Name = "b"; Type = InferedType.Primitive(typeof<float>, None, true) } ]
        |> toRecord
        |> SimpleCollection
        |> SimpleCollection

    let actual = BsonInference.inferType "" source
    actual |> shouldEqual expected

[<Test>]
let ``Unions properties of records in a collection``() =
    let source =
        BsonArray [ BsonDocument([ BsonElement("a", BsonInt32 1)
                                   BsonElement("b", BsonString "") ])
                    BsonDocument([ BsonElement("a", BsonDouble 1.2)
                                   BsonElement("c", BsonBoolean true) ]) ]

    let expected =
        [ { InferedProperty.Name = "a"; Type = InferedType.Primitive(typeof<float>, None, false) }
          { Name = "b"; Type = InferedType.Primitive(typeof<string>, None, true) }
          { Name = "c"; Type = InferedType.Primitive(typeof<bool>, None, true) } ]
        |> toRecord
        |> SimpleCollection

    let actual = BsonInference.inferType "" source
    actual |> shouldEqual expected

[<Test>]
let ``Null makes a string optional``() =
    let source =
        BsonArray [ BsonDocument("a", BsonNull.Value)
                    BsonDocument("a", BsonString "10") ]

    let expected =
        [ { InferedProperty.Name = "a"; Type = InferedType.Primitive(typeof<string>, None, true) } ]
        |> toRecord
        |> SimpleCollection

    let actual = BsonInference.inferType "" source
    actual |> shouldEqual expected

[<Test>]
let ``Null makes a DateTime optional``() =
    let source =
        BsonArray [ BsonDocument("a", BsonNull.Value)
                    BsonDocument("a", BsonDateTime 10L) ]

    let expected =
        [ { InferedProperty.Name = "a"; Type = InferedType.Primitive(typeof<DateTime>, None, true) } ]
        |> toRecord
        |> SimpleCollection

    let actual = BsonInference.inferType "" source
    actual |> shouldEqual expected

[<Test>]
let ``Null makes a record optional``() =
    let source =
        BsonArray [ BsonDocument("a", BsonNull.Value)
                    BsonDocument("a", BsonDocument("b", BsonInt32 10)) ]

    let prop = { InferedProperty.Name = "b"; Type = InferedType.Primitive(typeof<int>, None, false) }
    let expected =
        [ { InferedProperty.Name = "a"; Type = InferedType.Record(Some "a", [prop], true) } ]
        |> toRecord
        |> SimpleCollection

    let actual = BsonInference.inferType "" source
    actual |> shouldEqual expected

[<Test>]
let ``Infers mixed fields of a record as heterogeneous type``() =
    let source =
        BsonArray [ BsonDocument("a", BsonString "hi")
                    BsonDocument("a", BsonInt32 2)
                    BsonDocument("a", BsonInt64 2147483648L) ]

    let cases =
        Map.ofSeq [ InferedTypeTag.String, InferedType.Primitive(typeof<string>, None, false)
                    InferedTypeTag.Number, InferedType.Primitive(typeof<int64>, None, false) ]

    let expected =
        [ { InferedProperty.Name = "a"; Type = InferedType.Heterogeneous cases }]
        |> toRecord
        |> SimpleCollection

    let actual = BsonInference.inferType "" source
    actual |> shouldEqual expected

[<Test>]
let ``Inference of multiple nulls works``() =
    let source =
        BsonArray [ BsonInt32 0 :> BsonValue
                    BsonArray [ BsonDocument("a", BsonNull.Value)
                                BsonDocument("a", BsonNull.Value) ] :> BsonValue ]

    let prop = { InferedProperty.Name = "a"; Type = InferedType.Null }
    let expected =
        InferedType.Collection
            ([ InferedTypeTag.Number; InferedTypeTag.Collection ],
             [ InferedTypeTag.Collection, (Single, SimpleCollection(toRecord [prop]))
               InferedTypeTag.Number, (Single, InferedType.Primitive(typeof<int>, None, false)) ] |> Map.ofList)

    let actual = BsonInference.inferType "" source
    actual |> shouldEqual expected
