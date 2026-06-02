namespace backend.Models;

public class FileRecord
{
    public int FileRecordId { get; set; }
    
    public string OriginalName { get; set; } = string.Empty;
    public string StoredName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;

    public long SizeBytes { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public int OwnerId { get; set; }
    public User? Owner { get; set; }

    public int FileTypeId { get; set; }
    public FileType? FileType { get; set; }

    public FileMetadata? Metadata { get; set; }
    public List<Permission> Permissions { get; set; } = new();
}