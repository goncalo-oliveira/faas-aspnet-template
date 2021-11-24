# OpenFaaS ASPNET Functions

This project offers templates for OpenFaaS that make use of ASP.NET. The templates allow more control over the request and a better handling of the response by using the ASP.NET framework. Both C# and F# template are available.

## Installing the templates

Just pull the templates with the faas CLI.

> Since v2.0 is still in preview, you'll need to reference the pre-release version.

```bash
faas-cli template pull https://github.com/goncalo-oliveira/faas-aspnet-template#v2.0-preview-4
```

If you are upgrading, use the flag `--overwrite` to write over the existing templates.

If you want to install a previous version, you can indicate still do it by referencing the release.

```bash
faas-cli template pull https://github.com/goncalo-oliveira/faas-aspnet-template#v1.5.2
```

## Using the template

After installing, create a new function with the `aspnet` template. This will generate a C# template. If you want to create an F# template instead, use the `aspnet-fsharp` template.

```bash
faas-cli new --lang aspnet <function-name>
```

A single `Program.cs` file is generated when you create a new function with this template. This is because by default, a minimal API structure is used. Let's have a look at the contents:

``` csharp
Runner.Run( args, builder =>
{
    // add your services to the container
}, app =>
{
    // configure the HTTP request pipeline

    app.MapPost( "/", () =>
    {
        return new
        {
            Message = "Hello"
        };
    } );
} );
```

If what you are building is a [micro-api](https://itnext.io/micro-apis-with-openfaas-and-net-f82115efce4) or if you rather use a `Startup.cs` and the Generic Host model, you can update `Program.cs` with the following code

```csharp
Runner.Run( args, typeof( OpenFaaS.Startup ) );
```

Then, you can either add a minimal `Startup.cs` file, which looks like this

```csharp
namespace OpenFaaS
{
    public class Startup
    {
        public void Configure( WebApplicationBuilder builder )
        {
            // configure the application builder
        }

        public void Configure( WebApplication app )
        {
            // configure the HTTP request pipeline
        }
    }
}
```

Or you can add a standard `Startup.cs` file

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

        public void ConfigureServices( IServiceCollection services )
        {
            // add your services to the container
        }

        public void Configure( IApplicationBuilder app, bool isDevelopmentEnv )
        {
            // configure the HTTP request pipeline
        }
    }
}
```

Since you're not using a minimal API anymore, you'll also need to add at least one controller class.

## OpenFaaS Secrets

Secrets that the function has access to are loaded into the configuration model. They are prepended with the prefix `_secret_`. For example, a secret named `my-secret-key` can be accessed with the configuration key `_secret_my-secret-key`.

NOTE: The value of the secret is read as a byte array and then stored as a base64 string.

You can also use the extension methods `GetSecret` and `GetSecretAsString`.

```csharp
IServiceCollection services = ...

services.AddMyService( options =>
{
    options.ApiKey = Configuration.GetSecretAsString( "my-api-key" );
} );
```

## Polymorphic serialization

The JSON serializer from Microsoft is used by default. This means that there is limited support for polymorphic serialization and deserialization is not supported at all. You can find more in [this article](https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-polymorphism) where you will also find a few workarounds, including writing a custom converter.

If you rather use [Newtonsoft's Json.NET](https://www.newtonsoft.com/json), you still can. Add the package `OpenFaaS.Runner.NewtonsoftJson` and use the function builder to extend the functionality

```csharp
IServiceCollection services = ...

services.ConfigureFunction()
    .AddNewtonsoftJson();
```

## Function Builder

If you need to customize the function's behaviour, such as Json serialization options or routing options for example, you can use the function builder to extend functionality

```csharp
IServiceCollection services = ...

services.ConfigureFunction() // returns an IFunctionBuilder
    .ConfigureJsonOptions( options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    } )
    .ConfigureRouteOptions( options =>
    {
        options.LowercaseUrls = true;
    } );
```

## Private Repositories

If your function has packages from private repositories, you'll need to provide a nuget configuration file to the image build process. On previous versions this could be done with build arguments, but that is considered insecure. Since version 2.x this can only be done with secrets, using BuildKit.

> The OpenFaaS CLI doesn't seem yet to support this, therefore, this can only be done with the Docker CLI.

First, you'll need to make sure you are using BuildKit. This can be done with an environment variable.

```bash
export DOCKER_BUILDKIT=1
```

You'll need a `NuGet.Config` file. Let's consider we have one at `/home/user/.nuget/NuGet/NuGet.Config`. We just need to pass the file as a secret with the docker build; the name of the secret has to be `nuget.config`.

```bash
docker build -t function --secret id=nuget.config,src=/home/goncalo/.nuget/NuGet/NuGet.Config .
```

> Currently, the passwords on the configuration file need to be stored in clear text. If you are on Windows, this won't be the case for the `NuGet.Config` on your computer.

## Migrating from v1.x

Version 2.x brings better performance and less friction with dependencies by dropping the usage of `faas-run`. Unless there's a particular reason for not doing so, it is recommended to upgrade your functions to the newer templates.

The templates from v1.x included two C# projects; one that derives from `IHttpFunction`, designed to execute a single action (*pure* functions) and another one that derives from `ControllerBase`, designed to support [micro-apis](https://itnext.io/micro-apis-with-openfaas-and-net-f82115efce4). In total, there were three templates

- aspnet (IHttpFunction)
- aspnet-controller (ControllerBase)
- aspnet-fsharp (IHttpFunction)

The `IHttpFunction` interface disappeared in version 2.x. Functions are either directly mapped using a minimal API (great for *pure* functions) or they implement one or more controllers, derived from `ControllerBase` (better choice for *micro-apis*). This applies to both C# and F# templates; so now, there are only two templates (aspnet and aspnet-fsharp).

If your current function derives from `IHttpFunction`, the recommended course of action is to migrate to a minimal API function (which is what the template generates by default).

The project file also suffered some changes; it is now an executable and not a library, and replaced `OpenFaaS.Functions` with `OpenFaaS.Runner`.

The easiest way is to create a separate *hello* project to serve as a reference as you make the changes. Nonetheless, here's what the function project file looks like

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net6.0</TargetFramework>
    <RootNamespace>OpenFaaS</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="OpenFaaS.Runner" Version="2.0.0" />
  </ItemGroup>

</Project>
```

If you're going for a minimal API (your current function derives from `IHttpFunction`), the `Startup.cs` and the `Function.cs` files disappear. Instead, you'll have a `Program.cs` that looks like this

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OpenFaaS.Hosting;

Runner.Run( args, builder =>
{
    // your Startup.cs > ConfigureServices( IServiceCollection ) goes in here
}, app =>
{
    // your Startup.cs > Configure( IApplicationBuilder, bool ) goes in here

    // your Function.cs is implemented here
    app.MapPost( "/", () =>
    {
        return new
        {
            Message = "Hello"
        };
    } );
} );
```

If your current function is using the "old" `aspnet-controller` template, there are less changes to be made. The `Startup.cs` file can be maintained as it is. The same goes for the controller(s). You'll just need to add a `Program.cs` file similar to this

```csharp
using OpenFaaS;
using OpenFaaS.Hosting;

Runner.Run( args, typeof( Startup ) );
```
