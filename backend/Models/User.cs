namespace backend.Models;

public class User
{
    public int UserId { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    public int RoleId { get; set; }
    public Role? Role { get; set; }

    public List<FileRecord> OwnedFiles { get; set; } = new();
    public List<Permission> Permissions { get; set; } = new();
}
