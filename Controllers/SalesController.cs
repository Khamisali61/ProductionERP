using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductionApi.Data;
using ProductionApi.Models;

namespace ProductionApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SalesController : ControllerBase
    {
        private readonly AppDbContext _context;
        public SalesController(AppDbContext context) { _context = context; }

        [HttpPost("create")]
        public async Task<IActionResult> CreateSale([FromBody] CreateSaleDto dto)
        {
            string custName = "Walk-In Client";
            int? custId = null;
            int? salesPersonId = dto.SalesPersonId;
            string salesPersonName = "Unassigned";

            // Resolve Customer
            if (dto.CustomerId.HasValue && dto.CustomerId > 0)
            {
                var customer = await _context.BusinessPartners.FindAsync(dto.CustomerId);
                if (customer == null) return BadRequest("Invalid Customer ID");
                custName = customer.Name;
                custId = customer.Id;
                if (!salesPersonId.HasValue && customer.SalesPersonId.HasValue) salesPersonId = customer.SalesPersonId;
            }
            else if (!string.IsNullOrWhiteSpace(dto.WalkInName)) custName = dto.WalkInName + " (Walk-in)";

            if (salesPersonId.HasValue)
            {
                var sp = await _context.SalesPeople.FindAsync(salesPersonId);
                if (sp != null) salesPersonName = sp.Name;
            }

            var sale = new Sale
            {
                InvoiceNumber = $"INV-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}",
                CustomerId = custId,
                CustomerName = custName,
                SalesPersonId = salesPersonId,
                SalesPersonName = salesPersonName,
                SaleDate = DateTime.Now,
                PaymentStatus = dto.PaymentStatus,
                PaymentMethod = dto.PaymentStatus == "Paid" ? dto.PaymentMethod : "Credit",
                TransactionReference = dto.TransactionCode ?? "",
                Items = new List<SaleItem>()
            };

            decimal total = 0;
            foreach (var item in dto.Items)
            {
                var prod = await _context.Products.Include(p => p.PackagingOptions).FirstOrDefaultAsync(p => p.Id == item.ProductId);
                if (prod != null)
                {
                    var option = prod.PackagingOptions.FirstOrDefault(o => o.Id == item.PackagingOptionId);
                    string sizeLabel = option?.SizeLabel ?? "Unit";
                    decimal lineTotal = item.Quantity * item.UnitPrice;
                    sale.Items.Add(new SaleItem { ProductId = prod.Id, PackagingOptionId = item.PackagingOptionId, ProductName = prod.ProductName, SizeLabel = sizeLabel, Quantity = item.Quantity, UnitPrice = item.UnitPrice, LineTotal = lineTotal });
                    total += lineTotal;
                    _context.StockLedger.Add(new StockTransaction { Date = DateTime.Now, ItemType = "Product", ItemId = prod.Id, PackagingOptionId = item.PackagingOptionId, ItemName = $"{prod.ProductName} ({sizeLabel})", QuantityChange = -item.Quantity, UnitOfMeasure = "Tin", UnitCost = 0, TotalValue = 0, ReferenceType = "Sale", ReferenceNumber = sale.InvoiceNumber, Notes = $"Sold to {custName}" });
                }
            }
            sale.TotalAmount = total;

            if (dto.PaymentStatus == "Paid")
            {
                sale.PaidAmount = total; sale.Balance = 0;
                sale.Payments.Add(new SalePayment { Amount = total, Date = DateTime.Now, Method = dto.PaymentMethod, Reference = dto.TransactionCode });
            }
            else
            {
                sale.PaidAmount = 0; sale.Balance = total;
            }

            _context.Sales.Add(sale); await _context.SaveChangesAsync(); return Ok(sale);
        }

        [HttpPost("pay")]
        public async Task<IActionResult> AddPayment([FromBody] RepaymentDto dto)
        {
            var sale = await _context.Sales.Include(s => s.Payments).FirstOrDefaultAsync(s => s.Id == dto.SaleId);
            if (sale == null) return NotFound("Sale not found");
            if (sale.Balance <= 0) return BadRequest("Already paid");

            var payment = new SalePayment { SaleId = sale.Id, Date = DateTime.Now, Amount = dto.Amount, Method = dto.Method, Reference = dto.Reference };
            sale.PaidAmount += dto.Amount; sale.Balance = sale.TotalAmount - sale.PaidAmount;
            if (sale.Balance <= 0) { sale.Balance = 0; sale.PaymentStatus = "Paid"; } else { sale.PaymentStatus = "Partial"; }

            _context.Set<SalePayment>().Add(payment); await _context.SaveChangesAsync();
            return Ok(new { Message = "Payment Recorded", NewBalance = sale.Balance });
        }

        [HttpGet("report")]
        public async Task<IActionResult> GetSalesReport()
        {
            var sales = await _context.Sales.ToListAsync();
            var report = sales.GroupBy(s => s.SalesPersonName).Select(g => new { SalesPerson = g.Key, TotalSales = g.Sum(x => x.TotalAmount), CashCollected = g.Sum(x => x.PaidAmount), Outstanding = g.Sum(x => x.Balance), Count = g.Count() }).ToList();
            return Ok(report);
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory() { return Ok(await _context.Sales.Include(s => s.Items).OrderByDescending(s => s.SaleDate).ToListAsync()); }
    }
}