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
        var clientId = configuration.GetValue<string>("Auth0:ClientId");
        var authority = configuration.GetValue<string>("Auth0:Authority");

        //var privatePem = configuration.GetValue<string>("WebOidcClientPrivatePem");
        //var publicPem = configuration.GetValue<string>("WebOidcClientPublicPem");
        var privatePem = File.ReadAllText(Path.Combine("", "rsa256-oidc-private.pem"));
        var publicPem = File.ReadAllText(Path.Combine("", "rsa256-oidc-public.pem"));

        var rsaCertificate = X509Certificate2.CreateFromPem(publicPem, privatePem);
        var rsaCertificateKey = new RsaSecurityKey(rsaCertificate.GetRSAPrivateKey());
        var signingCredentials = new SigningCredentials(
                new X509SecurityKey(rsaCertificate, "GtUysKJ8XFsEnasIfWK3S9mIxCdlQzPKiP5piIPBUc8"), "RS256");

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
            now.AddMinutes(1),
            signingCredentials
        );

        token.Header[JwtClaimTypes.TokenType] = "client-authentication+jwt";

        var tokenHandler = new JwtSecurityTokenHandler();
        tokenHandler.OutboundClaimTypeMap.Clear();

        Console.WriteLine(rsaCertificate.Thumbprint);
        Console.WriteLine(signingCredentials.Kid);

        return tokenHandler.WriteToken(token);
    }
}
