using Duende.AccessTokenManagement.DPoP;
using Duende.AccessTokenManagement.OpenIdConnect;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

using BffAuth0.Server;
using BffAuth0.Server.Services;

using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.AddServerHeader = false;
});

var services = builder.Services;
var configuration = builder.Configuration;

services.AddSecurityHeaderPolicies()
    .SetPolicySelector(ctx =>
    {
        if (ctx.HttpContext.Request.Path.StartsWithSegments("/api"))
        {
            return ApiSecurityHeadersDefinitions.GetHeaderPolicyCollection(builder.Environment.IsDevelopment());
        }

        return SecurityHeadersDefinitions.GetHeaderPolicyCollection(
            builder.Environment.IsDevelopment(),
            configuration.GetValue<string>("Auth0:Domain"));
    });

services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN";
    options.Cookie.Name = "__Host-Http-X-XSRF-TOKEN";
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

services.AddHttpClient();
services.AddOptions();

// Dev only!
var oidcClientPrivatePem = File.ReadAllText(Path.Combine(builder.Environment.ContentRootPath, "rsa256-oidc-private.pem"));
var oidcClientPublicPem = File.ReadAllText(Path.Combine(builder.Environment.ContentRootPath, "rsa256-oidc-public.pem"));

// Deployments, Aspire setup
//var oidcClientPrivatePem = builder.Configuration.GetValue<string>("OidcClientPrivatePem");
//var oidcClientPublicPem = builder.Configuration.GetValue<string>("OidcClientPublicPem");

var rsaCertificate = X509Certificate2.CreateFromPem(oidcClientPublicPem, oidcClientPrivatePem);
var rsaCertificateKey = new RsaSecurityKey(rsaCertificate.GetRSAPrivateKey());

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = "Auth0"; // OpenIdConnectDefaults.AuthenticationScheme;
    options.DefaultSignOutScheme = "Auth0"; // OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.Cookie.Name = "__Host-Http-Auth0-Web";
    options.Cookie.SameSite = SameSiteMode.Lax;
    // can be strict if same-site
    //options.Cookie.SameSite = SameSiteMode.Strict;
})
.AddOpenIdConnect("Auth0", options =>
{
    options.Events = OidcEventHandlers.OidcEvents(builder.Configuration);

    options.Authority = $"https://{configuration.GetValue<string>("Auth0:Domain")}";
    options.ClientId = configuration.GetValue<string>("Auth0:ClientId");
    // Using client assertions, no secret used.
    //options.ClientSecret = configuration.GetValue<string>("Auth0:ClientSecret");
    options.ResponseType = OpenIdConnectResponseType.Code;
    options.Scope.Clear();
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");
 
    //options.CallbackPath = new PathString(configuration.GetValue<string>("Auth0:CallbackPath"));

    options.ClaimsIssuer = "Auth0";
    options.SaveTokens = true;
    options.UsePkce = true;

    // broken with Auth0, DPoP, PAR and client assertions
    options.GetClaimsFromUserInfoEndpoint = false;
    options.TokenValidationParameters.NameClaimType = "name";

    options.PushedAuthorizationBehavior = PushedAuthorizationBehavior.Require;
});

// Dev only!
var dpopClientPrivatePem = File.ReadAllText(Path.Combine(builder.Environment.ContentRootPath, "ecdsa256-dpop-private.pem"));
var dpopClientPublicPem = File.ReadAllText(Path.Combine(builder.Environment.ContentRootPath, "ecdsa256-dpop-public.pem"));

// Deployments, Aspire setup
//var dpopClientPrivatePem = builder.Configuration.GetValue<string>("DpopClientPrivatePem");
//var dpopClientPublicPem = builder.Configuration.GetValue<string>("DpopClientPublicPem");

var ecdsaCertificate = X509Certificate2.CreateFromPem(dpopClientPublicPem, dpopClientPrivatePem);
var ecdsaCertificateKey = new ECDsaSecurityKey(ecdsaCertificate.GetECDsaPrivateKey());

// add automatic token management
builder.Services.AddOpenIdConnectAccessTokenManagement(options =>
{
    // Only ES256 is supported by Auth0 DPoP
    var jwk = JsonWebKeyConverter.ConvertFromSecurityKey(ecdsaCertificateKey);
    jwk.Alg = "ES256";
    options.DPoPJsonWebKey = DPoPProofKey.ParseOrDefault(JsonSerializer.Serialize(jwk));
});

builder.Services.AddUserAccessTokenHttpClient("dpop-api-client", configureClient: client =>
{
    // See App Host for the api-service definition. This is the name of the service in the AppAspireHost project.
    client.BaseAddress = new("https+http://api-service");
});

//services.AddAuthentication(options =>
//{
//    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
//    options.DefaultChallengeScheme = Auth0Constants.AuthenticationScheme; //  OpenIdConnectDefaults.AuthenticationScheme;
//});

//builder.Services.AddAuth0WebAppAuthentication(options =>
//{
//    options.Domain = builder.Configuration.GetValue<string>("Auth0:Domain")!;
//    options.ClientId = builder.Configuration.GetValue<string>("Auth0:ClientId")!;
//    options.Scope = "openid profile email offline_access";
//    options.CallbackPath = builder.Configuration.GetValue<string>("Auth0:CallbackPath")!;

//    options.UsePushedAuthorization = true;
//    options.OpenIdConnectEvents = new OpenIdConnectEvents
//    {
//        OnPushAuthorization = context =>
//        {
//            context.ProtocolMessage.Parameters.Add("client_assertion", AssertionService.CreateClientToken(configuration));
//            context.ProtocolMessage.Parameters.Add("client_assertion_type", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer");
//            return Task.CompletedTask;
//        }
//    };  

//    options.ClientAssertionSecurityKey = rsaCertificateKey;
//    options.ClientAssertionSecurityKeyAlgorithm = "RS256";

//}).WithAccessToken(options =>
//{
//    options.Audience = builder.Configuration.GetValue<string>("Auth0:Audience")!;
//    options.UseRefreshTokens = true;
//});

services.AddControllersWithViews(options =>
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));

services.AddRazorPages().AddMvcOptions(options =>
{
    //var policy = new AuthorizationPolicyBuilder()
    //    .RequireAuthenticatedUser()
    //    .Build();
    //options.Filters.Add(new AuthorizeFilter(policy));
});

if (builder.Environment.IsDevelopment())
{
    // Development
    builder.Services.AddReverseProxy()
   .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
}
else
{
    // Production
    // Support for Aspire and Containers
    builder.Services.AddReverseProxy()
    .LoadFromMemory(YarpConfigurations.GetProductionRoutes(),
        YarpConfigurations.GetProductionClusters(builder.Configuration["DownstreamApiUrl"]!));
}

var app = builder.Build();

JsonWebTokenHandler.DefaultInboundClaimTypeMap.Clear();
// Remove this in deployments, only for debugging
IdentityModelEventSource.ShowPII = true;

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
}

app.UseSecurityHeaders();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseNoUnauthorizedRedirect("/api");

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();
app.MapNotFound("/api/{**segment}");

if (app.Environment.IsDevelopment())
{
    var uiDevServer = app.Configuration.GetValue<string>("UiDevServerUrl");
    if (!string.IsNullOrEmpty(uiDevServer))
    {
        app.MapReverseProxy();
    }
}
else
{
    // Proxy with production code configuration
    app.MapReverseProxy();
}

app.MapFallbackToPage("/_Host");

app.Run();
