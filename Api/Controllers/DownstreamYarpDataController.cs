using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[Authorize(AuthenticationSchemes = "BearerDPoP")]
[ApiController]
[Route("api/[controller]")]
public class DownstreamYarpDataController(ILogger<DownstreamYarpDataController> _logger) : ControllerBase
{
    [AllowAnonymous]
    [EndpointSummary("This is a summary from OpenApi attributes.")]
    [EndpointDescription("This is a description from OpenApi attributes.")]
    [Produces(typeof(DataFromApi))]
    [HttpGet]
    public IActionResult Get()
    {
        _logger.LogDebug("Get downstream API with OpenAPI definitions");

        return Ok(new DataFromApi
        {
            Date = DateOnly.FromDateTime(DateTime.Now),
            Summary = "Sample data yarp proxied"
        });
    }
}
