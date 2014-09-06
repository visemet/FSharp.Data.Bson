namespace BsonProvider.Runtime

open System.Collections.Generic
open MongoDB.Bson

[<RequireQualifiedAccess>]
module DocumentRegistry =

    let private dict = Dictionary<string, seq<BsonDocument>>()

    let Add = dict.Add

    let Remove = dict.Remove

    let Clear = dict.Clear

    let GetValue key = dict.[key]

    let TryGetValue key =
        match dict.TryGetValue key with
        | false, _ -> None
        | true, value -> Some value
