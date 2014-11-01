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
// Conversions from BsonValue to various primitive types
// -----------------------------------------------------------------------------

module BsonProvider.ProviderImplementation.BsonConversionsGenerator

open System
open Microsoft.FSharp.Quotations
open MongoDB.Bson
open FSharp.Data.Runtime
open FSharp.Data.Runtime.StructuralTypes
open ProviderImplementation
open BsonProvider.Runtime

#nowarn "10001"

let private (?) = QuotationBuilder.(?)

let private getConversionQuotation typ (value:Expr<BsonValue option>) =
    if typ = typeof<string> then
        <@@ BsonRuntime.ConvertString(%value) @@>
    elif typ = typeof<bool> then
        <@@ BsonRuntime.ConvertBoolean(%value) @@>
    elif typ = typeof<int> then
        <@@ BsonRuntime.ConvertInteger(%value) @@>
    elif typ = typeof<int64> then
        <@@ BsonRuntime.ConvertInteger64(%value) @@>
    elif typ = typeof<float> then
        <@@ BsonRuntime.ConvertFloat(%value) @@>
    elif typ = typeof<DateTime> then
        <@@ BsonRuntime.ConvertDateTime(%value) @@>
    elif typ = typeof<ObjectId> then
        <@@ BsonRuntime.ConvertObjectId(%value) @@>
    else invalidArg "typ" "unsupported primitive type '%A'" typ

/// Creates a function that takes Expr<BsonValue option> and converts it to
/// an expression of other type - the type is specified by `field`
let convertBsonValue (field:PrimitiveInferedProperty) =

    let returnType =
        match field.TypeWrapper with
        | TypeWrapper.None -> field.RuntimeType
        | TypeWrapper.Option ->
            typedefof<option<_>>.MakeGenericType field.RuntimeType
        | TypeWrapper.Nullable ->
            typedefof<Nullable<_>>.MakeGenericType field.RuntimeType

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
        let convert = getConversionQuotation field.InferedType
        match field.TypeWrapper with
        | TypeWrapper.None ->
            wrapInLetIfNeeded value <| fun (top:Expr<IBsonTop>) ->
                let t = field.RuntimeType
                let converted = convert <@ Some (%top).BsonValue @>
                typeof<BsonRuntime>?GetNonOptionalValue t (top, converted)

        | TypeWrapper.Option ->
            convert <@ Some (%%value:IBsonTop).BsonValue @>

        | TypeWrapper.Nullable ->
            invalidOp "conversion to Nullable type not supported"

    returnType, convert
