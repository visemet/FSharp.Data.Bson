#I "../bin"
#r "MongoDB.Bson.dll"
#r "FSharp.Data.Bson.dll"

open System
open System.IO
open System.Text
open MongoDB.Bson
open BsonProvider
open BsonProvider.Runtime

type Zips = BsonProvider<"/Users/maxh/Dropbox/zips.bson">
let samples = Zips.GetSamples()

printf "%A\n" samples.[0].Loc

let myZip = BsonDocument([ BsonElement("_id", BsonString "12345")
                           BsonElement("city", BsonString "CITY")
                           BsonElement("loc", BsonArray())
                           BsonElement("pop", BsonInt32 42)
                           BsonElement("state", BsonString "ST") ])

printf "%A\n" (new Zips.Root(myZip)).City
