namespace OpenFaaS

open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc
open OpenFaaS
open System
open System.Threading.Tasks

type Function() =
    inherit HttpFunction()

    [<HttpGet>]
    [<HttpPost>]
    override this.HandleAsync( request : HttpRequest ) =
        let result = {| Message = "Hello" |}
        Task.FromResult( this.Ok( result ) :> IActionResult )
