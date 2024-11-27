# SlowDown Middleware

![Build status](https://github.com/nearform/aspnetcore-slow-down/actions/workflows/dotnet.yml/badge.svg) ![License](https://img.shields.io/github/license/nearform/aspnetcore-slow-down)

A slow-down middleware for ASP.NET Core.

## Installation

**This package is not yet available on NuGet. When it is, you will be able to install the package with the following:**

```bash
dotnet add package Nearform.AspNetCore.SlowDown
```

## Usage

### Adding the middleware

```csharp
using Nearform.AspNetCore.SlowDown;

var builder = WebApplication.CreateBuilder(args);

// Add HybridCache.
#pragma warning disable EXTEXP0018
builder.Services.AddHybridCache();
#pragma warning restore EXTEXP0018

/* Add your other services */

// Add SlowDown middleware to the DI container.
builder.Services.AddSlowDown();

var app = builder.Build();

/* Add your other configuration */

// Enable the middleware.
app.UseSlowDown();

app.Run();
```

The response will have some additional headers:

| Header                  | Description                                                                                      |
| ----------------------- |--------------------------------------------------------------------------------------------------|
| `x-slow-down-limit`     | How many requests in total the client can make until the server starts to slow down the response |
| `x-slow-down-remaining` | How many requests remain to the client in the `TimeWindow`                                       |
| `x-slow-down-delay`     | How much delay (in milliseconds) has been applied to this request                                |

## Configuration

| Name                     | Type     | Default Value   | Description                                                                                                                                                                                                                                    |
|--------------------------|----------|-----------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `Delay`                  | number   | `1000`          | Base unit of time delay applied to requests. It is expressed in milliseconds. Set to `0` to disable delaying.                                                                                                                                  |
| `DelayAfter`             | number   | `5`             | Number of requests received during `TimeWindow` before starting to delay responses. Set to `0` to disable delaying.                                                                                                                            |
| `MaxDelay`               | number   | `int.MaxValue`  | The maximum value of delay that a request has after many consecutive attempts. It is an important option for the server when it is running behind a load balancer or reverse proxy, and has a request timeout. Set to `0` to disable delaying. |
| `TimeWindow`             | number   | `30000`         | The duration of the time window during which request counts are kept in memory. It is expressed in milliseconds. Set to `0` to disable delaying.                                                                                               |
| `AddHeaders`             | boolean  | `true`          | Flag to add custom headers `x-slow-down-limit`, `x-slow-down-remaining`, `x-slow-down-delay` for all server responses.                                                                                                                         |
| `KeyGenerator`           | delegate | (req) => req.ip | Function used to generate keys to uniquely identify requests coming from the same user                                                                                                                                                         |
| `OnLimitReached`         | delegate | `null`          | Function that gets called the first time the limit is reached within `TimeWindow`.                                                                                                                                                             |
| `SkipFailedRequests`     | boolean  | `false`         | When `true`, failed requests (status >= 400) won't be counted.                                                                                                                                                                                 |
| `SkipSuccessfulRequests` | boolean  | `false`         | When `true`, successful requests (status < 400) won't be counted.                                                                                                                                                                              |
| `Skip`                   | delegate | `null`          | Function used to skip requests. Returning `true` from the function will skip limiting for that request.                                                                                                                                        |

## Configuration examples

### Configuring in code

You can configure the middleware as part of the `AddSlowDown()` call. As delegates cannot be configured in JSON, this is how you'll customize any of the delegates.

```csharp
// Add SlowDown middleware to the DI container.
builder.Services.AddSlowDown(options =>
{
    options.OnLimitReached = request =>
    {
        // When we're limited, add a silly header into the response.
        request.HttpContext.Response.Headers["X-SlowDown-OnLimitReached"] = "Hi!";
    };
    
    // Change the `DelayAfter` value to 6 requests
    options.DelayAfter = 6;
});
```

### Configuring in `appsettings.json`

Boolean and numeric values can be configured in your `appsettings.json` file, nested underneath a `SlowDown` section. For example:

```json
{
  "SlowDown": {
    "Delay": 5000,
    "DelayAfter": 50,
    "MaxDelay": 60000,
    "TimeWindow": 30000
  }
}
```

## Example

A delay specified via the `Delay` option will be applied to requests coming from the same IP address (by default) when more than `DelayAfter` requests are received within the time specified in the `TimeWindow` option.

Consider the following configuration:

+ `Delay`: `10000` (10s)
+ `DelayAfter`: `10`
+ `MaxDelay`: `100000` (100s)

The following is an example of hitting an API for 10 minutes the result of hitting the API will look like:

- 1st request - no delay
- 2nd request - no delay
- 3rd request - no delay
- `...`
- 10th request - no delay
- 11th request - 10 seconds delay
- 12th request - 20 seconds delay
- 13th request - 30 seconds delay
- `...`
- 20th request - 100 seconds delay
- 21st request - 100 seconds delay\*

After 10 minutes without hitting the API the results will be:

- 21st request - no delay
- 22nd request - no delay
- `...`
- 30th request - no delay
- 31st request - 10 seconds delay

Delay remains the same because the value of `MaxDelay` option is `100000`.

## License

Nearform.AspNetCore.SlowDown is released under the MIT License.
