// ----------------------------------------------------------------------------------------------
// Conversions from string to various primitive types
// ----------------------------------------------------------------------------------------------

module BsonProvider.ProviderImplementation.BsonConversionsGenerator

open System
open Microsoft.FSharp.Quotations
open MongoDB.Bson
open FSharp.Data
open FSharp.Data.Runtime
open FSharp.Data.Runtime.StructuralTypes
open ProviderImplementation
open ProviderImplementation.QuotationBuilder
open BsonProvider.Runtime

#nowarn "10001"

let getConversionQuotation typ (value:Expr<BsonValue option>) =
    if typ = typeof<string> then
        <@@ BsonRuntime.ConvertString(%value) @@>
    elif typ = typeof<bool> || typ = typeof<Bit> then
        <@@ BsonRuntime.ConvertBoolean(%value) @@>
    elif typ = typeof<int> || typ = typeof<Bit0> || typ = typeof<Bit1> then
        <@@ BsonRuntime.ConvertInteger(%value) @@>
    elif typ = typeof<int64> then
        <@@ BsonRuntime.ConvertInteger64(%value) @@>
    elif typ = typeof<float> then
        <@@ BsonRuntime.ConvertFloat(%value) @@>
    elif typ = typeof<DateTime> then
        <@@ BsonRuntime.ConvertDateTime(%value) @@>
    elif typ = typeof<ObjectId> then
        <@@ BsonRuntime.ConvertObjectId(%value) @@>
    else failwithf "getConversionQuotation: Unsupported primitive type '%A'" typ

type BsonConversionCallingType =
    BsonDocument | BsonValueOption | BsonValueOptionAndPath

/// Creates a function that takes Expr<BsonValue option> and converts it to
/// an expression of other type - the type is specified by `field`
let convertBsonValue (replacer:AssemblyReplacer) canPassAllConversionCallingTypes (field:PrimitiveInferedProperty) =

    assert (field.TypeWithMeasure = field.RuntimeType)
    assert (field.Name = "")

    let returnType =
        match field.TypeWrapper with
        | TypeWrapper.None -> field.RuntimeType
        | TypeWrapper.Option -> typedefof<option<_>>.MakeGenericType field.RuntimeType
        | TypeWrapper.Nullable -> typedefof<Nullable<_>>.MakeGenericType field.RuntimeType
        |> replacer.ToRuntime

    let wrapInLetIfNeeded (value:Expr) getBody =
        match value with
        | Patterns.Var var ->
            let varExpr = Expr.Cast<'T> (Expr.Var var)
            getBody varExpr
        | _ ->
            let var = Var("value", typeof<'T>)
            let varExpr = Expr.Cast<'T> (Expr.Var var)
            Expr.Let(var, value, getBody varExpr)

    let convert (value:Expr) =
        let convert =  getConversionQuotation field.InferedType
        match field.TypeWrapper, canPassAllConversionCallingTypes with
        | TypeWrapper.None, true ->
            wrapInLetIfNeeded value <| fun (varExpr:Expr<BsonValueOptionAndPath>) ->
                typeof<BsonRuntime>?GetNonOptionalValue (field.RuntimeType) (<@ (%varExpr).Path @>, convert <@ (%varExpr).BsonOpt @>, <@ (%varExpr).BsonOpt @>)
        | TypeWrapper.None, false ->
            wrapInLetIfNeeded value <| fun (varExpr:Expr<IBsonTop>) ->
                typeof<BsonRuntime>?GetNonOptionalValue (field.RuntimeType) (<@ (%varExpr).Path() @>, convert <@ Some (%varExpr).BsonValue @>, <@ Some (%varExpr).BsonValue @>)
        | TypeWrapper.Option, true ->
            convert <@ (%%value:BsonValue option) @>
        | TypeWrapper.Option, false ->
            convert <@ Some (%%value:IBsonTop).BsonValue @>
        | TypeWrapper.Nullable, true ->
            typeof<TextRuntime>?OptionToNullable (field.RuntimeType) (convert <@ (%%value:BsonValue option) @>)
        | TypeWrapper.Nullable, false ->
            typeof<TextRuntime>?OptionToNullable (field.RuntimeType) (convert <@ Some (%%value:IBsonTop).BsonValue @>)
        |> replacer.ToRuntime

    let conversionCallingType =
        if canPassAllConversionCallingTypes then
            match field.TypeWrapper with
            | TypeWrapper.None -> BsonValueOptionAndPath
            | TypeWrapper.Option | TypeWrapper.Nullable -> BsonValueOption
        else BsonDocument

    returnType, convert, conversionCallingType
