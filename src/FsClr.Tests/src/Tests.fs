namespace FsClr

open System.Collections.Concurrent
open System.IO
open Expecto
open Expecto.Flip
open FSharp.Control
open FsClr.FileSystem


module Tests =
    let sortEvent event =
        match event with
        | Error _ -> 0
        | Created _ -> 1
        | Changed _ -> 2
        | Renamed (_oldPath, _) -> 3
        | Deleted _ -> 4

    let formatEvents events =
        events
        |> Seq.toList
        |> List.sortBy (snd >> sortEvent)
        |> List.choose
            (fun (ticks, event) ->
                match event with
                | Error _ -> None
                | Changed path -> Some (ticks, Path.GetFileName path, nameof Changed)
                | Created path -> Some (ticks, Path.GetFileName path, nameof Created)
                | Deleted path -> Some (ticks, Path.GetFileName path, nameof Deleted)
                | Renamed (_oldPath, path) -> Some (ticks, Path.GetFileName path, nameof Renamed))
        |> List.sortBy (fun (_, path, _) -> path)
        |> List.distinctBy (fun (_, path, event) -> path, event)

    let config =
        { FsCheckConfig.defaultConfig with
            maxTest = 10000
        }

    [<Tests>]
    let tests =
        testList
            "FileSystem"
            [
                testList
                    "watch"
                    [
                        testProperty "test" (fun a b -> a + b = b + a)

                        let testEvents write =
                            let path = ensureTempSessionDirectory ()
                            let stream, disposable = watch path

                            let events = ConcurrentBag ()

                            let watch () =
                                stream
                                |> AsyncSeq.iterAsync (fun event -> async { events.Add event })

                            let run () =
                                async {
                                    let! child = watch () |> Async.StartChild
                                    do! Async.Sleep 70
                                    do! write path |> Async.AwaitTask
                                    do! child
                                }

                            run () |> Async.runWithTimeout 1000

                            disposable.Dispose ()
                            Directory.Delete (path, true)

                            let events = formatEvents events

                            let eventMap =
                                events
                                |> List.map (fun (ticks, path, event) -> path, (event, ticks))
                                |> List.groupBy fst
                                |> List.map
                                    (fun (path, events) ->
                                        let event, _ticks =
                                            events
                                            |> List.map snd
                                            |> List.sortByDescending snd
                                            |> List.head

                                        path, event)
                                |> Map.ofList

                            let eventList =
                                events
                                |> List.map (fun (_ticks, path, event) -> path, event)

                            eventMap, eventList

                        test "create and delete" {
                            let write path =
                                task {
                                    let n = 3

                                    for i = 1 to n do
                                        let filePath = path </> $"file{i}.txt"
                                        do! File.WriteAllTextAsync (filePath, $"{i}")

                                    for i = 1 to n do
                                        let filePath = path </> $"file{i}.txt"
                                        File.Delete filePath
                                }

                            let eventMap, eventList = testEvents write

                            eventList
                            |> Expect.sequenceEqual
                                ""
                                [
                                    "file1.txt", nameof Created
                                    "file1.txt", nameof Deleted

                                    "file2.txt", nameof Created
                                    "file2.txt", nameof Deleted

                                    "file3.txt", nameof Created
                                    "file3.txt", nameof Deleted
                                ]

                            eventMap
                            |> Expect.sequenceEqual
                                ""
                                ([
                                    "file1.txt", nameof Deleted
                                    "file2.txt", nameof Deleted
                                    "file3.txt", nameof Deleted
                                 ]
                                 |> Map.ofList)
                        }

                        test "change" {
                            let write path =
                                task {
                                    let n = 2

                                    for i = 1 to n do
                                        let filePath = path </> $"file{i}.txt"
                                        do! File.WriteAllTextAsync (filePath, $"{i}")

                                    for i = 1 to n do
                                        let filePath = path </> $"file{i}.txt"
                                        do! File.WriteAllTextAsync (filePath, "")

                                    for i = 1 to n do
                                        let filePath = path </> $"file{i}.txt"
                                        File.Delete filePath
                                }

                            let eventMap, eventList = testEvents write

                            eventList
                            |> Expect.sequenceEqual
                                ""
                                [
                                    "file1.txt", nameof Created
                                    "file1.txt", nameof Changed
                                    "file1.txt", nameof Deleted

                                    "file2.txt", nameof Created
                                    "file2.txt", nameof Changed
                                    "file2.txt", nameof Deleted
                                ]

                            eventMap
                            |> Expect.sequenceEqual
                                ""
                                ([
                                    "file1.txt", nameof Deleted
                                    "file2.txt", nameof Deleted
                                 ]
                                 |> Map.ofList)
                        }

                        test "rename" {
                            let write path =
                                task {
                                    let n = 2

                                    for i = 1 to n do
                                        let filePath = path </> $"file{i}.txt"
                                        do! File.WriteAllTextAsync (filePath, $"{i}")

                                    for i = 1 to n do
                                        let filePath = path </> $"file{i}.txt"
                                        let filePath2 = path </> $"file_{i}.txt"
                                        File.Move (filePath, filePath2)

                                    for i = 1 to n do
                                        let filePath2 = path </> $"file_{i}.txt"
                                        File.Delete filePath2
                                }

                            let eventMap, eventList = testEvents write

                            eventList
                            |> Expect.sequenceEqual
                                ""
                                [
                                    "file1.txt", nameof Created
                                    "file2.txt", nameof Created

                                    "file_1.txt", nameof Renamed
                                    "file_1.txt", nameof Deleted

                                    "file_2.txt", nameof Renamed
                                    "file_2.txt", nameof Deleted
                                ]

                            eventMap
                            |> Expect.sequenceEqual
                                ""
                                ([
                                    "file1.txt", nameof Created
                                    "file2.txt", nameof Created
                                    "file_1.txt", nameof Deleted
                                    "file_2.txt", nameof Deleted
                                 ]
                                 |> Map.ofList)
                        }
                    ]
            ]
