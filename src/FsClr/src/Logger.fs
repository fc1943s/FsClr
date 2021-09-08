namespace FsClr

open FsCore
open Serilog
open Serilog.Events
open Serilog.Sinks.SpectreConsole


module Logger =
    let inline logError fn getLocals =
        if true then Log.Error $"{fn ()} {getLocals ()}"

    let inline logWarning fn getLocals =
        if true then Log.Warning $"{fn ()} {getLocals ()}"

    let inline logInfo fn getLocals =
        if true then Log.Information $"{fn ()} {getLocals ()}"

    let inline logDebug fn getLocals =
        if true then Log.Debug $"{fn ()} {getLocals ()}"

    let inline logTrace fn getLocals =
        if true then Log.Verbose $"{fn ()} {getLocals ()}"

    let inline seq x =
        let items = x |> Seq.map string |> String.concat ","
        $"[{items}]"

    let inline init () =
        Log.Logger <-
            LoggerConfiguration()
                .Enrich.FromLogContext()
                .MinimumLevel.Verbose()
                .WriteTo.Console()
                .WriteTo
                .spectreConsole(
                    "{Timestamp:HH:mm:ss} [{Level:u4}] {Message:lj}{NewLine}{Exception}",
                    minLevel = LogEventLevel.Verbose
                )
                .CreateLogger ()

        logInfo (fun () -> "Logger.init") getLocals
