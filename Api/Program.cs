using Auth0.AspNetCore.Authentication.Api;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Logging;
using Microsoft.OpenApi;
using NetEscapades.AspNetCore.SecurityHeaders.Infrastructure;
using WebApi;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Open up security restrictions to allow this to work
// Not recommended in production
var deploySwaggerUI = builder.Configuration.GetValue<bool>("DeploySwaggerUI");
var isDev = builder.Environment.IsDevelopment();

builder.Services.AddSecurityHeaderPolicies()
    .SetPolicySelector((PolicySelectorContext ctx) =>
    {
        // sum is weak security headers due to Swagger UI deployment
        // should only use in development
        if (deploySwaggerUI) 
        {
            // Weakened security headers for Swagger UI
            if (ctx.HttpContext.Request.Path.StartsWithSegments("/swagger"))
            {               
                return SecurityHeadersDefinitionsSwagger.GetHeaderPolicyCollection(isDev);
            }

            // Strict security headers
            return SecurityHeadersDefinitionsAPI.GetHeaderPolicyCollection(isDev);
        }
        // Strict security headers for production
        else
        {
            return SecurityHeadersDefinitionsAPI.GetHeaderPolicyCollection(isDev);
        }
    });

builder.Services.AddControllers();

// -- DPoP setup 1: Auth0 client libs --
// Using Auth0 client libs:
// Auth0.AspNetCore.Authentication.Api Nuget package
// https://auth0.com/docs/quickstart/backend/aspnet-core-webapi
builder.Services.AddAuth0ApiAuthentication(Consts.DPOP_BEARER_SCHEME, options =>
{
    // Auth0 Nuget package bug: the Auth0:Domain configuration is required to be set in the options,
    // otherwise it will throw an exception. This is a bug in the Auth0 Nuget package.
    options.Domain = builder.Configuration.GetValue<string>("Auth0:Domain")!;
    options.Audience = builder.Configuration.GetValue<string>("Auth0:Audience")!;

}).WithDPoP(dpopOptions =>
{
    dpopOptions.Mode = Auth0.AspNetCore.Authentication.Api.DPoP.DPoPModes.Allowed;
});

builder.Services.AddAuthorization();

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});

var app = builder.Build();

IdentityModelEventSource.ShowPII = true;
JsonWebTokenHandler.DefaultInboundClaimTypeMap.Clear();

app.UseSecurityHeaders();

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

app.MapOpenApi("/openapi/v1/openapi.json");

if (deploySwaggerUI)
{
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1/openapi.json", "v1");
    });
}

app.Run();

internal sealed class BearerSecuritySchemeTransformer(IAuthenticationSchemeProvider authenticationSchemeProvider) : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var authenticationSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();
        if (authenticationSchemes.Any(authScheme => authScheme.Name == "Bearer"))
        {
            var requirements = new Dictionary<string, IOpenApiSecurityScheme>
            {
                ["Bearer"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer", // "bearer" refers to the header name here
                    In = ParameterLocation.Header,
                    BearerFormat = "Json Web Token"
                }
            };
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes = requirements;
        }
        document.Info = new()
        {
            Title = "My API Bearer scheme",
            Version = "v1",
            Description = "API for Damien"
        };
    }
}