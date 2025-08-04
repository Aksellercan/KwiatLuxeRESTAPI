using Microsoft.EntityFrameworkCore;
using KwiatLuxeRESTAPI.Models;

namespace KwiatLuxeRESTAPI
{
    public class KwiatLuxeDb : DbContext
    {
        public KwiatLuxeDb(DbContextOptions<KwiatLuxeDb> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderProduct> OrderProducts { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartProduct> CartProducts { get; set; }
        public DbSet<Token> Tokens { get; set; }

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

            modelBuilder.Entity<Cart>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.TotalAmount).HasColumnType("decimal(18,2)");
                entity.HasOne(c => c.User)
                    .WithOne(c => c.Cart)
                    .HasForeignKey<Cart>(c => c.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<CartProduct>(entity =>
            {
                entity.HasKey(cp => new { cp.CartId, cp.ProductId });
                entity.HasOne(cp => cp.Cart)
                    .WithMany(c => c.CartProducts)
                    .HasForeignKey(cp => cp.CartId);
                entity.HasOne(cp => cp.Product)
                    .WithMany(p => p.CartProducts)
                    .HasForeignKey(cp => cp.ProductId);
            });

            modelBuilder.Entity<Token>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.Property(t => t.RefreshToken).HasMaxLength(600);
                entity.Property(t => t.CreatedAt).IsRequired();
                entity.Property(t => t.ExpiresAt).IsRequired();
                entity.Property(t => t.RevokedAt);
                entity.HasOne(t => t.User)
                    .WithOne(t => t.Token)
                    .HasForeignKey<Token>(t => t.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}