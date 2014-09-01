// --------------------------------------------------------------------------------------
// Helper operations for converting converting bsonValue values to other types
// --------------------------------------------------------------------------------------

namespace BsonProvider.Runtime

open System
open MongoDB.Bson
open FSharp.Data
open FSharp.Data.Runtime

/// Conversions from BsonValue to string/int/int64/float/boolean/datetime options
type BsonConversions =

    static member AsString (bsonValue : BsonValue) =
        match bsonValue.BsonType with
        | BsonType.String -> Some bsonValue.AsString
        | _ -> None

    static member AsInteger (bsonValue : BsonValue) =
        match bsonValue.BsonType with
        | BsonType.Int32 -> Some bsonValue.AsInt32
        | BsonType.Int64 -> Some <| int bsonValue.AsInt64
        | BsonType.Double -> Some <| int bsonValue.AsDouble
        | _ -> None

    static member AsInteger64 (bsonValue : BsonValue) =
        match bsonValue.BsonType with
        | BsonType.Int32 -> Some <| int64 bsonValue.AsInt32
        | BsonType.Int64 -> Some <| bsonValue.AsInt64
        | BsonType.Double -> Some <| int64 bsonValue.AsDouble
        | _ -> None

    static member AsFloat (bsonValue : BsonValue) =
        match bsonValue.BsonType with
        | BsonType.Int32 -> Some <| float bsonValue.AsInt32
        | BsonType.Int64 -> Some <| float bsonValue.AsInt64
        | BsonType.Double -> Some bsonValue.AsDouble
        | _ -> None

    static member AsBoolean (bsonValue : BsonValue) =
        match bsonValue.BsonType with
        | BsonType.Boolean -> Some bsonValue.AsBoolean
        | _ -> None

    static member AsDateTime (bsonValue : BsonValue) =
        match bsonValue.BsonType with
        | BsonType.DateTime -> Some <| bsonValue.AsBsonDateTime.ToUniversalTime()
        | _ -> None

    static member AsObjectId (bsonValue : BsonValue) =
        match bsonValue.BsonType with
        | BsonType.ObjectId -> Some bsonValue.AsObjectId
        | _ -> None
