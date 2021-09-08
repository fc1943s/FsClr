namespace FsClr

open Serilog


module Logger =
    let inline logWarning fn getLocals =
        if true then Log.Warning $"{fn ()} {getLocals ()}"
    let inline logDebug fn getLocals =
        if true then Log.Debug $"{fn ()} {getLocals ()}"

    let inline logTrace fn getLocals =
        if true then Log.Verbose $"{fn ()} {getLocals ()}"
