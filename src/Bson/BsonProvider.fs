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

        let sample = args.[0] :?> string
        let sampleIsList = args.[1] :?> bool
        let rootName = args.[2] :?> string
        let rootName = if String.IsNullOrWhiteSpace rootName then "Root" else NameUtils.singularize rootName
        let cultureStr = args.[3] :?> string
        let encodingStr = args.[4] :?> string
        let resolutionFolder = args.[5] :?> string
        let resource = args.[6] :?> string

        let invalidChars = [ for c in "\"|<>{}[]," -> c ] @ [ for i in 0..31 -> char i ] |> set
        let tryGetUri str =
            match Uri.TryCreate(str, UriKind.RelativeOrAbsolute) with
            | false, _ -> None
            | true, uri ->
                if str.Trim() = "" || not uri.IsAbsoluteUri && Seq.exists invalidChars.Contains str
                then None else Some uri

        let uri =
            match tryGetUri sample with
            | Some uri -> uri
            | None -> failwith "was not a file"

        let getSpecFromSamples samples =

            let inferedType =
                [ for sampleBson in samples -> BsonInference.inferType "" sampleBson ]
                |> Seq.fold (StructuralInference.subtypeInfered (*allowEmptyValues*)false) StructuralTypes.Top

            let ctx = BsonGenerationContext.Create(cultureStr, tpType, replacer)
            let result = BsonTypeBuilder.generateBsonType ctx (*canPassAllConversionCallingTypes*)false (*optionalityHandledByParent*)false rootName inferedType

            [
                let getSampleCode = fun _ ->
                    result.GetConverter ctx <@@ BsonTop.CreateList(File.Open(sample, FileMode.Open)) @@>

                let resultTypeArray = result.ConvertedType.MakeArrayType()

                // Generate static GetSample method
                yield ProvidedMethod("GetSamples", [], resultTypeArray, IsStaticMethod = true, InvokeCode = getSampleCode)
            ] |> tpType.AddMembers

            { GeneratedType = tpType
              RepresentationType = result.ConvertedType
              CreateFromTextReader = fun _reader -> failwith "not implemented"
              CreateFromTextReaderForSampleList = fun _reader -> failwith "not implemented" }

        let exhausted = ref false

        let resolver =
            { ResolutionType = DesignTime
              DefaultResolutionFolder = cfg.ResolutionFolder
              ResolutionFolder = resolutionFolder }

        let path =
            match resolver.TryResolveToPath uri with
            | Some path -> path
            | None -> failwith "could not resolve as file"

        use file = File.Open(path, FileMode.Open)
        let samples =
            seq {
                while not !exhausted do
                    match BsonSerializer.Deserialize<BsonDocument>(file) with
                    | null -> exhausted := true
                    | doc -> yield doc
            }

        let spec =
            samples
            |> Seq.take 10
            |> getSpecFromSamples

        spec.GeneratedType

    // Add static parameter that specifies the API we want to get (compile-time) 
    let parameters = 
        [ ProvidedStaticParameter("Sample", typeof<string>)
          ProvidedStaticParameter("SampleIsList", typeof<bool>, parameterDefaultValue = false) 
          ProvidedStaticParameter("RootName", typeof<string>, parameterDefaultValue = "Root") 
          ProvidedStaticParameter("Culture", typeof<string>, parameterDefaultValue = "") 
          ProvidedStaticParameter("Encoding", typeof<string>, parameterDefaultValue = "") 
          ProvidedStaticParameter("ResolutionFolder", typeof<string>, parameterDefaultValue = "")
          ProvidedStaticParameter("EmbeddedResource", typeof<string>, parameterDefaultValue = "") ]

    let helpText = 
        """<summary>Typed representation of a BSON document.</summary>
           <param name='Sample'>Location of a BSON sample file or a string containing a sample BSON document.</param>
           <param name='SampleIsList'>If true, sample should be a list of individual samples for the inference.</param>
           <param name='RootName'>The name to be used to the root type. Defaults to `Root`.</param>
           <param name='Culture'>The culture used for parsing numbers and dates. Defaults to the invariant culture.</param>
           <param name='Encoding'>The encoding used to read the sample. You can specify either the character set name or the codepage number. Defaults to UTF8 for files, and to ISO-8859-1 the for HTTP requests, unless `charset` is specified in the `Content-Type` response header.</param>
           <param name='ResolutionFolder'>A directory that is used when resolving relative file references (at design time and in hosted execution).</param>
           <param name='EmbeddedResource'>When specified, the type provider first attempts to load the sample from the specified resource
                (e.g. 'MyCompany.MyAssembly, resource_name.bson'). This is useful when exposing types generated by the type provider.</param>"""

    do bsonProvTy.AddXmlDoc helpText
    do bsonProvTy.DefineStaticParameters(parameters, buildTypes)

    // Register the main type with F# compiler
    do this.AddNamespace(ns, [ bsonProvTy ])
