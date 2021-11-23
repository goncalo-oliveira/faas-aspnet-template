# OpenFaaS ASPNET Functions

This project offers templates for OpenFaaS that make use of ASP.NET. The templates allow more control over the request and a better handling of the response by using the ASP.NET framework. Both C# and F# template are available.

## Installing the templates

Just pull the templates with the faas CLI.

```bash
faas-cli template pull https://github.com/goncalo-oliveira/faas-aspnet-template
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

A file named `Function.cs` is generated when you create a new function with this template. In this file is a class named `Function` that implements `ControllerBase`. This is what it looks like:

``` csharp
namespace OpenFaaS
{
    [ApiController]
    [Route("/")]
    public class Function : ControllerBase
    {
        [HttpGet]
        [HttpPost]
        public Task<IActionResult> ExecuteAsync()
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

As you can see, the function is just an MVC controller; this serves the purposes of either single action function and [micro-apis](https://itnext.io/micro-apis-with-openfaas-and-net-f82115efce4).

## OpenFaaS Secrets

Secrets that the function has access to are loaded into the configuration model. They are prepended with the prefix `_secret_`. For example, a secret named `my-secret-key` can be accessed with the configuration key `_secret_my-secret-key`.

NOTE: The value of the secret is read as a byte array and then stored as a base64 string.

You can also use the extension methods `GetSecret` and `GetSecretAsString`.

```csharp
public void ConfigureServices( IServiceCollection services )
{
    // add your services here.
    services.AddMyService( options =>
    {
        options.ApiKey = Configuration.GetSecretAsString( "my-api-key" );
    } );
}
```

## Polymorphic serialization

The JSON serializer from Microsoft is used by default. This means that there is limited support for polymorphic serialization and deserialization is not supported at all. You can find more in [this article](https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-polymorphism) where you will also find a few workarounds, including writing a custom converter.

If you rather use [Newtonsoft's Json.NET](https://www.newtonsoft.com/json), you still can. Add the package `OpenFaaS.Runner.NewtonsoftJson` and use the function builder to extend the functionality

```csharp
public void ConfigureServices( IServiceCollection services )
{
    var functionBuilder = services.ConfigureFunction();

    functionBuilder.AddNewtonsoftJson();
}

```

## Function Builder

If you need to customize the function's behaviour, such as Json serialization options for example, you can use the function builder to extend functionality

```csharp
public void ConfigureServices( IServiceCollection services )
{
    var functionBuilder = services.ConfigureFunction();

    functionBuilder.ConfigureJsonOptions( options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    } );
}
```

## Migrating from v1.x

Version 2.x brings better performance and less friction with dependencies by dropping the usage of `faas-run`. Unless there's a particular reason for not doing so, it is recommended to upgrade your functions to the newer templates.

The templates from v1.x included two C# projects; one that inherited `IHttpFunction`, designed to execute a single action and another one that inherited `ControllerBase`, designed to support [micro-apis](https://itnext.io/micro-apis-with-openfaas-and-net-f82115efce4). In total, there were three templates

- aspnet (IHttpFunction)
- aspnet-controller (ControllerBase)
- aspnet-fsharp (IHttpFunction)

Version 2.x dropped `IHttpFunction` inheritance and all functions inherit `ControllerBase`, either they are *pure* functions or *micro-apis*. This applies to both C# and F# templates; so now, there are only two templates (aspnet and aspnet-fsharp). Here's what the C# function looks like

```csharp
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace OpenFaaS
{
    [ApiController]
    [Route("/")]
    public class Function : ControllerBase
    {
        [HttpGet]
        [HttpPost]
        public Task<IActionResult> ExecuteAsync()
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

The project file also suffered some changes; it is now an executable and not a library, and replaced `OpenFaaS.Functions` with `OpenFaaS.Runner`.

The easiest way is to create a separate dummy project to serve as a reference as you make the changes. Nonetheless, here's what the function project file looks like

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net5.0</TargetFramework>
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

The new function, now an executable, also needs a `Program.cs`, which looks like this

```csharp
using System;

namespace OpenFaaS
{
    class Program
    {
        static void Main( string[] args ) => Hosting.Runner.Run( args );
    }
}
```
