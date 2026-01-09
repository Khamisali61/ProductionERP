using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ProductionApi.Models
{
    // --- 1. MASTER DATA ---
    public class RawMaterial { public int Id { get; set; } public string Name { get; set; } = string.Empty; public string Category { get; set; } = "General"; public decimal UnitCost { get; set; } }

    public class ProductDefinition
    {
        public int Id { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string UnitOfMeasure { get; set; } = "KG";
        public decimal OverheadPercentage { get; set; } = 10m;
        public decimal SpecificGravity { get; set; } = 1.0m;
        public List<ProductIngredient> Ingredients { get; set; } = new();
        public List<PackagingOption> PackagingOptions { get; set; } = new();
    }
    public class ProductIngredient { public int Id { get; set; } public int ProductDefinitionId { get; set; } public int RawMaterialId { get; set; } public virtual RawMaterial? RawMaterial { get; set; } public decimal StandardQty { get; set; } }
    public class PackagingOption { public int Id { get; set; } public int ProductDefinitionId { get; set; } public string SizeLabel { get; set; } = string.Empty; public decimal Capacity { get; set; } public decimal EmptyContainerCost { get; set; } }

    // --- 2. PRODUCTION DATA ---
    public class ProductionRun { public int Id { get; set; } public DateTime RunDate { get; set; } = DateTime.Now; public string BatchNumber { get; set; } = string.Empty; public int ProductDefinitionId { get; set; } public virtual ProductDefinition? ProductDefinition { get; set; } public decimal TotalRawMaterialCost { get; set; } public decimal GrandTotalProductionCost { get; set; } public decimal TotalYield { get; set; } public decimal CostPerKg { get; set; } public decimal CostPerLitre { get; set; } public List<ProductionPackage> Packages { get; set; } = new(); }
    public class ProductionPackage { public int Id { get; set; } public int ProductionRunId { get; set; } public string SizeLabel { get; set; } = string.Empty; public int QuantityProduced { get; set; } public decimal SnapshotLiquidCost { get; set; } public decimal SnapshotTinCost { get; set; } public decimal SnapshotVat { get; set; } public decimal UnitFinalCost { get; set; } }

    // --- 3. SALES MODULE (EXTENDED) ---
    public class SalesPerson
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
    }

    public class Sale
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime SaleDate { get; set; } = DateTime.Now;
        public int? CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;

        // Sales Rep Link
        public int? SalesPersonId { get; set; }
        public string SalesPersonName { get; set; } = string.Empty;

        public decimal TotalAmount { get; set; }

        // Payments & Balance
        public decimal PaidAmount { get; set; }
        public decimal Balance { get; set; }
        public string PaymentStatus { get; set; } = "Paid";
        public string PaymentMethod { get; set; } = "Cash";
        public string TransactionReference { get; set; } = string.Empty;

        public List<SaleItem> Items { get; set; } = new();
        public List<SalePayment> Payments { get; set; } = new();
    }

    public class SaleItem { public int Id { get; set; } public int SaleId { get; set; } public int ProductId { get; set; } public string ProductName { get; set; } = string.Empty; public int PackagingOptionId { get; set; } public string SizeLabel { get; set; } = string.Empty; public int Quantity { get; set; } public decimal UnitPrice { get; set; } public decimal LineTotal { get; set; } }

    public class SalePayment
    {
        public int Id { get; set; }
        public int SaleId { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
        public decimal Amount { get; set; }
        public string Method { get; set; } = "Cash";
        public string Reference { get; set; } = string.Empty;
    }

    // --- 4. ERP ---
    public class Expense { public int Id { get; set; } public DateTime Date { get; set; } = DateTime.Now; public string Description { get; set; } = string.Empty; public string Category { get; set; } = "General"; public decimal Amount { get; set; } }
    public class User { public int Id { get; set; } public string Username { get; set; } = string.Empty; public string Password { get; set; } = string.Empty; }
    public class LoginDto { public string Username { get; set; } = string.Empty; public string Password { get; set; } = string.Empty; }

    public class BusinessPartner
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = "Supplier";
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public int? SalesPersonId { get; set; }
    }

    public class StockTransaction { public int Id { get; set; } public DateTime Date { get; set; } = DateTime.Now; public string ItemType { get; set; } = "RawMaterial"; public int ItemId { get; set; } public int? PackagingOptionId { get; set; } public string ItemName { get; set; } = string.Empty; public decimal QuantityChange { get; set; } public string UnitOfMeasure { get; set; } = string.Empty; public decimal UnitCost { get; set; } public decimal TotalValue { get; set; } public string ReferenceType { get; set; } = "Manual"; public string ReferenceNumber { get; set; } = string.Empty; public string Notes { get; set; } = string.Empty; }
    public class PurchaseOrder { public int Id { get; set; } public string PoNumber { get; set; } = string.Empty; public DateTime OrderDate { get; set; } = DateTime.Now; public int SupplierId { get; set; } public string SupplierName { get; set; } = string.Empty; public string Status { get; set; } = "Pending"; public DateTime? ReceivedDate { get; set; } public string PaymentStatus { get; set; } = "Credit"; public string PaymentMethod { get; set; } = "None"; public string TransactionReference { get; set; } = string.Empty; public decimal TotalAmount { get; set; } public List<PurchaseOrderItem> Items { get; set; } = new(); }
    public class PurchaseOrderItem { public int Id { get; set; } public int PurchaseOrderId { get; set; } public int RawMaterialId { get; set; } public string RawMaterialName { get; set; } = string.Empty; public decimal Quantity { get; set; } public decimal UnitCost { get; set; } public decimal LineTotal { get; set; } }

    // --- DTOs ---
    public class PartnerInputDto
    {
        public string Name { get; set; } = string.Empty; public string Type { get; set; } = "Customer"; public string Phone { get; set; } = string.Empty; public string Email { get; set; } = string.Empty; public string Address { get; set; } = string.Empty;
        public int? SalesPersonId { get; set; }
    }

    public class CreateSaleDto
    {
        public int? CustomerId { get; set; }
        public string? WalkInName { get; set; }
        public string PaymentStatus { get; set; } = "Paid"; public string PaymentMethod { get; set; } = "Cash"; public string TransactionCode { get; set; } = string.Empty;
        public int? SalesPersonId { get; set; }
        public List<SaleItemDto> Items { get; set; } = new();
    }
    public class RepaymentDto { public int SaleId { get; set; } public decimal Amount { get; set; } public string Method { get; set; } = "Cash"; public string Reference { get; set; } = string.Empty; }

    public class ProductionInputDto { public string BatchNumber { get; set; } = string.Empty; public int ProductId { get; set; } public decimal TotalYield { get; set; } public decimal SpecificGravity { get; set; } public List<IngredientUsageDto> IngredientsUsed { get; set; } = new(); public List<PackingInputDto> Packaging { get; set; } = new(); }
    public class IngredientUsageDto { public int RawMaterialId { get; set; } public decimal QtyUsed { get; set; } }
    public class PackingInputDto { public int PackagingOptionId { get; set; } public int Quantity { get; set; } }
    public class StockAdjustmentDto { public string ItemType { get; set; } = "RawMaterial"; public int ItemId { get; set; } public int? PackagingOptionId { get; set; } public decimal Quantity { get; set; } public string Reason { get; set; } = string.Empty; public decimal CostPrice { get; set; } }
    public class CreatePoDto { public int SupplierId { get; set; } public string PaymentStatus { get; set; } = "Credit"; public string PaymentMethod { get; set; } = "None"; public string TransactionCode { get; set; } = string.Empty; public List<PoItemInputDto> Items { get; set; } = new(); }
    public class PoItemInputDto { public int RawMaterialId { get; set; } public decimal Quantity { get; set; } public decimal UnitCost { get; set; } }
    public class SaleItemDto { public int ProductId { get; set; } public int PackagingOptionId { get; set; } public int Quantity { get; set; } public decimal UnitPrice { get; set; } }
    public class CreateExpenseDto { public string Description { get; set; } = string.Empty; public decimal Amount { get; set; } public string Category { get; set; } = "General"; }
}