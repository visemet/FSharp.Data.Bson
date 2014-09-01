namespace BsonProvider.ProviderImplementation

open System
open System.IO
open ProviderImplementation
open ProviderImplementation.ProvidedTypes

type BsonProviderArgs =
    { Path : string
      InferLimit : int
      RootName : string
      ResolutionFolder : string
      EmbeddedResource : string }

type TypeProviderInstantiation =
    | Bson of BsonProviderArgs

    member x.GenerateType resolutionFolder runtimeAssembly =
        let f, args =
            match x with
            | Bson x ->
                (fun cfg -> new BsonProvider(cfg) :> TypeProviderForNamespaces),
                [| box x.Path
                   box x.InferLimit
                   box x.RootName
                   box x.ResolutionFolder
                   box x.EmbeddedResource |]
        Debug.generate resolutionFolder runtimeAssembly f args

    override x.ToString() =
        match x with
        | Bson x ->
            [ "Bson"
              x.Path
              x.InferLimit.ToString()
              x.RootName ]
        |> String.concat ","

    member x.ExpectedPath outputFolder =
        Path.Combine(outputFolder, (x.ToString().Replace(">", "&gt;").Replace("<", "&lt;").Replace("://", "_").Replace("/", "_") + ".expected"))

    member x.Dump resolutionFolder outputFolder runtimeAssembly signatureOnly ignoreOutput =
        let replace (oldValue:string) (newValue:string) (str:string) = str.Replace(oldValue, newValue)
        let output =
            x.GenerateType resolutionFolder runtimeAssembly
            |> match x with
               | _ -> Debug.prettyPrint signatureOnly ignoreOutput 10 100
            |> replace "FSharp.Data.Runtime." "FDR."
            |> if String.IsNullOrEmpty resolutionFolder then id else replace resolutionFolder "<RESOLUTION_FOLDER>"
        if outputFolder <> "" then
            File.WriteAllText(x.ExpectedPath outputFolder, output)
        output

    static member Parse (line:string) =
        let args = line.Split [|','|]
        match args.[0] with
        | "Bson" ->
            Bson { Path = args.[1]
                   InferLimit = int args.[2]
                   RootName = args.[3]
                   ResolutionFolder = ""
                   EmbeddedResource = "" }
        | _ -> failwithf "Unknown: %s" args.[0]

open System.Runtime.CompilerServices

[<assembly:InternalsVisibleToAttribute("BsonProvider.Tests.DesignTime")>]
do()
