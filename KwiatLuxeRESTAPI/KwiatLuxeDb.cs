using Microsoft.EntityFrameworkCore;
using KwiatLuxeRESTAPI.Models;

namespace KwiatLuxeRESTAPI
{
    public class KwiatLuxeDb : DbContext
    {
       public KwiatLuxeDb(DbContextOptions<KwiatLuxeDb> options) : base(options) { }
        public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity => 
            { 
                entity.HasKey(u => u.Id);
                entity.Property(u => u.Username).IsRequired().HasMaxLength(50);
                entity.Property(u => u.Password).IsRequired().HasMaxLength(64);
                entity.Property(u => u.Email).IsRequired().HasMaxLength(100);
                entity.Property(u => u.Role).IsRequired().HasMaxLength(20);
            });

            modelBuilder.Entity<Product>(entity => 
            { 
                entity.HasKey(p => p.Id);
                entity.HasIndex(p => p.ProductName);
                entity.Property(p => p.ProductName).IsRequired().HasMaxLength(100);
                entity.Property(p => p.ProductDescription).HasMaxLength(500);
                entity.Property(p => p.ProductPrice).HasColumnType("decimal(18,2)");
                entity.Property(p => p.FileImageUrl).HasMaxLength(200);
            });
        }
    }
}