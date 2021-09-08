namespace FsClr

open System
open System.IO
open System.Reflection
open FSharp.Control
open FsCore


module FileSystem =
    type FileSystemChange =
        | Error of exn: exn
        | Changed of path: string
        | Created of path: string
        | Deleted of path: string
        | Renamed of oldPath: string * path: string

    let inline watch path =
        let getLocals () = $"path={path} {getLocals ()}"

        let watcher = new FileSystemWatcher (Path = path, EnableRaisingEvents = true, IncludeSubdirectories = true)

        let changedStream =
            AsyncSeq.subscribeEvent watcher.Changed (fun event -> FileSystemChange.Changed event.FullPath)

        let deletedStream =
            AsyncSeq.subscribeEvent watcher.Deleted (fun event -> FileSystemChange.Deleted event.FullPath)

        let createdStream =
            AsyncSeq.subscribeEvent watcher.Created (fun event -> FileSystemChange.Created event.FullPath)

        let renamedStream =
            AsyncSeq.subscribeEvent
                watcher.Renamed
                (fun event -> FileSystemChange.Renamed (event.OldFullPath, event.FullPath))

        let errorStream =
            AsyncSeq.subscribeEvent watcher.Error (fun event -> FileSystemChange.Error (event.GetException ()))

        let stream =
            [
                changedStream
                deletedStream
                createdStream
                renamedStream
                errorStream
            ]
            |> AsyncSeq.mergeAll

        let disposable =
            Object.newDisposable
                (fun () ->
                    Logger.logDebug (fun () -> "Disposing watch stream") getLocals
                    watcher.EnableRaisingEvents <- false
                    watcher.Dispose ())

        stream, disposable

    let rec private waitForStream path =
        async {
            try
                return new FileStream (path, FileMode.Open, FileAccess.Write)
            with
            | _ ->
                let getLocals () = $"path={path} {getLocals ()}"
                Logger.logWarning (fun () -> "Error opening file for writing. Waiting...") getLocals
                do! Async.Sleep (TimeSpan.FromSeconds 1.)
                return! waitForStream path
        }

    let inline ensureTempSessionDirectory () =
        let tempFolder =
            Path.GetTempPath ()
            </> Assembly.GetEntryAssembly().GetName().Name
            </> string (Guid.newTicksGuid ())

        let result = Directory.CreateDirectory tempFolder

        let getLocals () =
            $"tempFolder={tempFolder} result.Exists={result.Exists} {getLocals ()}"

        Logger.logDebug (fun () -> "FileSystem.ensureTempSessionDirectory") getLocals

        tempFolder
