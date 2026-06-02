namespace backend.Models;

public class FileType
{
    public int FileTypeId { get; set; }
    
    public string Extension { get; set; } = string.Empty;
    public bool IsAllowed { get; set; } = true;

    public List<FileRecord> FileRecords { get; set; } = new();
}