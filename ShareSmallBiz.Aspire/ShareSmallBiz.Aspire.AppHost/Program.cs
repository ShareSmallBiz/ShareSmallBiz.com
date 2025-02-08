var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.ShareSmallBiz_Aspire_ApiService>("apiservice");

builder.AddProject<Projects.ShareSmallBiz_Aspire_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
