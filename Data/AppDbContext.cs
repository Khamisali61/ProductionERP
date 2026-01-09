using Microsoft.EntityFrameworkCore;
using ProductionApi.Models;

namespace ProductionApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Master Data
        public DbSet<RawMaterial> RawMaterials { get; set; }
        public DbSet<ProductDefinition> Products { get; set; }
        public DbSet<ProductIngredient> ProductIngredients { get; set; }
        public DbSet<PackagingOption> PackagingOptions { get; set; }

        // Production
        public DbSet<ProductionRun> ProductionRuns { get; set; }
        public DbSet<ProductionPackage> ProductionPackages { get; set; } // Required for Packaging Updates

        // Sales Module (NEW)
        public DbSet<Sale> Sales { get; set; }
        public DbSet<SaleItem> SaleItems { get; set; }
        public DbSet<SalePayment> SalePayments { get; set; }
        public DbSet<SalesPerson> SalesPeople { get; set; }

        // Procurement
        public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
        public DbSet<PurchaseOrderItem> PurchaseOrderItems { get; set; } // Added for safety

        // Inventory & Accounting
        public DbSet<StockTransaction> StockLedger { get; set; }
        public DbSet<BusinessPartner> BusinessPartners { get; set; }
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<User> Users { get; set; }
    }
}