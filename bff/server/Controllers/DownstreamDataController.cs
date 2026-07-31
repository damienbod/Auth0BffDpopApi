using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;

namespace BffAuth0.Server.Controllers;

[ValidateAntiForgeryToken]
[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
[ApiController]
[Route("api/[controller]")]
public class DownstreamDataController : ControllerBase
{
    private readonly IHttpClientFactory _clientFactory;

    public DownstreamDataController(IHttpClientFactory clientFactory)
    {
        _clientFactory = clientFactory;
    }

    [HttpGet]
    public async Task<string> GetAsync()
    {
        // if you need a delegated access token for downstream APIs
        var accessToken = await HttpContext.GetTokenAsync("access_token");

        return await GetApiDataUsingDelegatedToken(accessToken);
    }

    private async Task<string> GetApiDataUsingDelegatedToken(string? accessToken)
    {
        if(string.IsNullOrEmpty(accessToken))
        {
            throw new ArgumentNullException(nameof(accessToken), "Access token is null or empty.");
        }

        var client = _clientFactory.CreateClient();

        client.BaseAddress = new Uri("https://localhost:7288");


        client.DefaultRequestHeaders.Authorization
            = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.GetAsync("api/DownstreamData");

        if (response.IsSuccessStatusCode)
        {
            var data = await response.Content.ReadAsStringAsync();

            if (data != null)
            {
                return data;
            } 
        }

        var errorMessage = await response.Content.ReadAsStringAsync();
        throw new ApplicationException($"Status code: {response.StatusCode}, Error: {response.ReasonPhrase}, message: {errorMessage}");
    }
}
