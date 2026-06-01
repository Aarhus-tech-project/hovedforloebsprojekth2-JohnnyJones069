namespace backend.Models;

public class Permission
{
    public int PermissionId { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    public int FileRecordId { get; set; }
    public FileRecord? FileRecord { get; set; }

    public bool CanDownload { get; set; } = true;
    public bool CanEdit { get; set; } = false;
    public bool CanDelete { get; set; } = false;
}