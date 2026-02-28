var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis");

builder.AddProject<Projects.MX_IDP_Web>("idp-web")
    .WithReference(redis);

builder.Build().Run();
