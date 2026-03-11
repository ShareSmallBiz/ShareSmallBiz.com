using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace ShareSmallBiz.Portal.Controllers.api.admin;

[ApiController]
[Route("api/admin/roles")]
[Authorize(Roles = "Admin", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class AdminRolesController(
    RoleManager<IdentityRole> roleManager,
    ILogger<AdminRolesController> logger) : ControllerBase
{
    /// <summary>GET /api/admin/roles — list all roles</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var roles = await roleManager.Roles.OrderBy(r => r.Name).ToListAsync();
        return Ok(roles.Select(r => new { r.Id, r.Name }));
    }

    /// <summary>POST /api/admin/roles — create a new role</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRoleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { Message = "Role name is required." });

        if (await roleManager.RoleExistsAsync(request.Name))
            return Conflict(new { Message = $"Role '{request.Name}' already exists." });

        var result = await roleManager.CreateAsync(new IdentityRole(request.Name));
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        logger.LogInformation("Admin created role: {RoleName}", request.Name);
        return Ok(new { Message = $"Role '{request.Name}' created." });
    }

    /// <summary>DELETE /api/admin/roles/{roleId} — delete a role</summary>
    [HttpDelete("{roleId}")]
    public async Task<IActionResult> Delete(string roleId)
    {
        var role = await roleManager.FindByIdAsync(roleId);
        if (role is null) return NotFound();

        var result = await roleManager.DeleteAsync(role);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        logger.LogInformation("Admin deleted role: {RoleName}", role.Name);
        return NoContent();
    }
}

public class CreateRoleRequest
{
    public string Name { get; set; } = string.Empty;
}
