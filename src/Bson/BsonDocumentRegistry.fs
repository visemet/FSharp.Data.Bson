namespace BsonProvider.Runtime

open System.Collections.Generic
open MongoDB.Bson

[<RequireQualifiedAccess>]
module DocumentRegistry =

    let private dict = Dictionary<string, seq<BsonDocument>>()

    let Add key samples = dict.Add(key, samples)

    let Remove key = dict.Remove key

    let Clear() = dict.Clear()

    let GetValue key = dict.[key]

    let TryGetValue key =
        match dict.TryGetValue key with
        | false, _ -> None
        | true, value -> Some value
