namespace FsClr

open System
open System.IO
open System.Reflection
open Argu
open FSharp.Control
open Serilog
open FsCore
open Argu


module Cli =
    let inline parseArgs<'T when 'T :> IArgParserTemplate> args =
        let errorHandler =
            ProcessExiter (
                colorizer =
                    function
                    | ErrorCode.HelpText -> None
                    | _ -> Some ConsoleColor.Red
            )

        let parser =
            ArgumentParser.Create<'T> (
                programName =
                    Assembly.GetEntryAssembly().GetName().Name
                    + ".exe",
                errorHandler = errorHandler
            )

        parser.ParseCommandLine args

