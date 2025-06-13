using Microsoft.EntityFrameworkCore;
using KwiatLuxeRESTAPI.Models;

namespace KwiatLuxeRESTAPI
{
    public class KwiatLuxeDb : DbContext
    {
       public KwiatLuxeDb(DbContextOptions<KwiatLuxeDb> options) : base(options) { }
        public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderProduct> OrderProducts { get; set; }
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

            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(o => o.Id);
                entity.Property(o => o.OrderDate).IsRequired();
                entity.Property(o => o.TotalAmount).HasColumnType("decimal(18,2)");
                entity.HasOne(o => o.User)
                      .WithMany(u => u.Orders)
                      .HasForeignKey(o => o.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<OrderProduct>(entity =>
            {
                entity.HasKey(op => new { op.OrderId, op.ProductId });
                entity.HasOne(op => op.Order)
                      .WithMany(o => o.OrderProducts)
                      .HasForeignKey(op => op.OrderId);
                entity.HasOne(op => op.Product)
                      .WithMany(p => p.OrderProducts)
                      .HasForeignKey(op => op.ProductId);
            });
        }
    }
}