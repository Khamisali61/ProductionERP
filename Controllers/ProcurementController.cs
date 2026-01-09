using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductionApi.Data;
using ProductionApi.Models;

namespace ProductionApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProcurementController : ControllerBase
    {
        private readonly AppDbContext _context;
        public ProcurementController(AppDbContext context) { _context = context; }

        [HttpGet("raw-materials")]
        public async Task<IActionResult> GetMaterials() { return Ok(await _context.RawMaterials.ToListAsync()); }

        [HttpPost("create")]
        public async Task<IActionResult> CreatePO([FromBody] CreatePoDto input)
        {
            var supplier = await _context.BusinessPartners.FindAsync(input.SupplierId);
            if (supplier == null) return BadRequest("Invalid Supplier");

            // Validate Payment Info
            if (input.PaymentStatus == "Paid" && (input.PaymentMethod == "Mpesa" || input.PaymentMethod == "Bank"))
            {
                if (string.IsNullOrWhiteSpace(input.TransactionCode))
                    return BadRequest("Transaction Code required for Mpesa/Bank");
            }

            var po = new PurchaseOrder
            {
                PoNumber = $"PO-{DateTime.Now:yyyyMMdd}-{new Random().Next(100, 999)}",
                SupplierId = supplier.Id,
                SupplierName = supplier.Name,
                Status = "Pending",

                // SAVE NEW FIELDS
                PaymentStatus = input.PaymentStatus,
                PaymentMethod = input.PaymentStatus == "Paid" ? input.PaymentMethod : "Credit",
                TransactionReference = input.TransactionCode ?? "",

                Items = new List<PurchaseOrderItem>()
            };

            decimal grandTotal = 0;
            foreach (var item in input.Items)
            {
                var rm = await _context.RawMaterials.FindAsync(item.RawMaterialId);
                if (rm != null)
                {
                    decimal lineTotal = item.Quantity * item.UnitCost;
                    po.Items.Add(new PurchaseOrderItem
                    {
                        RawMaterialId = rm.Id,
                        RawMaterialName = rm.Name,
                        Quantity = item.Quantity,
                        UnitCost = item.UnitCost,
                        LineTotal = lineTotal
                    });
                    grandTotal += lineTotal;
                }
            }
            po.TotalAmount = grandTotal;
            _context.PurchaseOrders.Add(po);
            await _context.SaveChangesAsync();
            return Ok(po);
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory()
        {
            return Ok(await _context.PurchaseOrders.Include(p => p.Items).OrderByDescending(p => p.OrderDate).ToListAsync());
        }

        [HttpPost("{id}/receive")]
        public async Task<IActionResult> ReceiveGoods(int id)
        {
            var po = await _context.PurchaseOrders.Include(p => p.Items).FirstOrDefaultAsync(p => p.Id == id);
            if (po == null) return NotFound("PO not found");
            if (po.Status == "Received") return BadRequest("Already Received");

            foreach (var item in po.Items)
            {
                // Find RM to check category/unit (Optional optimization)
                // For now, just add to stock
                _context.StockLedger.Add(new StockTransaction
                {
                    Date = DateTime.Now,
                    ItemType = "RawMaterial",
                    ItemId = item.RawMaterialId,
                    ItemName = item.RawMaterialName,
                    QuantityChange = item.Quantity,
                    UnitOfMeasure = "Unit",
                    UnitCost = item.UnitCost,
                    TotalValue = item.LineTotal,
                    ReferenceType = "Purchase",
                    ReferenceNumber = po.PoNumber,
                    Notes = $"From {po.SupplierName} ({po.PaymentStatus})"
                });
            }
            po.Status = "Received";
            po.ReceivedDate = DateTime.Now;
            await _context.SaveChangesAsync();
            return Ok(po);
        }
    }
}