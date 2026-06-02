using backend.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DatabaseController : ControllerBase
{
    private readonly AppDbContext _context;

    public DatabaseController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("test-connection")]
    public async Task<IActionResult> TestConnection()
    {
        var canConnect = await _context.Database.CanConnectAsync();

        return Ok(new
        {
            database = "FileManagementDb",
            canConnect
        });
    }

    [HttpGet("tables")]
    public IActionResult GetTables()
    {
        return Ok(new
        {
            tables = new[]
            {
                "Roles",
                "Users",
                "FileTypes",
                "FileRecords",
                "FileMetadata",
                "Permissions"
            }
        });
    }


}