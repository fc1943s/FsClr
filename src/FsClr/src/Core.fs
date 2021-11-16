namespace FsClr


open System
open System.IO
open System.Threading.Tasks
open FSharp.Control
open System.Reactive.Linq
open FsCore


[<AutoOpen>]
module Operators =
    let inline (</>) a b = Path.Combine (a, b)

module Async =
    let startAsTask ct fn : Task =
        upcast Async.StartAsTask (fn, cancellationToken = ct)

    let map fn op =
        async {
            let! x = op
            let value = fn x
            return value
        }

    let runWithTimeout timeout fn =
        try
            Async.RunSynchronously (fn, timeout)
        with
        | :? TimeoutException -> printfn $"Async timeout reached ({timeout})"
        | e -> raise e

    let initAsyncSeq op = AsyncSeq.initAsync 1L (fun _ -> op)


module AsyncSeq =
    let subscribeEvent (event: IEvent<'H, 'A>) map =
        Observable
            .FromEventPattern<'H, 'A>(event.AddHandler, event.RemoveHandler)
            .Select (fun event -> DateTime.Now.Ticks, (map event.EventArgs))
        |> AsyncSeq.ofObservableBuffered


module Task =
    let ignore (t: Task<unit []>) =
        task {
            let! _tasks = t
            ()
        }
