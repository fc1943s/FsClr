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
        let watcher = new FileSystemWatcher (Path = path, EnableRaisingEvents = true, IncludeSubdirectories = true)

        let changedStream =
            AsyncSeq.subscribeEvent watcher.Changed (fun event -> FileSystemChange.Changed event.FullPath)
        //            |> AsyncSeq.bufferByTime 100
//            |> AsyncSeq.choose Array.tryLast

        let createdStream =
            AsyncSeq.subscribeEvent watcher.Created (fun event -> FileSystemChange.Created event.FullPath)

        let deletedStream =
            AsyncSeq.subscribeEvent watcher.Deleted (fun event -> FileSystemChange.Deleted event.FullPath)

        let renamedStream =
            AsyncSeq.subscribeEvent
                watcher.Renamed
                (fun event -> FileSystemChange.Renamed (event.OldFullPath, event.FullPath))

        let errorStream =
            AsyncSeq.subscribeEvent watcher.Error (fun event -> FileSystemChange.Error (event.GetException ()))

        let stream =
            [
                changedStream
                createdStream
                deletedStream
                renamedStream
                errorStream
            ]
            |> AsyncSeq.mergeAll

        let disposable =
            Object.newDisposable
                (fun () ->
                    watcher.EnableRaisingEvents <- false
                    watcher.Dispose ())

        stream, disposable

    let rec private waitForStream path =
        async {
            try
                return new FileStream (path, FileMode.Open, FileAccess.Write)
            with
            | _ ->
                let getLocals = $"path={path} {getLocals ()}"
                Logger.logWarning (fun () -> "Error opening file for writing. Waiting...") getLocals
                do! Async.Sleep (TimeSpan.FromSeconds 1.)
                return! waitForStream path
        }
