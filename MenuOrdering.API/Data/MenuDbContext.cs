using MenuOrdering.API.Models;
using Microsoft.EntityFrameworkCore;

namespace MenuOrdering.API.Data;

public class MenuDbContext : DbContext
{
    public MenuDbContext(DbContextOptions<MenuDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<Restaurant> Restaurants => Set<Restaurant>();

    public DbSet<Branch> Branches => Set<Branch>();

    public DbSet<RestaurantTable> RestaurantTables => Set<RestaurantTable>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<MenuItem> MenuItems => Set<MenuItem>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    public DbSet<Bill> Bills => Set<Bill>();

    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // =========================
        // User
        // =========================

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.FullName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(x => x.Email)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(x => x.PasswordHash)
                .IsRequired();

            entity.Property(x => x.Role)
                .HasConversion<int>();

            entity.HasIndex(x => x.Email)
                .IsUnique();

            entity.HasOne(x => x.Restaurant)
                .WithMany(x => x.Users)
                .HasForeignKey(x => x.RestaurantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Branch)
                .WithMany(x => x.Users)
                .HasForeignKey(x => x.BranchId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // =========================
        // Restaurant
        // =========================

        modelBuilder.Entity<Restaurant>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(x => x.Description)
                .HasMaxLength(1000);

            entity.Property(x => x.Phone)
                .HasMaxLength(50);

            entity.Property(x => x.Email)
                .HasMaxLength(200);

            entity.HasIndex(x => x.Name)
                .IsUnique();
        });

        // =========================
        // Branch
        // =========================

        modelBuilder.Entity<Branch>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(x => x.Address)
                .HasMaxLength(500);

            entity.Property(x => x.Phone)
                .HasMaxLength(50);

            entity.HasIndex(x => new
            {
                x.RestaurantId,
                x.Name
            })
            .IsUnique();

            entity.HasOne(x => x.Restaurant)
                .WithMany(x => x.Branches)
                .HasForeignKey(x => x.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // =========================
        // Restaurant Table
        // =========================

        modelBuilder.Entity<RestaurantTable>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.TableNumber)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(x => x.QRCode)
                .HasMaxLength(500);

            // Table numbers are unique INSIDE a branch,
            // not globally.
            entity.HasIndex(x => new
            {
                x.BranchId,
                x.TableNumber
            })
            .IsUnique();

            entity.HasOne(x => x.Branch)
                .WithMany(x => x.Tables)
                .HasForeignKey(x => x.BranchId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // =========================
        // Category
        // =========================

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.Description)
                .HasMaxLength(500);

            entity.HasIndex(x => new
            {
                x.RestaurantId,
                x.Name
            })
            .IsUnique();

            entity.HasOne(x => x.Restaurant)
                .WithMany(x => x.Categories)
                .HasForeignKey(x => x.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // =========================
        // Menu Item
        // =========================

        modelBuilder.Entity<MenuItem>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(x => x.Description)
                .HasMaxLength(1000);

            entity.Property(x => x.Price)
                .HasPrecision(18, 2);

            entity.Property(x => x.ImageUrl)
                .HasMaxLength(500);

            entity.HasIndex(x => new
            {
                x.CategoryId,
                x.Name
            })
            .IsUnique();

            entity.HasOne(x => x.Category)
                .WithMany(x => x.MenuItems)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // =========================
        // Order
        // =========================

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Status)
                .HasConversion<int>();

            entity.Property(x => x.SubTotal)
                .HasPrecision(18, 2);

            entity.Property(x => x.Tax)
                .HasPrecision(18, 2);

            entity.Property(x => x.Total)
                .HasPrecision(18, 2);

            entity.HasOne(x => x.Branch)
                .WithMany(x => x.Orders)
                .HasForeignKey(x => x.BranchId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Table)
                .WithMany(x => x.Orders)
                .HasForeignKey(x => x.TableId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // =========================
        // Order Item
        // =========================

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.UnitPrice)
                .HasPrecision(18, 2);

            entity.Property(x => x.TotalPrice)
                .HasPrecision(18, 2);

            entity.Property(x => x.Notes)
                .HasMaxLength(500);

            entity.HasOne(x => x.Order)
                .WithMany(x => x.OrderItems)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.MenuItem)
                .WithMany(x => x.OrderItems)
                .HasForeignKey(x => x.MenuItemId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // =========================
        // Bill
        // =========================

        modelBuilder.Entity<Bill>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.SubTotal)
                .HasPrecision(18, 2);

            entity.Property(x => x.Tax)
                .HasPrecision(18, 2);

            entity.Property(x => x.ServiceCharge)
                .HasPrecision(18, 2);

            entity.Property(x => x.Total)
                .HasPrecision(18, 2);

            entity.HasOne(x => x.Order)
                .WithOne(x => x.Bill)
                .HasForeignKey<Bill>(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // =========================
        // Payment
        // =========================

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Amount)
                .HasPrecision(18, 2);

            entity.Property(x => x.Status)
                .HasConversion<int>();

            entity.Property(x => x.PaymentMethod)
                .HasMaxLength(50);

            entity.Property(x => x.TransactionReference)
                .HasMaxLength(200);

            entity.HasOne(x => x.Order)
                .WithMany(x => x.Payments)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}