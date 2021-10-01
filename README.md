# OpenFaaS ASPNET Functions

This project offers templates for OpenFaaS that make use of ASP.NET 5. The templates allow more control over the request (by providing an `HttpRequest` instance) and a better handling of the response by returning an `IActionResult`. Both C# and F# languages are supported.

## Installing the templates

Just pull the templates with the faas CLI.

```bash
faas-cli template pull https://github.com/goncalo-oliveira/faas-aspnet-template
```

If you are upgrading, use the flag `--overwrite` to write over the existing templates.

## Using the template

After installing, create a new function with the `aspnet` template. This will generate a C# template. If you want to create an F# template instead, use the `aspnet-fsharp` template.

```bash
faas-cli new --lang aspnet <function-name>
```

A file named `Function.cs` is generated when you create a new function with this template. In this file is a class named `Function` that implements `HttpFunction`. This is what it looks like:

``` csharp
namespace OpenFaaS
{
    public class Function : HttpFunction
    {
        [HttpGet]
        [HttpPost]
        public override Task<IActionResult> HandleAsync( HttpRequest request )
        {
            var result = new
            {
                Message = "Hello!"
            };

            return Task.FromResult( Ok( result ) );
        }
    }
}
```

This is just an example. You can now start implementing your function.

If you want to restrict function execution to a particular HTTP method (or methods) you can decorate `HandleAsync` with HTTP method attributes. Unhandled metods will return a 405 response.

```csharp
public class Function : HttpFunction
{
    [HttpPost]
    public override Task<IActionResult> HandleAsync( HttpRequest request )
    {
        // this will only execute with a POST method
    }
}
```

## Dependency Injection

The template supports the dependency injection design pattern. To allow you to extend this, you can configure additional services in the `Startup.cs` file with the `ConfigureServices` method. This is how the generated file looks like.

```csharp
namespace OpenFaaS
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices( IServiceCollection services )
        {
            services.AddHttpFunction<Function>();

            // add your services here.
        }
    }
}
```

As an example, let's consider we want to make `IHttpClientFactory` accessible on our function, because we want to instantiate `HttpClient` safely inside our function. We configure the container as we would on an ASP.NET or .NET Core application.

```csharp
public void ConfigureServices( IServiceCollection services )
{
    services.AddHttpFunction<Function>();

    // add your services here.
    services.AddHttpClient();
}
```

The above requires adding the package `Microsoft.Extensions.Http` to the project.

Then we simply inject `IHttpClientFactory` on our function and let the runtime do the rest.

```csharp
public class Function : HttpFunction
{
    private readonly IHttpClientFactory clientFactory;

    public Function( IHttpClientFactory httpClientFactory )
    {
        clientFactory = httpClientFactory;
    }

    [HttpGet]
    [HttpPost]
    public override async Task<IActionResult> HandleAsync( HttpRequest request )
    {
        var httpClient = clientFactory.CreateClient();

        ...
    }
}
```

## Configuration

The template also exposes ASPNET's configuration model. The `Startup.cs` file contains an `IConfiguration` instance that is populated with environment variables and OpenFaaS secrets.

### OpenFaaS Secrets

Secrets that the function has access to are also loaded into the configuration model. They are prepended with the prefix `_secret_`. For example, a secret named `my-secret-key` can be accessed with the configuration key `_secret_my-secret-key`.

NOTE: The value of the secret is read as a byte array and then stored as a base64 string.

You can also use the extension methods `GetSecret` and `GetSecretAsString`.

```csharp
public void ConfigureServices( IServiceCollection services )
{
    services.AddHttpFunction<Function>();

    // add your services here.
    services.AddMyService( options =>
    {
        options.ApiKey = Configuration.GetSecretAsString( "my-api-key" );
    } );
}
```

## Route templates

Route templates are supported through HTTP method attributes. When used, the route template values are injected on the `RouteData`.

```csharp
public class Function : HttpFunction
{
    [HttpGet( "{id}" )]
    public override Task<IActionResult> HandleAsync( HttpRequest request )
    {
        var id = request.HttpContext.GetRouteValue( "id" );
    }
}
```

## Manual route handling

By default, only root path or route templates are accepted by the handler. Everything else throws back a 404 response. If we want to bypass this and handle the routes on the function, we can set `IgnoreRoutingRules=true` in the options, when configuring our function in the `Startup.cs` file.

```csharp
public void ConfigureServices( IServiceCollection services )
{
    services.AddHttpFunction<Function>( options =>
    {
        options.IgnoreRoutingRules = true;
    } );

    // add your services here.
}
```

## Authentication and Authorization

It is possible to use ASPNET's authentication pipeline. Furthermore, the `Authorize` attribute is allowed in the function class and will return a 401 unless the user is authenticated.

```csharp
namespace OpenFaaS
{
    [Authorize]
    public class Function : HttpFunction
    {
        ...
    }
}
```

## Controller-based template

Sometimes we want to create a more complex workload, with multiple methods or even with multiple routes. For these scenarios, we can use the `aspnet-controller` template. When we create a new function with this template, instead of having a `Function.cs` we now have a `Controller.cs` file.

```csharp
namespace OpenFaaS
{
    [ApiController]
    [Route("/")]
    public class Controller : ControllerBase
    {
        [HttpGet]
        public Task<IActionResult> GetAsync()
        {
            var result = new
            {
                Message = "Hello!"
            };

            return Task.FromResult<IActionResult>( Ok( result ) );
        }
    }
}
```

This controller class is exactly the same as we would have when creating a Web API project with ASPNET. We also still have access to a `Startup.cs` file. Additionally, we have access to the HTTP request pipeline configuration.

```csharp
namespace OpenFaaS
{
    public class Startup
    {
        public Startup( IConfiguration configuration )
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices( IServiceCollection services )
        {
            // add your services here.
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure( IApplicationBuilder app, bool isDevelopmentEnv )
        {
        }
    }
}
```

## Polymorphic serialization

Since template version 1.5, which uses [FaaS Runner](https://github.com/goncalo-oliveira/faas-run) 1.7, the default JSON serializer from Microsoft is used in place of Newtonsoft's. This means that there is limited support for polymorphic serialization and deserialization is not supported at all. You can find more in [this article](https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-polymorphism) where you will also find a few workarounds, including writing a custom converter.

If you rather use [Newtonsoft's Json.NET](https://www.newtonsoft.com/json), you still can. Find out more [here](https://github.com/goncalo-oliveira/faas-aspnet-newtonsoft/).

## Customizing Json serializer

If you need to customize the serialization options, you can easily do so on the `Startup.cs` class. If using Microsoft's JSON serializer (default from template version 1.5), you can do the following

```csharp
public void ConfigureServices( IServiceCollection services )
{
    services.Configure<JsonOptions>( options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    } );

    // add your services here.
}
```

If you are using [Newtonsoft's Json.NET](https://www.newtonsoft.com/json) instead, read [here](https://github.com/goncalo-oliveira/faas-aspnet-newtonsoft/).

## Debugging and running locally

It is possible to run a function locally with [FaaS Runner](https://github.com/goncalo-oliveira/faas-run). This also adds the option to attach to the process when running, to be able to debug the function. A configuration file can be passed to the runner. The CLI takes the assembly path as argument.

```
~/source/hello$ faas-run bin/Debug/netstandard2.0/function.dll
```

> To run an `aspnet-controller` function, version **1.6+** is required.

### VS Code

When using VS Code, a configuration can be easily created in `launch.json` file that uses `faas-run`. This skips the need to manually attach to a process.

```json
{
    "name": ".NET Core Launch (faas run)",
    "type": "coreclr",
    "request": "launch",
    "preLaunchTask": "build",
    "program": "faas-run",
    "args": ["bin/Debug/net5.0/function.dll", "--no-auth"],
    "cwd": "${workspaceFolder}",
    "stopAtEntry": false,
    "console": "internalConsole"
},
```
