using Auth0.AspNetCore.Authentication;
using BffAuth0.Server;
using BffAuth0.Server.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using NetEscapades.AspNetCore.SecurityHeaders.Infrastructure;
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
    options.Cookie.Name = "__Host_Http-X-XSRF-TOKEN";
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

services.AddHttpClient();
services.AddOptions();

var privatePem = File.ReadAllText(Path.Combine(builder.Environment.ContentRootPath, "rsa256-oidc-private.pem"));
var publicPem = File.ReadAllText(Path.Combine(builder.Environment.ContentRootPath, "rsa256-oidc-public.pem"));

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
    options.Domain = "dev-damienbod.eu.auth0.com"; // builder.Configuration["Auth0:Domain"];
    options.ClientId = "0erfhb9bqdefyOZp2x4b8lIP5Ampdf2P"; // builder.Configuration["Auth0:ClientId"];
    options.Scope = "https://auth0-api1";

    options.ClientAssertionSecurityKey = rsaCertificateKey;
    options.ClientAssertionSecurityKeyAlgorithm = "RS256";
});

//services.AddAuthentication(options =>
//{
//    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
//    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
//})
//.AddCookie(options =>
//{
//    options.Cookie.Name = "__Host_Http-auth0";
//    options.Cookie.SameSite = SameSiteMode.Lax;
//})
//.AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
//{
//    options.Authority = $"https://{configuration["Auth0:Domain"]}";
//    options.ClientId = configuration["Auth0:ClientId"];
//    options.ClientSecret = configuration["Auth0:ClientSecret"];
//    options.ResponseType = OpenIdConnectResponseType.Code;
//    options.Scope.Clear();
//    options.Scope.Add("openid");
//    options.Scope.Add("profile");
//    options.Scope.Add("email");
//    options.Scope.Add("auth0-user-api-one");
//    // options.CallbackPath = new PathString("/signin-oidc");
//    options.ClaimsIssuer = "Auth0";
//    options.SaveTokens = true;
//    options.UsePkce = true;
//    options.GetClaimsFromUserInfoEndpoint = true;
//    options.TokenValidationParameters.NameClaimType = "name";

//    options.Events = new OpenIdConnectEvents
//    {
//        OnTokenResponseReceived = context =>
//        {
//            var idToken = context.TokenEndpointResponse.IdToken;
//            return Task.CompletedTask;
//        },
//        // handle the logout redirection 
//        OnRedirectToIdentityProviderForSignOut = (context) =>
//        {
//            var logoutUri = $"https://{configuration["Auth0:Domain"]}/v2/logout?client_id={configuration["Auth0:ClientId"]}";

//            var postLogoutUri = context.Properties.RedirectUri;
//            if (!string.IsNullOrEmpty(postLogoutUri))
//            {
//                if (postLogoutUri.StartsWith("/"))
//                {
//                    // transform to absolute
//                    var request = context.Request;
//                    postLogoutUri = request.Scheme + "://" + request.Host + request.PathBase + postLogoutUri;
//                }
//                logoutUri += $"&returnTo={Uri.EscapeDataString(postLogoutUri)}";
//            }

//            context.Response.Redirect(logoutUri);
//            context.HandleResponse();

//            return Task.CompletedTask;
//        },
//        OnRedirectToIdentityProvider = context =>
//        {
//            // The context's ProtocolMessage can be used to pass along additional query parameters
//            // to Auth0's /authorize endpoint.
//            // 
//            // Set the audience query parameter to the API identifier to ensure the returned Access Tokens can be used
//            // to call protected endpoints on the corresponding API.
//            context.ProtocolMessage.SetParameter("audience", "https://auth0-api1");

//            return Task.FromResult(0);
//        }
//    };
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
