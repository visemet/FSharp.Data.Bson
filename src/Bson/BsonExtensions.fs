/// Extension methods that can be used to work with BsonValue in a less safe, but more convenient way.
/// This module also provides the dynamic operator.

#if FX_NO_DEFAULT_PARAMETER_VALUE_ATTRIBUTE

namespace System.Runtime.InteropServices

open System

[<AttributeUsageAttribute(AttributeTargets.Parameter, Inherited = false)>]
type OptionalAttribute() = 
    inherit Attribute()

#endif

namespace FSharp.Data

open System
open System.Globalization
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open FSharp.Data
open FSharp.Data.Runtime
open Microsoft.FSharp.Core

[<Extension>]
type BsonExtensions =

  /// Get a sequence of key-value pairs representing the properties of an object
  [<Extension;CompilerMessageAttribute("This method is intended for use from C# only.", 10001, IsHidden=true)>]
  static member Properties(x:BsonValue) =
    match x with
      | BsonValue.Document properties -> properties
      | _ -> [| |]

  /// Get property of a BSON object. Fails if the value is not an object
  /// or if the property is not present
  [<Extension>]
  static member GetProperty(x, propertyName) = 
    match x with
    | BsonValue.Document properties -> 
        match Array.tryFind (fst >> ((=) propertyName)) properties with 
        | Some (_, value) -> value
        | None -> failwithf "Didn't find property '%s' in %s" propertyName <| x.ToString()
    | _ -> failwithf "Not an object: %s" <| x.ToString()

  /// Try to get a property of a BSON value.
  /// Returns None if the value is not an object or if the property is not present.
  [<Extension>]
  static member TryGetProperty(x, propertyName) = 
    match x with
    | BsonValue.Document properties -> 
        Array.tryFind (fst >> ((=) propertyName)) properties |> Option.map snd
    | _ -> None

  /// Assuming the value is an object, get value with the specified name
  [<Extension>] 
  static member inline Item(x, propertyName) = BsonExtensions.GetProperty(x, propertyName)

  /// Get all the elements of a BSON value.
  /// Returns an empty array if the value is not a BSON array.
  [<Extension>]
  static member AsArray(x:BsonValue) = 
    match x with
    | (BsonValue.Array elements) -> elements
    | _ -> [| |]

  /// Get all the elements of a BSON value (assuming that the value is an array)
  [<Extension>] 
  static member inline GetEnumerator(x) = BsonExtensions.AsArray(x) |> Array.toSeq

  /// Try to get the value at the specified index, if the value is a BSON array.
  [<Extension>] 
  static member inline Item(x, index) = BsonExtensions.AsArray(x).[index]

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module BsonExtensions =
  /// Get property of a BSON object (assuming that the value is an object)
  let (?) (bsonObject:BsonValue) propertyName = bsonObject.GetProperty(propertyName)

  type BsonValue with
    member x.Properties =
      match x with
      | BsonValue.Document properties -> properties
      | _ -> [| |]

// TODO: needs more consideration
#if ENABLE_BSONEXTENSIONS_OPTIONS

/// Extension methods that can be used to work with BsonValue in more convenient way.
/// This module also provides the dynamic operator.
module Options =

  open System.Runtime.CompilerServices
  
  type BsonValue with
  
    /// Get a sequence of key-value pairs representing the properties of an object
    member x.Properties = 
      match x with
      | BsonValue.Record properties -> properties
      | _ -> [| |]
  
    /// Try to get a property of a BSON value.
    /// Returns None if the value is not an object or if the property is not present.
    member x.TryGetProperty(propertyName) = 
      match x with
      | BsonValue.Record properties -> 
          Array.tryFind (fst >> ((=) propertyName)) properties |> Option.map snd
      | _ -> None
  
    /// Try to get a property of a BSON value.
    /// Returns None if the value is not a BSON object or if the property is not present.
    member inline x.Item with get(propertyName) = x.TryGetProperty(propertyName)
  
    /// Get all the elements of a BSON value.
    /// Returns an empty array if the value is not a BSON array.
    member x.AsArray() = 
      match x with
      | BsonValue.Array elements -> elements
      | _ -> [| |]

    /// Get all the elements of a BSON value (assuming that the value is an array)
    member inline x.GetEnumerator() = x.AsArray().GetEnumerator()
  
    /// Try to get the value at the specified index, if the value is a BSON array.
    member inline x.Item with get(index) = x.AsArray().[index]
  
    /// Get the string value of an element (assuming that the value is a scalar)
    member x.AsString(?cultureInfo) =
      let cultureInfo = defaultArg cultureInfo  CultureInfo.InvariantCulture
      BsonConversions.AsString (*useNoneForNullOrWhiteSpace*)false cultureInfo x
  
    /// Get a number as an integer (assuming that the value fits in integer)
    member x.AsInteger(?cultureInfo) = 
      let cultureInfo = defaultArg cultureInfo  CultureInfo.InvariantCulture
      BsonConversions.AsInteger cultureInfo x
  
    /// Get a number as a 64-bit integer (assuming that the value fits in 64-bit integer)
    member x.AsInteger64(?cultureInfo) = 
      let cultureInfo = defaultArg cultureInfo  CultureInfo.InvariantCulture
      BsonConversions.AsInteger64 cultureInfo x
  
    /// Get a number as a decimal (assuming that the value fits in decimal)
    member x.AsDecimal(?cultureInfo) = 
      let cultureInfo = defaultArg cultureInfo  CultureInfo.InvariantCulture
      BsonConversions.AsDecimal cultureInfo x
  
    /// Get a number as a float (assuming that the value is convertible to a float)
    member x.AsFloat(?cultureInfo, ?missingValues) = 
      let cultureInfo = defaultArg cultureInfo  CultureInfo.InvariantCulture
      let missingValues = defaultArg missingValues TextConversions.DefaultMissingValues
      BsonConversions.AsFloat missingValues (*useNoneForMissingValues*)true cultureInfo x
  
    /// Get the boolean value of an element (assuming that the value is a boolean)
    member x.AsBoolean(?cultureInfo) =
      let cultureInfo = defaultArg cultureInfo  CultureInfo.InvariantCulture
      BsonConversions.AsBoolean cultureInfo x
  
    /// Get the datetime value of an element (assuming that the value is a string
    /// containing well-formed ISO date or MSFT BSON date)
    member x.AsDateTime(?cultureInfo) = 
      let cultureInfo = defaultArg cultureInfo  CultureInfo.InvariantCulture
      BsonConversions.AsDateTime cultureInfo x
  
    /// Get the guid value of an element (assuming that the value is a guid)
    member x.AsGuid() =
      BsonConversions.AsGuid x
  
    /// Get inner text of an element
    member x.InnerText =     
      match x.AsString() with
      | Some str -> str
      | None -> x.AsArray() |> Array.map (fun e -> e.InnerText) |> String.concat ""
  
  [<Extension>] 
  [<AbstractClass>]
  type BsonValueOptionExtensions() = 
  
    /// Get a sequence of key-value pairs representing the properties of an object
    [<Extension>] 
    static member Properties(x) = 
      match x with
      | Some (bson:BsonValue) -> bson.Properties
      | None -> [| |]
  
    /// Try to get a property of a BSON value.
    /// Returns None if the value is not an object or if the property is not present.
    [<Extension>] 
    static member TryGetProperty(x, propertyName) = 
      match x with
      | Some (BsonValue.Record properties) -> 
          Array.tryFind (fst >> ((=) propertyName)) properties |> Option.map snd
      | _ -> None
  
    /// Try to get a property of a BSON value.
    /// Returns None if the value is not a BSON object or if the property is not present.
    [<Extension>] 
    static member inline Item(x, propertyName) = BsonValueOptionExtensions.TryGetProperty(x, propertyName)
  
    /// Get all the elements of a BSON value.
    /// Returns an empty array if the value is not a BSON array.
    [<Extension>] 
    static member AsArray(x) = 
      match x with
      | Some (BsonValue.Array elements) -> elements
      | _ -> [| |]

    /// Get all the elements of a BSON value (assuming that the value is an array)
    [<Extension>] 
    static member inline GetEnumerator(x) = BsonValueOptionExtensions.AsArray(x).GetEnumerator()
  
    /// Try to get the value at the specified index, if the value is a BSON array.
    [<Extension>] 
    static member inline Item(x, index) = BsonValueOptionExtensions.AsArray(x).[index]
  
    /// Get the string value of an element (assuming that the value is a scalar)
    [<Extension>] 
    static member AsString(x, ?cultureInfo) =
      let cultureInfo = defaultArg cultureInfo  CultureInfo.InvariantCulture
      x |> Option.bind (BsonConversions.AsString (*useNoneForNullOrWhiteSpace*)false cultureInfo)
  
    /// Get a number as an integer (assuming that the value fits in integer)
    [<Extension>] 
    static member AsInteger(x, ?cultureInfo) = 
      let cultureInfo = defaultArg cultureInfo  CultureInfo.InvariantCulture
      x |> Option.bind (BsonConversions.AsInteger cultureInfo)
  
    /// Get a number as a 64-bit integer (assuming that the value fits in 64-bit integer)
    [<Extension>] 
    static member AsInteger64(x, ?cultureInfo) = 
      let cultureInfo = defaultArg cultureInfo  CultureInfo.InvariantCulture
      x |> Option.bind (BsonConversions.AsInteger64 cultureInfo)
  
    /// Get a number as a decimal (assuming that the value fits in decimal)
    [<Extension>] 
    static member AsDecimal(x, ?cultureInfo) = 
      let cultureInfo = defaultArg cultureInfo  CultureInfo.InvariantCulture
      x |> Option.bind (BsonConversions.AsDecimal cultureInfo)
  
    /// Get a number as a float (assuming that the value is convertible to a float)
    [<Extension>] 
    static member AsFloat(x, ?cultureInfo, ?missingValues) = 
      let cultureInfo = defaultArg cultureInfo  CultureInfo.InvariantCulture
      let missingValues = defaultArg missingValues TextConversions.DefaultMissingValues
      x |> Option.bind (BsonConversions.AsFloat missingValues (*useNoneForMissingValues*)true cultureInfo)
  
    /// Get the boolean value of an element (assuming that the value is a boolean)
    [<Extension>] 
    static member AsBoolean(x, ?cultureInfo) =
      let cultureInfo = defaultArg cultureInfo  CultureInfo.InvariantCulture
      x |> Option.bind (BsonConversions.AsBoolean cultureInfo)
  
    /// Get the datetime value of an element (assuming that the value is a string
    /// containing well-formed ISO date or MSFT BSON date)
    [<Extension>] 
    static member AsDateTime(x, ?cultureInfo) = 
      let cultureInfo = defaultArg cultureInfo  CultureInfo.InvariantCulture
      x |> Option.bind (BsonConversions.AsDateTime cultureInfo)
  
    /// Get the guid value of an element (assuming that the value is a guid)
    [<Extension>] 
    static member AsGuid(x) =
      x |> Option.bind BsonConversions.AsGuid
  
    /// Get inner text of an element
    [<Extension>] 
    static member InnerText(x) =
      match BsonValueOptionExtensions.AsString(x) with
      | Some str -> str
      | None -> BsonValueOptionExtensions.AsArray(x) |> Array.map (fun e -> e.InnerText) |> String.concat ""
  
  /// [omit]
  type BsonValueOverloads = BsonValueOverloads with
      static member inline ($) (x:BsonValue                 , BsonValueOverloads) = fun propertyName -> x.TryGetProperty propertyName
      static member inline ($) (x:BsonValue option          , BsonValueOverloads) = fun propertyName -> x |> Option.bind (fun x -> x.TryGetProperty propertyName)
  
  /// Get property of a BSON value (assuming that the value is an object)
  let inline (?) x (propertyName:string) = (x $ BsonValueOverloads) propertyName

#endif
