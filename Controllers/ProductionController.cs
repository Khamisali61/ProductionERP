using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductionApi.Data;
using ProductionApi.Models;

namespace ProductionApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductionController : ControllerBase
    {
        private readonly AppDbContext _context;
        public ProductionController(AppDbContext context) { _context = context; }

        [HttpGet("products")]
        public async Task<IActionResult> GetProducts()
        {
            // CRITICAL: Includes Ingredients AND RawMaterial details
            return Ok(await _context.Products
                .Include(p => p.Ingredients).ThenInclude(i => i.RawMaterial)
                .Include(p => p.PackagingOptions)
                .ToListAsync());
        }

        // ... [Rest of your controller code stays the same] ...
        // (Just ensure the GetProducts method above is updated)

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginDto login)
        {
            var user = _context.Users.FirstOrDefault(u => u.Username == login.Username && u.Password == login.Password);
            if (user != null) return Ok(new { Message = "Success", Token = "valid-session-token", Role = "Admin" });
            return Unauthorized();
        }

        [HttpPost("products/create")]
        public async Task<IActionResult> CreateProduct([FromBody] ProductDefinition dto) { _context.Products.Add(dto); await _context.SaveChangesAsync(); return Ok(dto); }

        [HttpPost("products/update")]
        public async Task<IActionResult> UpdateProduct([FromBody] ProductDefinition dto)
        {
            var existing = await _context.Products.Include(p => p.Ingredients).Include(p => p.PackagingOptions).FirstOrDefaultAsync(p => p.Id == dto.Id);
            if (existing == null) return NotFound();
            existing.ProductName = dto.ProductName; existing.UnitOfMeasure = dto.UnitOfMeasure; existing.OverheadPercentage = dto.OverheadPercentage; existing.SpecificGravity = dto.SpecificGravity;
            _context.ProductIngredients.RemoveRange(existing.Ingredients); existing.Ingredients = dto.Ingredients;
            _context.PackagingOptions.RemoveRange(existing.PackagingOptions); existing.PackagingOptions = dto.PackagingOptions;
            await _context.SaveChangesAsync(); return Ok(existing);
        }

        [HttpPost("run")]
        public async Task<IActionResult> RecordProduction([FromBody] ProductionInputDto input)
        {
            var product = await _context.Products.Include(p => p.PackagingOptions).FirstOrDefaultAsync(p => p.Id == input.ProductId);
            if (product == null) return BadRequest("Invalid Product");
            if (string.IsNullOrWhiteSpace(input.BatchNumber)) input.BatchNumber = $"BN-{DateTime.Now:yyyyMMdd}-{DateTime.Now:HHmm}";

            decimal totalRmCost = 0;
            foreach (var ing in input.IngredientsUsed)
            {
                var rm = await _context.RawMaterials.FindAsync(ing.RawMaterialId);
                if (rm != null)
                {
                    decimal cost = ing.QtyUsed * rm.UnitCost; totalRmCost += cost;
                    _context.StockLedger.Add(new StockTransaction { Date = DateTime.Now, ItemType = "RawMaterial", ItemId = rm.Id, ItemName = rm.Name, QuantityChange = -ing.QtyUsed, UnitOfMeasure = "KG", UnitCost = rm.UnitCost, TotalValue = -cost, ReferenceType = "Production", ReferenceNumber = input.BatchNumber, Notes = "Consumption" });
                }
            }
            decimal grandTotalCost = totalRmCost * (1 + (product.OverheadPercentage / 100m));
            decimal costPerKg = input.TotalYield > 0 ? grandTotalCost / input.TotalYield : 0;
            decimal costPerLitre = costPerKg * product.SpecificGravity;

            var run = new ProductionRun { BatchNumber = input.BatchNumber, ProductDefinitionId = input.ProductId, RunDate = DateTime.Now, TotalRawMaterialCost = totalRmCost, GrandTotalProductionCost = grandTotalCost, TotalYield = input.TotalYield, CostPerKg = costPerKg, CostPerLitre = costPerLitre, Packages = new List<ProductionPackage>() };

            foreach (var pkgInput in input.Packaging)
            {
                var option = product.PackagingOptions.FirstOrDefault(o => o.Id == pkgInput.PackagingOptionId);
                if (option != null && pkgInput.Quantity > 0)
                {
                    decimal liquidCost = option.Capacity * costPerLitre;
                    decimal finalUnitCost = (liquidCost + option.EmptyContainerCost) * 1.16m;
                    run.Packages.Add(new ProductionPackage { SizeLabel = option.SizeLabel, QuantityProduced = pkgInput.Quantity, SnapshotLiquidCost = liquidCost, SnapshotTinCost = option.EmptyContainerCost, SnapshotVat = (liquidCost + option.EmptyContainerCost) * 0.16m, UnitFinalCost = finalUnitCost });
                    _context.StockLedger.Add(new StockTransaction { Date = DateTime.Now, ItemType = "Product", ItemId = product.Id, PackagingOptionId = option.Id, ItemName = $"{product.ProductName} - {option.SizeLabel}", QuantityChange = pkgInput.Quantity, UnitOfMeasure = "Unit", UnitCost = finalUnitCost, TotalValue = pkgInput.Quantity * finalUnitCost, ReferenceType = "Production", ReferenceNumber = run.BatchNumber, Notes = "Output" });
                }
            }
            _context.ProductionRuns.Add(run); await _context.SaveChangesAsync(); return Ok(run);
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory() { return Ok(await _context.ProductionRuns.Include(r => r.ProductDefinition).Include(r => r.Packages).OrderByDescending(r => r.RunDate).ToListAsync()); }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRun(int id)
        {
            var run = await _context.ProductionRuns.FindAsync(id); if (run == null) return NotFound();
            var txns = await _context.StockLedger.Where(t => t.ReferenceNumber == run.BatchNumber).ToListAsync(); _context.StockLedger.RemoveRange(txns);
            var pkgs = await _context.ProductionPackages.Where(p => p.ProductionRunId == id).ToListAsync(); _context.ProductionPackages.RemoveRange(pkgs);
            _context.ProductionRuns.Remove(run); await _context.SaveChangesAsync(); return Ok(new { Message = "Deleted" });
        }

        [HttpPost("{id}/update-packaging")]
        public async Task<IActionResult> UpdateRunPackaging(int id, [FromBody] List<PackingInputDto> newPackages)
        {
            var run = await _context.ProductionRuns.Include(r => r.Packages).Include(r => r.ProductDefinition).ThenInclude(pd => pd!.PackagingOptions).FirstOrDefaultAsync(r => r.Id == id);
            if (run == null) return NotFound();
            var oldFgTxns = await _context.StockLedger.Where(t => t.ReferenceNumber == run.BatchNumber && t.ItemType == "Product").ToListAsync(); _context.StockLedger.RemoveRange(oldFgTxns);
            _context.ProductionPackages.RemoveRange(run.Packages);
            foreach (var input in newPackages)
            {
                var opt = run.ProductDefinition?.PackagingOptions.FirstOrDefault(o => o.Id == input.PackagingOptionId);
                if (opt != null)
                {
                    decimal liquidCost = opt.Capacity * run.CostPerLitre; decimal finalCost = (liquidCost + opt.EmptyContainerCost) * 1.16m;
                    run.Packages.Add(new ProductionPackage { ProductionRunId = run.Id, SizeLabel = opt.SizeLabel, QuantityProduced = input.Quantity, SnapshotLiquidCost = liquidCost, SnapshotTinCost = opt.EmptyContainerCost, SnapshotVat = (liquidCost + opt.EmptyContainerCost) * 0.16m, UnitFinalCost = finalCost });
                    _context.StockLedger.Add(new StockTransaction { Date = DateTime.Now, ItemType = "Product", ItemId = run.ProductDefinitionId, PackagingOptionId = opt.Id, ItemName = $"{run.ProductDefinition?.ProductName} - {opt.SizeLabel}", QuantityChange = input.Quantity, UnitOfMeasure = "Unit", UnitCost = finalCost, TotalValue = input.Quantity * finalCost, ReferenceType = "Production", ReferenceNumber = run.BatchNumber, Notes = "FG Updated" });
                }
            }
            await _context.SaveChangesAsync(); return Ok(run);
        }
    }
}