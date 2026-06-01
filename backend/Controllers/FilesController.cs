using backend.Data;
using backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBase
{
    private readonly AppDbContext _context;

    public FilesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetFiles()
    {
        var files = await _context.FileRecords
            .Include(f => f.Owner)
            .Include(f => f.FileType)
            .Select(f => new
            {
                f.FileRecordId,
                f.OriginalName,
                f.StoredName,
                f.StoragePath,
                f.SizeBytes,
                f.UploadedAt,
                owner = f.Owner != null ? f.Owner.Username : null,
                fileType = f.FileType != null ? f.FileType.Extension : null
            })
            .ToListAsync();

        return Ok(files);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetFile(int id)
    {
        var file = await _context.FileRecords
            .Include(f => f.Owner)
            .Include(f => f.FileType)
            .Include(f => f.Metadata)
            .FirstOrDefaultAsync(f => f.FileRecordId == id);

        if (file == null)
        {
            return NotFound(new { message = "File not found" });
        }

        return Ok(new
        {
            file.FileRecordId,
            file.OriginalName,
            file.StoredName,
            file.StoragePath,
            file.SizeBytes,
            file.UploadedAt,
            owner = file.Owner?.Username,
            fileType = file.FileType?.Extension,
            metadata = file.Metadata
        });
    }
}