// -----------------------------------------------------------------------------
// Implements type inference for BSON
// -----------------------------------------------------------------------------

module BsonProvider.ProviderImplementation.BsonInference

open System
open MongoDB.Bson
open FSharp.Data
open FSharp.Data.Runtime
open FSharp.Data.Runtime.StructuralTypes

/// Infer the type of a BSON value.
let rec inferType parentName (bsonValue : BsonValue) =
    match bsonValue.BsonType with
    | BsonType.Null -> InferedType.Null
    | BsonType.Boolean -> InferedType.Primitive (typeof<bool>, None, false)
    | BsonType.Int32 -> InferedType.Primitive (typeof<int>, None, false)
    | BsonType.Int64 -> InferedType.Primitive (typeof<int64>, None, false)
    | BsonType.Double -> InferedType.Primitive (typeof<float>, None, false)
    | BsonType.String -> InferedType.Primitive (typeof<string>, None, false)
    | BsonType.DateTime -> InferedType.Primitive (typeof<DateTime>, None, false)

    | BsonType.Binary -> InferedType.Primitive (typeof<BsonBinaryData>, None, false)
    | BsonType.ObjectId -> InferedType.Primitive (typeof<ObjectId>, None, false)

    | BsonType.Array elems ->
        let elemName = NameUtils.singularize parentName
        let allowEmptyValues = false

        bsonValue.AsBsonArray
        |> Seq.map (inferType elemName)
        |> StructuralInference.inferCollectionType allowEmptyValues

    | BsonType.Document elems ->
        let recordName =
            if String.IsNullOrEmpty parentName
            then None
            else Some parentName
        let fields =
            [ for elem in bsonValue.AsBsonDocument ->
                let typ = inferType elem.Name elem.Value
                { InferedProperty.Name = elem.Name; Type = typ} ]

        InferedType.Record (recordName, fields, false)

    | _ -> InferedType.Primitive (typeof<BsonType>, None, false)
