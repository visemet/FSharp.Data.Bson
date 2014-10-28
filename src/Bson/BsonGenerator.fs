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
// BSON type provider - generates code for accessing inferred elements
// -----------------------------------------------------------------------------

namespace BsonProvider.ProviderImplementation

open System
open System.Collections.Generic
open Microsoft.FSharp.Quotations
open MongoDB.Bson
open FSharp.Data.Runtime
open FSharp.Data.Runtime.StructuralTypes
open ProviderImplementation
open ProviderImplementation.ProvidedTypes
open BsonProvider.Runtime
open BsonProvider.ProviderImplementation.BsonConversionsGenerator

#nowarn "10001"

/// Context that is used to generate the BSON types
type BsonGenerationContext =
    {
        TypeProviderType : ProvidedTypeDefinition
        UniqueNiceName : string -> string // to nameclash type names
        IBsonTopType : Type
        BsonValueType : Type
        BsonRuntimeType : Type
        TypeCache : Dictionary<InferedType, ProvidedTypeDefinition>
        GenerateConstructors : bool
    }

    static member Create(tpType, ?uniqueNiceName, ?typeCache) =
        let uniqueNiceName = defaultArg uniqueNiceName (NameUtils.uniqueGenerator NameUtils.nicePascalName)
        let typeCache = defaultArg typeCache (Dictionary())
        BsonGenerationContext.Create(tpType, uniqueNiceName, typeCache, true)

    static member Create(tpType, uniqueNiceName, typeCache, generateConstructors) =
        {
            TypeProviderType = tpType
            UniqueNiceName = uniqueNiceName
            IBsonTopType = typeof<IBsonTop>
            BsonValueType = typeof<BsonValue>
            BsonRuntimeType = typeof<BsonRuntime>
            TypeCache = typeCache
            GenerateConstructors = generateConstructors
        }

    member x.MakeOptionType(typ:Type) =
        typedefof<option<_>>.MakeGenericType typ

type BsonGenerationResult =
    {
        ConvertedType : Type
        Converter : (Expr -> Expr) option
    }

    member x.GetConverter ctx =
        defaultArg x.Converter id

    member x.ConverterFunc ctx =
        ReflectionHelpers.makeDelegate (x.GetConverter ctx) ctx.IBsonTopType

    member x.ConvertedTypeErased ctx =
        if x.ConvertedType.IsArray then
            match x.ConvertedType.GetElementType() with
            | :? ProvidedTypeDefinition -> ctx.IBsonTopType.MakeArrayType()
            | _ -> x.ConvertedType
        else
            match x.ConvertedType with
            | :? ProvidedTypeDefinition -> ctx.IBsonTopType
            | _ -> x.ConvertedType

[<AutoOpen>]
module ActivePatterns =

    let (|MapWithNull|_|) (map:Map<_,_>) =
        if map.Count = 2 then
            match Map.toList map with
            | [ (InferedTypeTag.Null, _); elem ]
            | [ elem; (InferedTypeTag.Null, _) ] -> Some elem
            | _ -> None
        else None

module BsonTypeBuilder =

    let (?) = QuotationBuilder.(?)

    let private inferType typ =
        { ConvertedType = typ
          Converter = None }

    let private inferCollection (elemType:Type) conv =
        { ConvertedType = elemType.MakeArrayType()
          Converter = Some conv }

    // check if a type was already created for the inferedType before creating a new one
    let internal getOrCreateType ctx inferedType createType =

        // normalize properties of the inferedType which don't affect code generation
        let rec normalize topLevel = function
        | InferedType.Heterogeneous map ->
            map
            |> Map.map (fun _ inferedType -> normalize false inferedType)
            |> InferedType.Heterogeneous
        | InferedType.Collection (order, types) ->
            InferedType.Collection (order, Map.map (fun _ (multiplicity, inferedType) -> multiplicity, normalize false inferedType) types)
        | InferedType.Record (_, props, optional) ->
            let props =
              props
              |> List.map (fun { Name = name; Type = inferedType } -> { InferedProperty.Name = name; Type = normalize false inferedType })
            // optional only affects the parent, so at top level always set to true regardless of the actual value
            InferedType.Record (None, props, optional || topLevel)
        | x -> x

        let inferedType = normalize true inferedType
        let typ =
            match ctx.TypeCache.TryGetValue inferedType with
            | true, typ -> typ
            | _ ->
                let typ = createType()
                ctx.TypeCache.Add(inferedType, typ)
                typ

        { ConvertedType = typ
          Converter = None }

    let replaceWithBsonValue (ctx:BsonGenerationContext) typ =
        if typ = ctx.IBsonTopType then
            ctx.BsonValueType
        elif typ.IsArray && typ.GetElementType() = ctx.IBsonTopType then
            ctx.BsonValueType.MakeArrayType()
        elif typ.IsGenericType && typ.GetGenericArguments() = [| ctx.IBsonTopType |] then
            typ.GetGenericTypeDefinition().MakeGenericType ctx.BsonValueType
        else
            typ

    let rec generateBsonType ctx optionalityHandledByParent nameOverride inferedType =

        match inferedType with

        | InferedType.Primitive (inferedType, unit, optional) ->

            let typ, conv =
                PrimitiveInferedProperty.Create("", inferedType, optional, unit)
                |> convertBsonValue

            { ConvertedType = typ
              Converter = Some conv }

        | InferedType.Record (name, props, optional) -> getOrCreateType ctx inferedType <| fun () ->

            if optional && not optionalityHandledByParent then
                failwith "generateBsonType: optionality not handled for %A" inferedType

            let name =
                if String.IsNullOrEmpty nameOverride
                then match name with Some name -> name | _ -> "Record"
                else nameOverride
                |> ctx.UniqueNiceName

            // Generate new type for the record
            let objectTy = ProvidedTypeDefinition(name, Some ctx.IBsonTopType, HideObjectMethods = true)
            ctx.TypeProviderType.AddMember(objectTy)

            // to nameclash property names
            let makeUnique = NameUtils.uniqueGenerator NameUtils.nicePascalName
            makeUnique "BsonValue" |> ignore

            // Add all record fields as properties
            let members =
                [ for prop in props ->

                    let optionalityHandledByProperty =
                        match prop.Type with
                        | InferedType.Primitive (_, _, optional) -> optional
                        | _ -> false

                    let propResult = generateBsonType ctx (*optionalityHandledByParent*)true "" prop.Type
                    let propName = prop.Name

                    let getter = fun (Singleton doc) ->
                        if prop.Type.IsOptional then
                            ctx.BsonRuntimeType?ConvertOptionalProperty (propResult.ConvertedTypeErased ctx) (doc, propName, propResult.ConverterFunc ctx) :> Expr
                        else
                            propResult.GetConverter ctx <@@ BsonRuntime.GetPropertyPacked(%%doc, propName) @@>

                    let convertedType =
                        if prop.Type.IsOptional && not optionalityHandledByProperty then
                            ctx.MakeOptionType propResult.ConvertedType
                        else propResult.ConvertedType

                    let name = makeUnique prop.Name
                    prop.Name,
                    ProvidedProperty(name, convertedType, GetterCode = getter),
                    ProvidedParameter(NameUtils.niceCamelName name, replaceWithBsonValue ctx convertedType) ]

            let names, properties, parameters = List.unzip3 members
            objectTy.AddMembers properties

            if ctx.GenerateConstructors then

                objectTy.AddMember <|
                    ProvidedConstructor(parameters, InvokeCode = fun args ->
                        let properties =
                            Expr.NewArray(typeof<string * obj>,
                                          args
                                          |> List.mapi (fun i a -> Expr.NewTuple [ Expr.Value names.[i]
                                                                                   Expr.Coerce(a, typeof<obj>) ]))
                        <@@ BsonRuntime.CreateDocument(%%properties) @@>)

                let hasBsonValueType =
                    match parameters with
                    | [ prop ] -> prop.ParameterType = ctx.BsonValueType
                    | _ -> false

                if not hasBsonValueType then
                    objectTy.AddMember <|
                            ProvidedConstructor(
                                [ProvidedParameter("bsonValue", ctx.BsonValueType)],
                                InvokeCode = fun (Singleton arg) ->
                                    <@@ BsonTop.Create((%%arg:BsonValue), "") @@>)

            objectTy

        | InferedType.Collection (_, SingletonMap (_, (_, typ)))
        | InferedType.Collection (_, EmptyMap InferedType.Top typ) ->

            let elementResult = generateBsonType ctx (*optionalityHandledByParent*)false nameOverride typ

            let conv = fun (doc:Expr) ->
                ctx.BsonRuntimeType?ConvertArray (elementResult.ConvertedTypeErased ctx) (doc, elementResult.ConverterFunc ctx)

            inferCollection elementResult.ConvertedType conv

        | InferedType.Collection (_, MapWithNull (_, (_, typ))) ->
            let elementResult = generateBsonType ctx (*optionalityHandledByParent*)false nameOverride typ

            let conv = fun (doc:Expr) ->
                ctx.BsonRuntimeType?ConvertOptionalArray (elementResult.ConvertedTypeErased ctx) (doc, elementResult.ConverterFunc ctx)

            let convertedType = ctx.MakeOptionType elementResult.ConvertedType
            inferCollection convertedType conv

        | InferedType.Collection _ ->

            let conv = fun (doc:Expr) ->
                ctx.BsonRuntimeType?ConvertArray ctx.IBsonTopType (doc, ReflectionHelpers.makeDelegate id ctx.IBsonTopType)

            inferCollection ctx.IBsonTopType conv

        | InferedType.Top
        | InferedType.Heterogeneous _ -> inferType ctx.IBsonTopType

        | InferedType.Null -> inferType (ctx.MakeOptionType ctx.IBsonTopType)

        | InferedType.Json _ -> failwith "JSON type not supported"
