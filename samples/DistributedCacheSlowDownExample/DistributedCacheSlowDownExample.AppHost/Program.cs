// ReSharper disable InconsistentNaming

var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache")
    .WithRedisInsight()
    .WithRedisCommander();

var api1_5556 = builder
    .AddProject<Projects.DistributedCacheSlowDownExample_ApiService>("api1-5556")
    .WithReference(cache);
var api2_5249 = builder
    .AddProject<Projects.DistributedCacheSlowDownExample_ApiService2>("api2-5249")
    .WithReference(cache);

builder.AddProject<Projects.DistributedCacheSlowDownExample_Web>("web")
    .WithExternalHttpEndpoints()
    .WithReference(cache)
    .WithReference(api1_5556)
    .WithReference(api2_5249);

builder.Build().Run();
