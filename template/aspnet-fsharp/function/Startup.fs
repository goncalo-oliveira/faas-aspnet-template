namespace OpenFaaS

open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection

type Startup private () =
    new ( configuration: IConfiguration ) as this =
        Startup() then
        this.Configuration <- configuration

    // This method gets called by the runtime. Use this method to add services to the container.
    member this.ConfigureServices( services: IServiceCollection ) =
        services.AddHttpFunction<Function>() |> ignore

    member val Configuration : IConfiguration = null with get, set
