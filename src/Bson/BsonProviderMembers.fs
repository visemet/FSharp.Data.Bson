namespace BsonProvider.ProviderImplementation

open System
open System.IO
open System.Reflection
open Microsoft.FSharp.Core.CompilerServices
open MongoDB.Bson
open MongoDB.Bson.Serialization
open ProviderImplementation.ProvidedTypes
open BsonProvider.Runtime
open BsonProvider.Runtime.IO

#nowarn "10001"

[<AutoOpen>]
module private Helpers =
    let invalidChars = [ for c in "\"|<>{}[]," -> c ] @ [ for i in 0..31 -> char i ] |> set

    let tryGetUri str =
        match Uri.TryCreate(str, UriKind.RelativeOrAbsolute) with
        | false, _ -> None
        | true, uri ->
            if str.Trim() = "" || not uri.IsAbsoluteUri && Seq.exists invalidChars.Contains str
            then None else Some uri

module internal Members =

    /// Generates the static GetSamples() method
    let private getSamples (ctx:BsonGenerationContext) (result:BsonGenerationResult)
                           path (resolver:UriResolver) =

        let resolver = { resolver with ResolutionType = DesignTime }
        let path =
            match tryGetUri path with
            | None -> failwith "path does not represent a location"
            | Some uri ->
                match resolver.TryResolveToPath uri with
                | None -> failwith "path could not be resolved as a file"
                | Some path -> path

        let resultTypeArray = result.ConvertedType.MakeArrayType()
        let m = ProvidedMethod("GetSamples", [], resultTypeArray, IsStaticMethod = true)

        m.InvokeCode <- fun _ ->
            use file = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read)
            let samples =
                [|
                    while file.Position <> file.Length do
                        let doc = BsonSerializer.Deserialize<BsonDocument>(file)
                        yield BsonTop.Create(doc, "")
                |]

            result.GetConverter ctx <@@ samples @@>

        m.AddXmlDoc "Returns the entire set of sample BSON documents"
        m

    let createAllMembers (cfg:TypeProviderConfig) (ctx:BsonGenerationContext) (result:BsonGenerationResult)
                         path limit resolutionFolder resource =

        let resolver =
            { ResolutionType = Runtime
              DefaultResolutionFolder = cfg.ResolutionFolder
              ResolutionFolder = resolutionFolder }

        [ getSamples ctx result path resolver :> MethodInfo ]
