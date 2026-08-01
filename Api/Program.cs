using Duende.AspNetCore.Authentication.JwtBearer.DPoP;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using NetEscapades.AspNetCore.SecurityHeaders.Infrastructure;
using WebApi;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddHybridCache();
builder.Services.AddKeyedHybridCache(ServiceProviderKeys.ProofTokenReplayHybridCache);

// -- DPoP setup 1: Auth0 client libs --
// Using Auth0 client libs:
// Auth0.AspNetCore.Authentication.Api Nuget package
// https://auth0.com/docs/quickstart/backend/aspnet-core-webapi
//builder.Services.AddAuth0ApiAuthentication(options =>
//{
//    options.Domain = builder.Configuration["Auth0:Domain"];
//    options.JwtBearerOptions = new JwtBearerOptions
//    {
//        Audience = builder.Configuration["Auth0:Audience"]
//    };
//}).WithDPoP(dpopOptions =>
//{
//    dpopOptions.Mode = DPoPModes.Allowed;
//});

// -- DPoP setup 2: all OIDC clients --
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer("Bearer", options =>
{
    options.Authority = "https://dev-damienbod.eu.auth0.com/";
    options.Audience = "https://auth0-api1";
});

// NOTE: DPoP is disabled here because of missing Auth0 enterprise license.
// -- DPoP setup: all OIDC clients --
// Duende.AspNetCore.Authentication.JwtBearer NuGet package
// layers DPoP onto the "token" scheme above
//builder.Services.ConfigureDPoPTokensForScheme("Bearer", opt =>
//{
//    opt.ProofTokenLifetime = TimeSpan.FromSeconds(10);

//    opt.ProofTokenValidationParameters.ValidAlgorithms =
//    [
//        SecurityAlgorithms.RsaSsaPssSha256,
//            SecurityAlgorithms.RsaSsaPssSha384,
//            SecurityAlgorithms.RsaSsaPssSha512,

//            SecurityAlgorithms.EcdsaSha256,
//            SecurityAlgorithms.EcdsaSha384,
//            SecurityAlgorithms.EcdsaSha512
//    ];
//});

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