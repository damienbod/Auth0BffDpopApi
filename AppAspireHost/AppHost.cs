using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// Used in the applications when called downstream APIs, etc
const string WEB_APPLICATION = "web-app-bff-service";
const string API_SERVICE = "api-service"; 

IResourceBuilder<ProjectResource>? webApi = null;
IResourceBuilder<ProjectResource>? webApplication = null;

// Parameters for the web application
var webOidcClientPrivatePem = builder.AddParameter("WebOidcClientPrivatePem", secret: true);
var webOidcClientPublicPem = builder.AddParameter("WebOidcClientPublicPem");
var webDpopClientPrivatePem = builder.AddParameter("WebDpopClientPrivatePem", secret: true);
var webDpopClientPublicPem = builder.AddParameter("WebDpopClientPublicPem");

var webAuth0Authority = builder.AddParameter("WebAuth0Authority");
var webAuth0Audience = builder.AddParameter("WebAuth0Audience");
var webAuth0Domain = builder.AddParameter("WebAuth0Domain");
var webAuth0ClientId = builder.AddParameter("WebAuth0ClientId");
var webAuth0CallbackPath = builder.AddParameter("WebAuth0CallbackPath");

// Parameters for the web API
var apiAuth0Authority = builder.AddParameter("ApiAuth0Authority");
var apiAuth0Audience = builder.AddParameter("ApiAuth0Audience");
var apiAuth0Domain = builder.AddParameter("ApiAuth0Domain");
var apiDeploySwaggerUI = builder.AddParameter("ApiDeploySwaggerUI");

webApi = builder.AddProject<Projects.WebApi>(API_SERVICE)
    .WithExternalHttpEndpoints()
    .WithEnvironment("ApiAuth0Authority", apiAuth0Authority)
    .WithEnvironment("ApiAuth0Audience", apiAuth0Audience)
    .WithEnvironment("ApiAuth0Domain", apiAuth0Domain)
    .WithEnvironment("ApiDeploySwaggerUI", apiDeploySwaggerUI);

if (builder.Environment.IsDevelopment())
{
    var angularFrontend = builder.AddJavaScriptApp("angular", "../bff/ui", "start")
        .WithHttpsEndpoint(port: 3000, 4201, env: "BASE_URL");

    webApplication =builder.AddProject<Projects.BffAuth0_Server>(WEB_APPLICATION)
        .WithExternalHttpEndpoints()
        .WithReference(angularFrontend)
        .WaitFor(angularFrontend)
        .WithReference(webApi)
        .WaitFor(webApi)
        .WithEnvironment("WebAuth0Authority", webAuth0Authority)
        .WithEnvironment("WebAuth0Audience", webAuth0Audience)
        .WithEnvironment("WebAuth0Domain", webAuth0Domain)
        .WithEnvironment("WebAuth0ClientId", webAuth0ClientId)
        .WithEnvironment("WebAuth0CallbackPath", webAuth0CallbackPath)
        .WithEnvironment("WebOidcClientPrivatePem", webOidcClientPrivatePem)
        .WithEnvironment("WebOidcClientPublicPem", webOidcClientPublicPem)
        .WithEnvironment("WebDpopClientPrivatePem", webDpopClientPrivatePem)
        .WithEnvironment("WebDpopClientPublicPem", webDpopClientPublicPem);
}
else
{
    // Hint: to make this work, the deployment pipeline must execute npm run build 
    // which deploys to the wwwroot folder of the bffauth0-server project.
    webApplication = builder.AddProject<Projects.BffAuth0_Server>(WEB_APPLICATION)
        .WithExternalHttpEndpoints()
        .WithReference(webApi)
        .WaitFor(webApi)
        .WithEnvironment("WebAuth0Authority", webAuth0Authority)
        .WithEnvironment("WebAuth0Audience", webAuth0Audience)
        .WithEnvironment("WebAuth0Domain", webAuth0Domain)
        .WithEnvironment("WebAuth0ClientId", webAuth0ClientId)
        .WithEnvironment("WebAuth0CallbackPath", webAuth0CallbackPath)
        .WithEnvironment("WebOidcClientPrivatePem", webOidcClientPrivatePem)
        .WithEnvironment("WebOidcClientPublicPem", webOidcClientPublicPem)
        .WithEnvironment("WebDpopClientPrivatePem", webDpopClientPrivatePem)
        .WithEnvironment("WebDpopClientPublicPem", webDpopClientPublicPem);
}

builder.Build().Run();
