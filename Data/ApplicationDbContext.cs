using JwtSecurityApi.Models;
using Microsoft.EntityFrameworkCore;

namespace JwtSecurityApi.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(user => user.Id);
            entity.HasIndex(user => user.NormalizedEmail).IsUnique();
            entity.Property(user => user.FullName).IsRequired();
            entity.Property(user => user.Email).IsRequired();
            entity.Property(user => user.NormalizedEmail).IsRequired();
            entity.Property(user => user.PasswordHash).IsRequired();
            entity.Property(user => user.Role).IsRequired();
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");
            entity.HasKey(product => product.Id);
            entity.Property(product => product.Name).IsRequired();
            entity.Property(product => product.Price).HasPrecision(18, 2);

            entity.HasOne(product => product.CreatedByUser)
                .WithMany()
                .HasForeignKey(product => product.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
