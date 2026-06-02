namespace backend.Models;

public class FileMetadata
{
    public int FileMetadataId { get; set; }

    public int FileRecordId { get; set; }
    public FileRecord? FileRecord { get; set; }

    public string? Description { get; set; }
    public string? Tags { get; set; }
    public DateTime? LastModified { get; set; }
}