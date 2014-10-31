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

// -----------------------------------------------------------------------------
// Implements type inference for BSON
// -----------------------------------------------------------------------------

module BsonProvider.ProviderImplementation.BsonInference

open System
open MongoDB.Bson
open FSharp.Data.Runtime
open FSharp.Data.Runtime.StructuralTypes
open BsonProvider.Runtime

/// Infer the type of a BSON value.
let rec inferType parentName (bsonValue:BsonValue) =
    match bsonValue.BsonType with
    | BsonType.Boolean -> InferedType.Primitive (typeof<bool>, None, false)
    | BsonType.Int32 -> InferedType.Primitive (typeof<int>, None, false)
    | BsonType.Int64 -> InferedType.Primitive (typeof<int64>, None, false)
    | BsonType.Double -> InferedType.Primitive (typeof<float>, None, false)
    | BsonType.String -> InferedType.Primitive (typeof<string>, None, false)
    | BsonType.DateTime -> InferedType.Primitive (typeof<DateTime>, None, false)
    | BsonType.ObjectId -> InferedType.Primitive (typeof<ObjectId>, None, false)

    | BsonType.Array elems ->
        let elemName = NameUtils.singularize parentName
        bsonValue.AsBsonArray
        |> Seq.map (inferType elemName)
        |> StructuralInference.inferCollectionType (*allowEmptyValues*)false

    | BsonType.Document elems ->
        let recordName =
            if String.IsNullOrEmpty parentName then None
            else Some parentName
        let fields =
            [ for elem in bsonValue.AsBsonDocument ->
                let typ = inferType elem.Name elem.Value
                { InferedProperty.Name = elem.Name; Type = typ } ]
        InferedType.Record (recordName, fields, false)

    | OptionalBsonType _ -> InferedType.Null
    | _ -> InferedType.Primitive (typeof<BsonValue>, None, false)
