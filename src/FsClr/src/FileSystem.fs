namespace FsClr

open System
open System.IO
open System.Reflection
open FSharp.Control
open FsCore


module FileSystem =
    [<RequireQualifiedAccess>]
    type FileSystemChangeType =
        | Error
        | Changed
        | Created
        | Deleted
        | Renamed

    type FileSystemChange =
        | Error of exn: exn
        | Changed of path: string
        | Created of path: string
        | Deleted of path: string
        | Renamed of oldPath: string * path: string

    type FileSystemChange with
        static member inline Path event =
            match event with
            | Error _ -> None, None
            | Changed path -> None, Some path
            | Created path -> None, Some path
            | Deleted path -> None, Some path
            | Renamed (oldPath, path) -> Some oldPath, Some path

        static member inline Type event =
            match event with
            | Error _ -> FileSystemChangeType.Error
            | Changed _ -> FileSystemChangeType.Changed
            | Created _ -> FileSystemChangeType.Created
            | Deleted _ -> FileSystemChangeType.Deleted
            | Renamed _ -> FileSystemChangeType.Renamed

    let watchWithFilter path filter =
        let fullPath = Path.GetFullPath path
        let getLocals () = $"fullPath={fullPath} {getLocals ()}"

        let watcher =
            new FileSystemWatcher (
                Path = fullPath,
                NotifyFilter = filter,
                EnableRaisingEvents = true,
                IncludeSubdirectories = true
            )

        let getEventPath (path: string) = path.Trim().Replace (fullPath, "")

        let changedStream =
            AsyncSeq.subscribeEvent
                watcher.Changed
                (fun event -> FileSystemChange.Changed (getEventPath event.FullPath))

        let deletedStream =
            AsyncSeq.subscribeEvent
                watcher.Deleted
                (fun event -> FileSystemChange.Deleted (getEventPath event.FullPath))

        let createdStream =
            AsyncSeq.subscribeEvent
                watcher.Created
                (fun event -> FileSystemChange.Created (getEventPath event.FullPath))

        let renamedStream =
            AsyncSeq.subscribeEvent
                watcher.Renamed
                (fun event -> FileSystemChange.Renamed (getEventPath event.OldFullPath, getEventPath event.FullPath))

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

    let ensureTempSessionDirectory () =
        let tempFolder =
            Path.GetTempPath ()
            </> Assembly.GetEntryAssembly().GetName().Name
            </> string (Guid.newTicksGuid ())

        let result = Directory.CreateDirectory tempFolder

        let getLocals () =
            $"tempFolder={tempFolder} result.Exists={result.Exists} {getLocals ()}"

        Logger.logDebug (fun () -> "FileSystem.ensureTempSessionDirectory") getLocals

        tempFolder

    let watch path =
        watchWithFilter
            path
            (NotifyFilters.Attributes
             ||| NotifyFilters.Security
             ||| NotifyFilters.CreationTime
             ||| NotifyFilters.DirectoryName
             ||| NotifyFilters.FileName)
