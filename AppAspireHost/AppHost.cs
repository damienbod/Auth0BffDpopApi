using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<ProjectResource>? webApi = null;
IResourceBuilder<ProjectResource>? webApplication = null;

// Parameters for the web application
var webOidcClientPrivatePem = builder.AddParameter("WebOidcClientPrivatePem", secret: true);
var webOidcClientPublicPem = builder.AddParameter("WebOidcClientPublicPem");
var webDpopClientPrivatePem = builder.AddParameter("WebDpopClientPrivatePem", secret: true);
var webDpopClientPublicPem = builder.AddParameter("WebDpopClientPublicPem");

// Parameters for the web API
var apiAuth0Authority = builder.AddParameter("ApiAuth0Authority");
var apiAuth0Audience = builder.AddParameter("ApiAuth0Audience");
var apiAuth0Domain = builder.AddParameter("ApiAuth0Domain");
var apiDeploySwaggerUI = builder.AddParameter("ApiDeploySwaggerUI");

webApi = builder.AddProject<Projects.WebApi>("webapi")
    .WithExternalHttpEndpoints()
    .WithEnvironment("ApiAuth0Authority", apiAuth0Authority)
    .WithEnvironment("ApiAuth0Audience", apiAuth0Audience)
    .WithEnvironment("ApiAuth0Domain", apiAuth0Domain)
    .WithEnvironment("ApiDeploySwaggerUI", apiDeploySwaggerUI);

if (builder.Environment.IsDevelopment())
{
    var angularFrontend = builder.AddJavaScriptApp("angular", "../bff/ui", "start")
        .WithHttpsEndpoint(port: 3000, 4201, env: "BASE_URL");

    webApplication =builder.AddProject<Projects.BffAuth0_Server>("bffauth0-server")
        .WithReference(angularFrontend)
        .WithReference(webApi)
        .WaitFor(angularFrontend)
        .WithExternalHttpEndpoints();
}
else
{
    // Hint: to make this work, the deployment pipeline must execute npm run build 
    // which deploys to the wwwroot folder of the bffauth0-server project.
    webApplication = builder.AddProject<Projects.BffAuth0_Server>("bffauth0-server")
        .WithExternalHttpEndpoints()
        .WithReference(webApi)
        .WaitFor(webApi)
        //.WithEnvironment("WebOidcAuthority", webOidcAuthority)
        //.WithEnvironment("WebOidcClientId", webOidcClientId)
        .WithEnvironment("WebOidcClientPrivatePem", webOidcClientPrivatePem)
        .WithEnvironment("WebOidcClientPublicPem", webOidcClientPublicPem)
        .WithEnvironment("WebDpopClientPrivatePem", webDpopClientPrivatePem)
        .WithEnvironment("WebDpopClientPublicPem", webDpopClientPublicPem);
}

builder.Build().Run();
