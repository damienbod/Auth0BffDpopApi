using Duende.IdentityModel;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;

namespace BffAuth0.Server;

public static class AssertionService
{
    public static string CreateClientToken(IConfiguration configuration)
    {
        var now = DateTime.UtcNow;
        var clientId = configuration.GetValue<string>("Auth0ClientId");
        var authority = configuration.GetValue<string>("Auth0Authority");

        // Dev only!
        var oidcClientPrivatePem = File.ReadAllText(Path.Combine("", "rsa256-oidc-private.pem"));
        var oidcClientPublicPem = File.ReadAllText(Path.Combine("", "rsa256-oidc-public.pem"));

        // Deployments, Aspire setup
        //var oidcClientPrivatePem = builder.Configuration.GetValue<string>("OidcClientPrivatePem");
        //var oidcClientPublicPem = builder.Configuration.GetValue<string>("OidcClientPublicPem");

        var rsaCertificate = X509Certificate2.CreateFromPem(oidcClientPublicPem, oidcClientPrivatePem);
        var rsaCertificateKey = new RsaSecurityKey(rsaCertificate.GetRSAPrivateKey());

        string kid = Base64UrlEncoder.Encode(rsaCertificateKey.ComputeJwkThumbprint());
        var signingCredentials = new SigningCredentials(new X509SecurityKey(rsaCertificate, kid), "RS256");

        var token = new JwtSecurityToken(
            clientId,
            authority,
            new List<Claim>()
            {
                new Claim(JwtClaimTypes.JwtId, Guid.NewGuid().ToString()),
                new Claim(JwtClaimTypes.Subject, clientId!),
                new Claim(JwtClaimTypes.IssuedAt, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            },
            now,
            now.AddMinutes(5),
            signingCredentials
        );

        token.Header[JwtClaimTypes.TokenType] = "client-authentication+jwt";

        var tokenHandler = new JwtSecurityTokenHandler();
        tokenHandler.OutboundClaimTypeMap.Clear();

        return tokenHandler.WriteToken(token);
    }
}
