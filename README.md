# OpenFaaS ASPNET Template

This template for OpenFaaS makes use of ASP.NET 5. This allows more control over the request (by providing an `HttpRequest` instance) and better handling of the response by returning an `IActionResult`.

## Installing the template

Just pull the template with the faas CLI.

```bash
faas-cli template pull https://github.com/redpandaltd/faas-aspnet-template
```

## Using the template

After installing the template, create a new function with the `aspnet` template.

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

If you want to restrict function execution to a particular HTTP method (or methods) you can decorate `HandleAsync` with HTTP method attributes.

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

You can also implement only a specific HTTP method (or multiple, each with its own logic) if you want. Instead of overriding the public method `HandleAsync`, override the protected methods `HandleGetAsync` or `HandlePostAsync` for example.

``` csharp
namespace OpenFaaS
{
    public class Function : HttpFunction
    {
        // not overriding HandleAsync so that I can use the other handlers

        protected override Task<IActionResult> HandlePostAsync( HttpRequest request )
        {
            var result = new
            {
                Message = "Hello POST!"
            };

            return Task.FromResult( Ok( result ) );
        }
    }
}
```

When we use this, all methods that are not handled return a 405.

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

Secrets that the function has access to are also loaded into the configuration model. They are prepended with the prefix `openfaas_secret_`. For example, a secret named `my-secret-key` can be accessed with the configuration key `openfaas_secret_my-secret-key`.

NOTE: The value of the secret is read as a byte array and then stored as a base64 string.

You can also use the extension methods `GetSecret` and `GetSecretAsString` by installing [Redpanda.Extensions.OpenFaaS.Configuration](https://www.nuget.org/packages/Redpanda.Extensions.OpenFaaS.Configuration/). You can read more [here](https://github.com/redpandaltd/faas-configuration-extensions/blob/master/README.md).

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

## Debugging

It is possible to run a function locally with `faas-run` CLI. This also adds the option to attach to the process when running, to be able to debug the function. A configuration file can be passed to the runner. The CLI takes the debug assembly as argument.

```
~/source/hello$ faas-run bin/Debug/netstandard2.0/function.dll
```

### VS Code

When using VS Code, a configuration can be easily created in `launch.json` file that uses `faas-run`. This skips the need to manually attach to a process.

```json
{
    "name": ".NET Core Launch (faas run)",
    "type": "coreclr",
    "request": "launch",
    "preLaunchTask": "build",
    "program": "faas-run",
    "args": ["bin/Debug/netstandard2.0/function.dll", "--no-auth"],
    "cwd": "${workspaceFolder}",
    "stopAtEntry": false,
    "console": "internalConsole"
},
```
