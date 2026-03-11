using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace ShareSmallBiz.Portal.Controllers.api;

/// <summary>
/// Base class for all REST API controllers.
/// Enforces JWT Bearer authentication so React clients using Authorization: Bearer tokens
/// are handled correctly instead of falling through to the cookie scheme.
/// </summary>
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public abstract class ApiControllerBase : ControllerBase
{
}
