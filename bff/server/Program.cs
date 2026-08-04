using Auth0.AspNetCore.Authentication;
using BffAuth0.Server;
using BffAuth0.Server.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography.X509Certificates;

var builder = WebApplication.CreateBuilder(args);

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
            configuration["Auth0:Domain"]);
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
var privatePem = File.ReadAllText(Path.Combine(builder.Environment.ContentRootPath, "rsa256-oidc-private.pem"));
var publicPem = File.ReadAllText(Path.Combine(builder.Environment.ContentRootPath, "rsa256-oidc-public.pem"));

// Deployments, Aspire setup
//var webDpopClientPrivatePem = builder.Configuration.GetValue<string>("WebDpopClientPrivatePem");
//var webDpopClientPublicPem = builder.Configuration.GetValue<string>("WebDpopClientPublicPem");

var rsaCertificate = X509Certificate2.CreateFromPem(publicPem, privatePem);
var rsaCertificateKey = new RsaSecurityKey(rsaCertificate.GetRSAPrivateKey());

services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = Auth0Constants.AuthenticationScheme; //  OpenIdConnectDefaults.AuthenticationScheme;
});

builder.Services.AddAuth0WebAppAuthentication(options =>
{
    options.Domain = builder.Configuration["Auth0:Domain"]!;
    options.ClientId = builder.Configuration["Auth0:AppClientId"]!;
    options.Scope = "openid profile email offline_access";
    options.CallbackPath = builder.Configuration["Auth0:CallbackPath"]!;

    options.ClientAssertionSecurityKey = rsaCertificateKey;
    options.ClientAssertionSecurityKeyAlgorithm = "RS256";

}).WithAccessToken(options =>
{
    options.Audience = builder.Configuration["Auth0:Audience"]!;
    options.UseRefreshTokens = true;
});

services.AddControllersWithViews(options =>
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));

services.AddRazorPages().AddMvcOptions(options =>
{
    //var policy = new AuthorizationPolicyBuilder()
    //    .RequireAuthenticatedUser()
    //    .Build();
    //options.Filters.Add(new AuthorizeFilter(policy));
});

builder.Services.AddReverseProxy()
   .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

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

app.MapFallbackToPage("/_Host");

app.Run();
