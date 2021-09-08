namespace FsClr

open System.IO
open Expecto
open Expecto.Flip
open FSharp.Control
open FSharp.Control.Tasks.V2


module Tests =
    [<Tests>]
    let tests =
        testList
            "FileSystem"
            [
                testList
                    "watch"
                    [
                        test "test1" {
                            0 |> Expect.equal "" 0

//                            let path = FileSystem.ensureTempSessionDirectory ()
//
//                            let stream, disposable = FileSystem.watch path
//
//                            let watch () =
//                                stream
//                                |> AsyncSeq.iterAsync
//                                    (fun event ->
//                                        async {
//                                            let! content = File.ReadAllTextAsync path |> Async.AwaitTask
//                                            printfn $"event={event} content={content}"
//                                        })
//
//                            let write () =
//                                task {
//                                    for i = 0 to 100 do
//                                        do! File.WriteAllTextAsync (path, $"{i}")
//                                }
//
//                            let run () =
//                                async {
//                                    let! child = watch () |> Async.StartChild
//                                    do! write () |> Async.AwaitTask
//                                    do! child
//                                }
//
//                            run () |> Async.runWithTimeout 1000
//
//                            disposable.Dispose ()
                        }
                    ]
            ]
