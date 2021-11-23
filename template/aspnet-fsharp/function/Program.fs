module Program

[<EntryPoint>]
let main args =
    OpenFaaS.Hosting.Runner.Run( args )
    0
