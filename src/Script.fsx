#I "../bin"
#r "FSharp.Data.Bson.dll"

open System
open System.IO
open System.Text
open BsonProvider
open BsonProvider.Runtime

type Zips = BsonProvider<"/Users/maxh/Dropbox/zips.bson">
let samples = Zips.GetSamples()

printf "%A\n" samples.[0].Loc
