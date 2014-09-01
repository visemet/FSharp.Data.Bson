namespace BsonProvider.ProviderImplementation

open System
open System.IO
open Microsoft.FSharp.Core.CompilerServices
open MongoDB.Bson
open MongoDB.Bson.Serialization
open ProviderImplementation
open ProviderImplementation.ProvidedTypes
open ProviderImplementation.ProviderHelpers
open FSharp.Data
open FSharp.Data.Runtime
open FSharp.Data.Runtime.StructuralTypes
open BsonProvider
open BsonProvider.Runtime
open BsonProvider.Runtime.IO
open BsonProvider.ProviderImplementation.Members

// ----------------------------------------------------------------------------------------------

#nowarn "10001"

[<TypeProvider>]
type public BsonProvider(cfg:TypeProviderConfig) as this =
    inherit DisposableTypeProviderForNamespaces()

    // Generate namespace and type 'BsonProvider.BsonProvider'
    let asm, version, replacer = AssemblyResolver.init cfg
    let ns = "BsonProvider"
    let bsonProvTy = ProvidedTypeDefinition(asm, ns, "BsonProvider", Some typeof<BsonValue>)

    let buildTypes (typeName:string) (args:obj[]) =

        // Generate the required type
        let tpType = ProvidedTypeDefinition(asm, ns, typeName, Some typeof<obj>)

        let path = args.[0] :?> string
        let limit = args.[1] :?> int
        let rootName = args.[2] :?> string
        // let rootName = if String.IsNullOrWhiteSpace rootName then "Root" else NameUtils.singularize rootName
        let resolutionFolder = args.[3] :?> string
        let resource = args.[4] :?> string

        let invalidChars = [ for c in "\"|<>{}[]," -> c ] @ [ for i in 0..31 -> char i ] |> set
        let tryGetUri str =
            match Uri.TryCreate(str, UriKind.RelativeOrAbsolute) with
            | false, _ -> None
            | true, uri ->
                if str.Trim() = "" || not uri.IsAbsoluteUri && Seq.exists invalidChars.Contains str
                then None else Some uri

        let uri =
            match tryGetUri path with
            | Some uri -> uri
            | None -> failwith "path was not a file"

        let getSpecFromSamples samples =

            let inferedType =
                [ for sampleBson in samples -> BsonInference.inferType "" sampleBson ]
                |> Seq.fold (StructuralInference.subtypeInfered (*allowEmptyValues*)false) StructuralTypes.Top

            let ctx = BsonGenerationContext.Create(tpType, replacer)
            let result = BsonTypeBuilder.generateBsonType ctx (*canPassAllConversionCallingTypes*)false (*optionalityHandledByParent*)false rootName inferedType

            createAllMembers cfg ctx result path limit resolutionFolder resource
            |> tpType.AddMembers

            { GeneratedType = tpType
              RepresentationType = result.ConvertedType
              CreateFromTextReader = fun _reader -> failwith "not implemented"
              CreateFromTextReaderForSampleList = fun _reader -> failwith "not implemented" }

        let resolver =
            { ResolutionType = DesignTime
              DefaultResolutionFolder = cfg.ResolutionFolder
              ResolutionFolder = resolutionFolder }

        let path =
            match resolver.TryResolveToPath uri with
            | Some path -> path
            | None -> failwith "could not resolve as file"

        use file = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read)
        let samples =
            seq {
                while file.Position <> file.Length do
                    yield BsonSerializer.Deserialize<BsonDocument>(file)
            }

        let spec =
            samples
            |> Seq.truncate 10
            |> getSpecFromSamples

        spec.GeneratedType

    // Add static parameter that specifies the API we want to get (compile-time)
    let parameters =
        [ ProvidedStaticParameter("Path", typeof<string>)
          ProvidedStaticParameter("InferLimit", typeof<int>, parameterDefaultValue = 100)
          ProvidedStaticParameter("RootName", typeof<string>, parameterDefaultValue = "Root")
          ProvidedStaticParameter("ResolutionFolder", typeof<string>, parameterDefaultValue = "")
          ProvidedStaticParameter("EmbeddedResource", typeof<string>, parameterDefaultValue = "") ]

    let helpText =
        """<summary>Typed representation of a BSON document.</summary>
           <param name='Path'>Location of a BSON sample file.</param>
           <param name='InferLimit'>Number of documents to use for inference. Defaults to `100`.
                If this is set as zero then all documents are used.</param>
           <param name='RootName'>Name used for the root type. Defaults to `Root`.</param>
           <param name='ResolutionFolder'>Directory used for resolving relative file references
                (at design time and in hosted execution).</param>
           <param name='EmbeddedResource'>When specified, the type provider first attempts to load
                the file from the specified resource (e.g. 'MyCompany.MyAssembly, resource_name.bson').
                This is useful for when exposing types generated by the type provider.</param>"""

    do bsonProvTy.AddXmlDoc helpText
    do bsonProvTy.DefineStaticParameters(parameters, buildTypes)

    // Register the main type with F# compiler
    do this.AddNamespace(ns, [ bsonProvTy ])
