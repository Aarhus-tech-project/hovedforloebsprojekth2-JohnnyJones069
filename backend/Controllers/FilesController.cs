using backend.Data;
using backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace backend.Controllers;

[Authorize]
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

    private bool TryGetCurrentUser(out int currentUserId, out string role)
    {
        currentUserId = 0;
        role = string.Empty;

        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var roleClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

        if (string.IsNullOrEmpty(userIdClaim))
        {
            return false;
        }

        if (!int.TryParse(userIdClaim, out currentUserId))
        {
            return false;
        }

        role = roleClaim ?? string.Empty;
        return true;
    }

    [HttpGet]
    public async Task<IActionResult> GetFiles()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;

        if (string.IsNullOrEmpty(userIdClaim))
        {
            return Unauthorized("User ID or role was not found in token.");
        }

        if (!int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized("Invalid user ID in token.");
        }

        var isAdmin = roleClaim == "Admin";

        var query = _context.FileRecords
            .Include(f => f.Owner)
            .Include(f => f.FileType)
            .Include(f => f.Permissions)
            .AsQueryable();

        if (!isAdmin)
        {
            query = query.Where(f => 
                f.OwnerId == userId ||
                f.Permissions.Any(p => p.UserId == userId)
            );
        }

        var files = await query
            .Select(f => new
            {
                f.FileRecordId,
                f.PublicId,
                f.OriginalName,
                f.StoredName,
                f.RelativePath,
                f.SizeBytes,
                f.UploadedAt,
                Owner = f.Owner != null ? f.Owner.Username : null,
                FileType = f.FileType != null ? f.FileType.Extension : null
            })
            .ToListAsync();

        return Ok(files);
    }

    [HttpGet("{publicId}")]
    public async Task<IActionResult> GetFileById(Guid publicId)
    {
        if (!TryGetCurrentUser(out int currentUserId, out string role))
        {
            return Unauthorized("Invalid or missing user token.");
        }

        var file = await _context.FileRecords
            .Include(f => f.Owner)
            .Include(f => f.FileType)
            .Include(f => f.Metadata)
            .Include(f => f.Permissions)
            .FirstOrDefaultAsync(f => f.PublicId == publicId);

        if (file == null)
        {
            return NotFound(new { message = "File not found" });
        }

        var isAdmin = role == "Admin";
        var isOwner = file.OwnerId == currentUserId;
        var hasPermission = file.Permissions.Any(p => p.UserId == currentUserId);

        if (!isAdmin && !isOwner && !hasPermission)
        {
            return Forbid();
        }

        return Ok(new
        {
            file.FileRecordId,
            file.PublicId,
            file.OriginalName,
            file.StoredName,
            file.RelativePath,
            file.SizeBytes,
            file.UploadedAt,
            Owner = file.Owner?.Username,
            FileType = file.FileType?.Extension,
            Metadata = file.Metadata
        });
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim))
        {
            return Unauthorized("User ID was not found in token.");
        }

        if (!int.TryParse(userIdClaim, out int ownerId))
        {
            return Unauthorized("Invalid user ID in token.");
        }

        var owner = await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == ownerId);

        if (owner == null)
        {
            return Unauthorized("User does not exist.");
        }

        var extension = Path.GetExtension(file.FileName).ToLower();

        var fileType = await _context.FileTypes
            .FirstOrDefaultAsync(ft => ft.Extension.ToLower() == extension);

        if (fileType == null)
        {
            return BadRequest("File type is not allowed.");
        }

        var uploadPath = _configuration["FileStorage:UploadPath"] ?? "uploads";
        var fullUploadRoot = Path.Combine(Directory.GetCurrentDirectory(), uploadPath);

        var storedName = $"{Guid.NewGuid()}{extension}";

        var relativePath = Path.Combine(
            "users",
            owner.PublicId.ToString(),
            "documents",
            DateTime.UtcNow.Year.ToString(),
            DateTime.UtcNow.Month.ToString("00"),
            storedName
        );

        var fullPath = Path.Combine(fullUploadRoot, relativePath);

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var fileRecord = new FileRecord
        {
            PublicId = Guid.NewGuid(),
            OriginalName = file.FileName,
            StoredName = storedName,
            RelativePath = relativePath.Replace("\\", "/"),
            SizeBytes = file.Length,
            UploadedAt = DateTime.UtcNow,
            OwnerId = owner.UserId,
            FileTypeId = fileType.FileTypeId
        };

        _context.FileRecords.Add(fileRecord);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "File uploaded successfully.",
            fileId = fileRecord.FileRecordId,
            publicId = fileRecord.PublicId,
            originalName = fileRecord.OriginalName,
            storedName = fileRecord.StoredName,
            relativePath = fileRecord.RelativePath,
            ownerId = fileRecord.OwnerId
        });
    }

    [HttpGet("download/{publicId}")]
    public async Task<IActionResult> DownloadFile(Guid publicId)
    {
        if (!TryGetCurrentUser(out int currentUserId, out string role))
        {
            return Unauthorized("Invalid or missing user token.");
        }

        var fileRecord = await _context.FileRecords
            .Include(f => f.Permissions)
            .FirstOrDefaultAsync(f => f.PublicId == publicId);

        if (fileRecord == null)
        {
            return NotFound(new { message = "File not found in database" });
        }

        var isAdmin = role == "Admin";
        var isOwner = fileRecord.OwnerId == currentUserId;
        var hasPermission = fileRecord.Permissions.Any(p => p.UserId == currentUserId);

        if (!isAdmin && !isOwner && !hasPermission)
        {
            return Forbid();
        }

        if (!System.IO.File.Exists(fileRecord.StoragePath))
        {
            return NotFound(new { message = "Physical file not found on server" });
        }

        var fileBytes = await System.IO.File.ReadAllBytesAsync(fileRecord.StoragePath);

        return File(fileBytes, "application/octet-stream", fileRecord.OriginalName);
    }

    [HttpDelete("{publicId}")]
    public async Task<IActionResult> DeleteFile(Guid publicId)
    {
        if (!TryGetCurrentUser(out int currentUserId, out string role))
        {
            return Unauthorized("Invalid or missing user token.");
        }

        var fileRecord = await _context.FileRecords
            .FirstOrDefaultAsync(f => f.PublicId == publicId);

        if (fileRecord == null)
        {
            return NotFound(new { message = "File not found" });
        }

        var isAdmin = role == "Admin";
        var isOwner = fileRecord.OwnerId == currentUserId;

        if (!isAdmin && !isOwner)
        {
            return Forbid();
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