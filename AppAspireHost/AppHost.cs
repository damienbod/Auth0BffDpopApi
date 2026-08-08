using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.WebApi>("webapi")
    .WithExternalHttpEndpoints();

if (builder.Environment.IsDevelopment())
{
    var angularFrontend = builder.AddJavaScriptApp("angular", "../bff/ui", "start")
        .WithHttpsEndpoint(port: 3000, 4201, env: "BASE_URL");

    builder.AddProject<Projects.BffAuth0_Server>("bffauth0-server")
        .WithReference(angularFrontend)
        .WaitFor(angularFrontend)
        .WithExternalHttpEndpoints();
}
else
{
    // Hint: to make this work, the deployment pipeline must execute npm run build
    builder.AddProject<Projects.BffAuth0_Server>("bffauth0-server")
        .WithExternalHttpEndpoints();
}

builder.Build().Run();
