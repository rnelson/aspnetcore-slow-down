var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache")
    .WithRedisInsight()
    .WithRedisCommander()
    ;

var apiService = builder
    .AddProject<Projects.DistributedCacheSlowDownExample_ApiService>("apiservice")
    .WithReference(cache);
var apiService2 = builder
    .AddProject<Projects.DistributedCacheSlowDownExample_ApiService2>("apiservice2")
    .WithReference(cache);

builder.AddProject<Projects.DistributedCacheSlowDownExample_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(cache)
    .WithReference(apiService)
    .WithReference(apiService2);

builder.Build().Run();
