using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<FileType> FileTypes => Set<FileType>();
    public DbSet<FileRecord> FileRecords => Set<FileRecord>();
    public DbSet<FileMetadata> FileMetadata => Set<FileMetadata>();
    public DbSet<Permission> Permissions => Set<Permission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Role>().HasData(
            new Role { RoleId = 1, RoleName = "Admin" },
            new Role { RoleId = 2, RoleName = "User" }
        );

        modelBuilder.Entity<FileType>().HasData(
            new FileType { FileTypeId = 1, Extension = ".pdf", IsAllowed = true },
            new FileType { FileTypeId = 2, Extension = ".txt", IsAllowed = true },
            new FileType { FileTypeId = 3, Extension = ".docx", IsAllowed = true }
        );

        modelBuilder.Entity<FileRecord>()
            .HasOne(f => f.Owner)
            .WithMany(u => u.OwnedFiles)
            .HasForeignKey(f => f.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Permission>()
            .HasOne(p => p.User)
            .WithMany(u => u.Permissions)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Permission>()
            .HasOne(p => p.FileRecord)
            .WithMany(f => f.Permissions)
            .HasForeignKey(p => p.FileRecordId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FileMetadata>()
            .HasOne(m => m.FileRecord)
            .WithOne(f => f.Metadata)
            .HasForeignKey<FileMetadata>(m => m.FileRecordId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}