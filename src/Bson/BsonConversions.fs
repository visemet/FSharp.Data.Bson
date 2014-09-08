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
// Helper operations for converting BSON values to other types
// -----------------------------------------------------------------------------

namespace BsonProvider.Runtime

open MongoDB.Bson

/// Conversions from a BsonValue to string, int, int64, float, boolean,
/// DateTime, and ObjectId options.
type BsonConversions =

    static member AsString (value:BsonValue) =
        match value.BsonType with
        | BsonType.String -> Some value.AsString
        | _ -> None

    static member AsInteger (value:BsonValue) =
        match value.BsonType with
        | BsonType.Int32 -> Some value.AsInt32
        | BsonType.Int64 -> Some <| int value.AsInt64
        | BsonType.Double -> Some <| int value.AsDouble
        | _ -> None

    static member AsInteger64 (value:BsonValue) =
        match value.BsonType with
        | BsonType.Int32 -> Some <| int64 value.AsInt32
        | BsonType.Int64 -> Some <| value.AsInt64
        | BsonType.Double -> Some <| int64 value.AsDouble
        | _ -> None

    static member AsFloat (value:BsonValue) =
        match value.BsonType with
        | BsonType.Int32 -> Some <| float value.AsInt32
        | BsonType.Int64 -> Some <| float value.AsInt64
        | BsonType.Double -> Some value.AsDouble
        | _ -> None

    static member AsBoolean (value:BsonValue) =
        match value.BsonType with
        | BsonType.Boolean -> Some value.AsBoolean
        | _ -> None

    static member AsDateTime (value:BsonValue) =
        match value.BsonType with
        | BsonType.DateTime -> Some <| value.AsBsonDateTime.ToUniversalTime()
        | _ -> None

    static member AsObjectId (value:BsonValue) =
        match value.BsonType with
        | BsonType.ObjectId -> Some value.AsObjectId
        | _ -> None
