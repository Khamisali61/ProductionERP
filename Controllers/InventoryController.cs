using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductionApi.Data;
using ProductionApi.Models;

namespace ProductionApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InventoryController : ControllerBase
    {
        private readonly AppDbContext _context;
        public InventoryController(AppDbContext context) { _context = context; }

        // --- NEW: SALES PEOPLE ---
        [HttpGet("salespeople")]
        public async Task<IActionResult> GetSalesPeople() => Ok(await _context.SalesPeople.ToListAsync());

        [HttpPost("salespeople")]
        public async Task<IActionResult> CreateSalesPerson([FromBody] SalesPerson sp)
        {
            _context.SalesPeople.Add(sp);
            await _context.SaveChangesAsync();
            return Ok(sp);
        }

        // --- PARTNERS ---
        [HttpGet("partners")]
        public async Task<IActionResult> GetPartners(string type = "All")
        {
            var query = _context.BusinessPartners.AsQueryable();
            if (type != "All") query = query.Where(p => p.Type == type);
            return Ok(await query.ToListAsync());
        }

        [HttpPost("partners")]
        public async Task<IActionResult> CreatePartner([FromBody] PartnerInputDto dto)
        {
            var p = new BusinessPartner
            {
                Name = dto.Name,
                Type = dto.Type,
                Phone = dto.Phone,
                Email = dto.Email,
                Address = dto.Address,
                SalesPersonId = dto.SalesPersonId // Save assigned Rep
            };
            _context.BusinessPartners.Add(p);
            await _context.SaveChangesAsync();
            return Ok(p);
        }

        // --- RAW MATERIALS (Your Original Logic) ---
        [HttpPost("raw-materials")]
        public async Task<IActionResult> SaveRawMaterial([FromBody] RawMaterial dto)
        {
            if (dto.Id == 0) _context.RawMaterials.Add(dto);
            else { var e = await _context.RawMaterials.FindAsync(dto.Id); if (e != null) { e.Name = dto.Name; e.UnitCost = dto.UnitCost; e.Category = dto.Category; } }
            await _context.SaveChangesAsync(); return Ok(dto);
        }

        [HttpGet("items")]
        public async Task<IActionResult> GetAllItems()
        {
            var rms = await _context.RawMaterials.Select(r => new { r.Id, r.Name, Type = "RawMaterial", Cost = r.UnitCost }).ToListAsync();
            var products = await _context.Products.Include(p => p.PackagingOptions).ToListAsync();
            var sellableItems = new List<object>();
            foreach (var p in products)
            {
                if (p.PackagingOptions.Any()) { foreach (var opt in p.PackagingOptions) { sellableItems.Add(new { Id = p.Id, PackagingOptionId = opt.Id, Name = $"{p.ProductName} - {opt.SizeLabel}", Type = "Product", Cost = 0m }); } }
                else { sellableItems.Add(new { Id = p.Id, PackagingOptionId = 0, Name = p.ProductName, Type = "Product", Cost = 0m }); }
            }
            return Ok(new { RawMaterials = rms, Products = sellableItems });
        }

        [HttpDelete("delete-item/{type}/{id}")]
        public async Task<IActionResult> DeleteItem(string type, int id)
        {
            var history = await _context.StockLedger.Where(s => s.ItemType == type && s.ItemId == id).ToListAsync();
            if (history.Any()) _context.StockLedger.RemoveRange(history);
            if (type == "RawMaterial") { var i = await _context.RawMaterials.FindAsync(id); if (i != null) { var u = await _context.ProductIngredients.Where(x => x.RawMaterialId == id).ToListAsync(); if (u.Any()) _context.ProductIngredients.RemoveRange(u); _context.RawMaterials.Remove(i); } }
            else { var i = await _context.Products.FindAsync(id); if (i != null) _context.Products.Remove(i); }
            await _context.SaveChangesAsync(); return Ok(new { message = "Deleted" });
        }

        [HttpGet("stock-levels")]
        public async Task<IActionResult> GetStockLevels()
        {
            var s = await _context.StockLedger.Where(x => x.ItemType == "RawMaterial").GroupBy(x => x.ItemId).Select(g => new { Id = g.Key, Q = g.Sum(x => x.QuantityChange), V = g.Sum(x => x.TotalValue) }).ToListAsync();
            var m = await _context.RawMaterials.ToListAsync();
            var rmRes = m.Select(r => { var st = s.FirstOrDefault(x => x.Id == r.Id); return new { Id = r.Id, Name = r.Name, Type = "RawMaterial", Category = r.Category, CurrentQty = st?.Q ?? 0, AvgValue = st?.V ?? 0, UOM = "KG" }; }).ToList();
            var prodRes = await _context.StockLedger.Where(x => x.ItemType == "Product").GroupBy(x => new { x.ItemId, x.PackagingOptionId, x.ItemName }).Select(g => new { Id = g.Key.ItemId, Name = g.Key.ItemName, Type = "Product", Category = "Finished Goods", CurrentQty = g.Sum(x => x.QuantityChange), AvgValue = g.Sum(x => x.TotalValue), UOM = "Unit" }).ToListAsync();
            var c = new List<object>(); c.AddRange(rmRes); c.AddRange(prodRes); return Ok(c);
        }

        [HttpPost("adjust")]
        public async Task<IActionResult> AdjustStock([FromBody] StockAdjustmentDto dto)
        {
            string n = ""; if (dto.ItemType == "RawMaterial") { var r = await _context.RawMaterials.FindAsync(dto.ItemId); n = r?.Name ?? "Unknown"; } else { var p = await _context.Products.Include(x => x.PackagingOptions).FirstOrDefaultAsync(x => x.Id == dto.ItemId); var o = p?.PackagingOptions.FirstOrDefault(x => x.Id == dto.PackagingOptionId); n = $"{p?.ProductName} ({o?.SizeLabel})"; }
            _context.StockLedger.Add(new StockTransaction { ItemType = dto.ItemType, ItemId = dto.ItemId, PackagingOptionId = dto.PackagingOptionId, ItemName = n, QuantityChange = dto.Quantity, UnitCost = dto.CostPrice, TotalValue = dto.Quantity * dto.CostPrice, ReferenceType = "Adjustment", Notes = dto.Reason, UnitOfMeasure = "Unit" });
            await _context.SaveChangesAsync(); return Ok();
        }
    }
}