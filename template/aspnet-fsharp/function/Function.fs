namespace OpenFaaS

open System
open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc

[<ApiController>]
[<Route("/")>]
type Function() =
    inherit ControllerBase()

    [<HttpGet>]
    [<HttpPost>]
    member this.ExecuteAsync() =
        let result = {| Message = "Hello" |}
        Task.FromResult( this.Ok( result ) :> IActionResult )
