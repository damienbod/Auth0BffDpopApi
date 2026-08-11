using Duende.AccessTokenManagement;
using Duende.AccessTokenManagement.DPoP;
using Duende.IdentityModel;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using System.Net.Http.Headers;

namespace BffAuth0.Server;

public static class OidcEventHandlers
{
    public static OpenIdConnectEvents OidcEvents(IConfiguration configuration)
    {
        return new OpenIdConnectEvents
        {
            OnAuthorizationCodeReceived = async context => await OnAuthorizationCodeReceivedHandler(context, configuration),

            // use OAuth PAR
            OnPushAuthorization = async context => await OnPushAuthorizationHandler(context, configuration),

            OnRedirectToIdentityProviderForSignOut = async context => await OnRedirectToIdentityProviderForSignOutHandler(context, configuration),

            // standard OIDC flow handlers using JAR and client assertions - not using OAuth PAR
            //OnRedirectToIdentityProvider = async context => await OnRedirectToIdentityProviderHandler(context, configuration),
        };
    }

    private static async Task OnRedirectToIdentityProviderForSignOutHandler(RedirectContext context, IConfiguration configuration)
    {
        var logoutUri = $"https://{configuration.GetValue<string>("Auth0:Domain")}/v2/logout?client_id={configuration.GetValue<string>("Auth0:ClientId")}";

        var postLogoutUri = context.Properties.RedirectUri;
        if (!string.IsNullOrEmpty(postLogoutUri))
        {
            if (postLogoutUri.StartsWith("/"))
            {
                // transform to absolute
                var request = context.Request;
                postLogoutUri = request.Scheme + "://" + request.Host + request.PathBase + postLogoutUri;
            }
            logoutUri += $"&returnTo={Uri.EscapeDataString(postLogoutUri)}";
        }

        context.Response.Redirect(logoutUri);
        context.HandleResponse();
    }

    private static async Task OnAuthorizationCodeReceivedHandler(AuthorizationCodeReceivedContext context, IConfiguration configuration)
    {
        // https://openid.net/specs/openid-connect-eap-acr-values-1_0-final.html
        if (context.Properties != null && context.Properties.Items.ContainsKey("acr_values"))
        {
            context.ProtocolMessage.AcrValues = context.Properties.Items["acr_values"];
        }

        if (context.TokenEndpointRequest != null)
        {
            context.TokenEndpointRequest.ClientAssertionType = OidcConstants.ClientAssertionTypes.JwtBearer;
            context.TokenEndpointRequest.ClientAssertion = AssertionService.CreateClientToken(configuration);
        }
    }

    /// <summary>
    /// Not using OAuth PAR
    /// </summary>
    //private static async Task OnRedirectToIdentityProviderHandler(RedirectContext context, IConfiguration configuration)
    //{
    //    var request = AssertionService.SignAuthorizationRequest(context.ProtocolMessage, configuration);
    //    var clientId = context.ProtocolMessage.ClientId;
    //    var redirectUri = context.ProtocolMessage.RedirectUri;

    //    context.ProtocolMessage.Parameters.Clear();
    //    context.ProtocolMessage.ClientId = clientId;
    //    context.ProtocolMessage.RedirectUri = redirectUri;
    //    context.ProtocolMessage.SetParameter("request", request);
    //}

    private static async Task OnPushAuthorizationHandler(PushedAuthorizationContext context, IConfiguration configuration)
    {
        context.ProtocolMessage.Parameters.Add("client_assertion", AssertionService.CreateClientToken(configuration));
        context.ProtocolMessage.Parameters.Add("client_assertion_type", OidcConstants.ClientAssertionTypes.JwtBearer);

        context.ProtocolMessage.Parameters.Add("audience", configuration.GetValue<string>("Auth0:Audience"));

        context.HandleClientAuthentication();

        // https://openid.net/specs/openid-connect-eap-acr-values-1_0-final.html
        if (context.Properties.Items.ContainsKey("acr_values"))
        {
            context.ProtocolMessage.AcrValues = context.Properties.Items["acr_values"];
        }
    }
}
