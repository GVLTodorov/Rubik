using Microsoft.AspNetCore.Mvc;
using Rubik.Api.Configuration;

namespace Rubik.Api.Controllers;

/// <summary>Sends the site root to the Swagger UI.</summary>
[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class HomeController : ControllerBase
{
    [HttpGet("/")]
    public RedirectResult Index() => Redirect(RubikConstants.Route.Swagger);
}
