namespace FsClr

open Expecto
open Expecto.Flip


module Tests =
    [<Tests>]
    let tests =
        testList
            "FileSystem"
            [
                testList
                    "watch"
                    [
                        test "???" {
                            0 |> Expect.equal "" 0
                        }
                    ]
            ]
