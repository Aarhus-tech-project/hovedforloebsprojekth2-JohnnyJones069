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
    private readonly IConfiguration _configuration;

    public FilesController(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
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
                Owner = f.Owner != null ? f.Owner.Username : null,
                FileType = f.FileType != null ? f.FileType.Extension : null
            })
            .ToListAsync();

        return Ok(files);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetFileById(int id)
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
            Owner = file.Owner?.Username,
            FileType = file.FileType?.Extension,
            Metadata = file.Metadata
        });
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile(IFormFile file, int ownerId)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No file uploaded" });
        }

        var ownerExists = await _context.Users.AnyAsync(u => u.UserId == ownerId);

        if (!ownerExists)
        {
            return BadRequest(new { message = $"OwnerId {ownerId} does not exist" });
        }

        var extension = Path.GetExtension(file.FileName).ToLower();

        var fileType = await _context.FileTypes
            .FirstOrDefaultAsync(ft => ft.Extension == extension && ft.IsAllowed);

        if (fileType == null)
        {
            return BadRequest(new { message = $"File type {extension} is not allowed" });
        }

        var uploadPath = _configuration["FileStorage:UploadPath"] ?? "uploads";
        var fullUploadPath = Path.Combine(Directory.GetCurrentDirectory(), uploadPath);

        if (!Directory.Exists(fullUploadPath))
        {
            Directory.CreateDirectory(fullUploadPath);
        }

        var storedName = $"{Guid.NewGuid()}{extension}";
        var storagePath = Path.Combine(fullUploadPath, storedName);

        await using (var stream = new FileStream(storagePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var fileRecord = new FileRecord
        {
            OriginalName = file.FileName,
            StoredName = storedName,
            StoragePath = storagePath,
            SizeBytes = file.Length,
            UploadedAt = DateTime.UtcNow,
            OwnerId = ownerId,
            FileTypeId = fileType.FileTypeId
        };

        _context.FileRecords.Add(fileRecord);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "File uploaded",
            fileRecord.FileRecordId,
            fileRecord.OriginalName,
            fileRecord.StoredName,
            fileRecord.SizeBytes,
            fileRecord.UploadedAt
        });
    }

    [HttpGet("download/{id}")]
    public async Task<IActionResult> DownloadFile(int id)
    {
        var fileRecord = await _context.FileRecords
            .FirstOrDefaultAsync(f => f.FileRecordId == id);

        if (fileRecord == null)
        {
            return NotFound(new { message = "File not found in database" });
        }

        if (!System.IO.File.Exists(fileRecord.StoragePath))
        {
            return NotFound(new { message = "Physical file not found on server" });
        }

        var fileBytes = await System.IO.File.ReadAllBytesAsync(fileRecord.StoragePath);

        return File(fileBytes, "application/octet-stream", fileRecord.OriginalName);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteFile(int id)
    {
        var fileRecord = await _context.FileRecords
            .FirstOrDefaultAsync(f => f.FileRecordId == id);

        if (fileRecord == null)
        {
            return NotFound(new { message = "File not found" });
        }

        if (System.IO.File.Exists(fileRecord.StoragePath))
        {
            System.IO.File.Delete(fileRecord.StoragePath);
        }

        _context.FileRecords.Remove(fileRecord);
        await _context.SaveChangesAsync();

        return Ok(new { message = "File deleted" });
    }
}