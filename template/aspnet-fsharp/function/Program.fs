module Program

[<EntryPoint>]
let main args =
    OpenFaaS.Hosting.Runner.Run( args, typeof<OpenFaaS.Startup> )

    0
