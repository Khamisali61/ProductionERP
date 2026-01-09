using Microsoft.EntityFrameworkCore;
using ProductionApi.Data;
using ProductionApi.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
// UPDATED TO V15 TO FORCE A CLEAN DATABASE REBUILD
builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlite("Data Source=erp_production_v15.db"));
builder.Services.AddCors(o => o.AddPolicy("AllowAll", b => b.AllowAnyMethod().AllowAnyHeader().AllowAnyOrigin()));
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    // Only run seeder if table is empty
    if (!db.RawMaterials.Any())
    {
        Console.WriteLine("--- STARTING DATABASE SEEDING ---");

        // 1. SALES PEOPLE
        if (!db.SalesPeople.Any())
        {
            db.SalesPeople.AddRange(
                new SalesPerson { Name = "John Doe", Region = "Nairobi", Phone = "0700123456" },
                new SalesPerson { Name = "Jane Smith", Region = "Mombasa", Phone = "0700987654" }
            );
            db.SaveChanges();
        }

        // 2. RAW MATERIALS (The Master List)
        var rms = new List<RawMaterial> {
            // --- SOLUTIONS ---
            new() { Name = "Water", Category = "SOLUTION", UnitCost = 0.10m },
            new() { Name = "Long Oil Alkyd (Synresins)", Category = "SOLUTION", UnitCost = 245.00m },
            new() { Name = "Long Oil Alkyd (Black)", Category = "SOLUTION", UnitCost = 170.00m },
            new() { Name = "Industrial Kerosene", Category = "SOLUTION", UnitCost = 171.95m },
            new() { Name = "Kerosene", Category = "SOLUTION", UnitCost = 171.95m },
            new() { Name = "White Spirit", Category = "SOLUTION", UnitCost = 205.00m },
            new() { Name = "Pine Oil", Category = "SOLUTION", UnitCost = 650.00m },
            new() { Name = "Dispersant-WD Eagle", Category = "SOLUTION", UnitCost = 220.00m },
            new() { Name = "Acticide K14", Category = "SOLUTION", UnitCost = 215.00m },
            new() { Name = "Acticide EPW Paste", Category = "SOLUTION", UnitCost = 800.00m },
            new() { Name = "Styrene Acrylic", Category = "SOLUTION", UnitCost = 170.00m },
            new() { Name = "Delta 1501", Category = "SOLUTION", UnitCost = 380.00m },
            new() { Name = "Calcium Drier", Category = "SOLUTION", UnitCost = 380.00m },
            new() { Name = "Cobalt Drier", Category = "SOLUTION", UnitCost = 1050.00m },
            new() { Name = "Zirconium 18%", Category = "SOLUTION", UnitCost = 550.00m },
            new() { Name = "Antiskin (Meko)", Category = "SOLUTION", UnitCost = 380.00m },
            new() { Name = "Eagle ES-4045", Category = "SOLUTION", UnitCost = 370.00m },
            new() { Name = "Ammonia Sol", Category = "SOLUTION", UnitCost = 95.00m },
            new() { Name = "Dispex N40", Category = "SOLUTION", UnitCost = 220.00m },
            new() { Name = "Antifoam", Category = "SOLUTION", UnitCost = 380.00m },
            new() { Name = "NPG", Category = "SOLUTION", UnitCost = 327.50m },
            new() { Name = "MPG", Category = "SOLUTION", UnitCost = 220.00m },
            new() { Name = "Acetone", Category = "SOLUTION", UnitCost = 175.00m },
            new() { Name = "Kerosine (Turp)", Category = "SOLUTION", UnitCost = 172.41m },

            // --- DRY POWDER ---
            new() { Name = "HEC Thickener", Category = "DRY POWDER", UnitCost = 970.00m },
            new() { Name = "Melwit-34", Category = "DRY POWDER", UnitCost = 8.00m },
            new() { Name = "Melgrit RT-A (30/80)", Category = "DRY POWDER", UnitCost = 10.00m },
            new() { Name = "Melgrit RT-D (3mm)", Category = "DRY POWDER", UnitCost = 10.00m },
            new() { Name = "Work Off", Category = "DRY POWDER", UnitCost = 35.83m },
            new() { Name = "Sodium Benzoate", Category = "DRY POWDER", UnitCost = 215.00m },
            new() { Name = "Bentone", Category = "DRY POWDER", UnitCost = 475.00m },
            new() { Name = "TIO2", Category = "DRY POWDER", UnitCost = 350.00m },
            new() { Name = "Titanium", Category = "DRY POWDER", UnitCost = 360.00m },
            new() { Name = "Black Paste", Category = "DRY POWDER", UnitCost = 540.00m },
            new() { Name = "Hydrocarbon Resin", Category = "DRY POWDER", UnitCost = 249.34m },
            new() { Name = "Calgon", Category = "DRY POWDER", UnitCost = 225.00m },
            new() { Name = "Bermocol(Tylose)", Category = "DRY POWDER", UnitCost = 800.00m },
            new() { Name = "Mergal K6N", Category = "DRY POWDER", UnitCost = 215.00m },

            // --- PACKAGING ---
            new() { Name = "Empty Tin 1L", Category = "PACKAGING", UnitCost = 51.00m },
            new() { Name = "Empty Tin 4L", Category = "PACKAGING", UnitCost = 121.00m },
            new() { Name = "Empty Tin 0.5L", Category = "PACKAGING", UnitCost = 43.00m },
            new() { Name = "Empty Tin 0.25L", Category = "PACKAGING", UnitCost = 29.00m },
            new() { Name = "Empty Bucket 20L", Category = "PACKAGING", UnitCost = 270.00m },
            new() { Name = "Empty Bucket 10L", Category = "PACKAGING", UnitCost = 170.00m },
            new() { Name = "Empty Bucket 5L", Category = "PACKAGING", UnitCost = 61.44m },
            new() { Name = "Empty Bucket 30KG", Category = "PACKAGING", UnitCost = 289.00m },
            new() { Name = "Carton 4x4L", Category = "PACKAGING", UnitCost = 45.00m }
        };

        // Save Raw Materials first so we can query them
        db.RawMaterials.AddRange(rms);
        db.SaveChanges();
        Console.WriteLine("Raw Materials Saved.");

        // 3. LOOKUP HELPER (Now queries the DB directly to be sure)
        int GetRm(string name)
        {
            var item = db.RawMaterials.AsEnumerable()
                         .FirstOrDefault(r => r.Name.Trim().Equals(name.Trim(), StringComparison.OrdinalIgnoreCase));

            if (item == null)
            {
                // DEBUG: Print what failed
                Console.WriteLine($"[CRITICAL ERROR] Could not find ingredient: '{name}'");
                throw new Exception($"Missing RM: {name}");
            }
            return item.Id;
        }

        try
        {
            Console.WriteLine("Seeding Products...");

            // --- P1: HI-GLOSS WHITE ---
            var p1 = new ProductDefinition { ProductName = "HI-GLOSS ECON WHITE", UnitOfMeasure = "KG", SpecificGravity = 1.10m, OverheadPercentage = 10 };
            db.Products.Add(p1); db.SaveChanges();

            db.ProductIngredients.AddRange(
                new ProductIngredient { ProductDefinitionId = p1.Id, RawMaterialId = GetRm("Long Oil Alkyd (Synresins)"), StandardQty = 40.00m },
                new ProductIngredient { ProductDefinitionId = p1.Id, RawMaterialId = GetRm("Kerosene"), StandardQty = 24.00m },
                new ProductIngredient { ProductDefinitionId = p1.Id, RawMaterialId = GetRm("Calcium Drier"), StandardQty = 0.60m },
                new ProductIngredient { ProductDefinitionId = p1.Id, RawMaterialId = GetRm("Sodium Benzoate"), StandardQty = 0.20m },
                new ProductIngredient { ProductDefinitionId = p1.Id, RawMaterialId = GetRm("Bentone"), StandardQty = 0.03m },
                new ProductIngredient { ProductDefinitionId = p1.Id, RawMaterialId = GetRm("TIO2"), StandardQty = 8.00m },
                new ProductIngredient { ProductDefinitionId = p1.Id, RawMaterialId = GetRm("Water"), StandardQty = 140.00m },
                new ProductIngredient { ProductDefinitionId = p1.Id, RawMaterialId = GetRm("Eagle ES-4045"), StandardQty = 2.00m },
                new ProductIngredient { ProductDefinitionId = p1.Id, RawMaterialId = GetRm("Zirconium 18%"), StandardQty = 2.00m },
                new ProductIngredient { ProductDefinitionId = p1.Id, RawMaterialId = GetRm("Cobalt Drier"), StandardQty = 0.40m },
                new ProductIngredient { ProductDefinitionId = p1.Id, RawMaterialId = GetRm("Antiskin (Meko)"), StandardQty = 0.50m }
            );

            db.PackagingOptions.AddRange(
                new PackagingOption { ProductDefinitionId = p1.Id, SizeLabel = "1L", Capacity = 1, EmptyContainerCost = 51 },
                new PackagingOption { ProductDefinitionId = p1.Id, SizeLabel = "4L", Capacity = 4, EmptyContainerCost = 121 },
                new PackagingOption { ProductDefinitionId = p1.Id, SizeLabel = "0.5L", Capacity = 0.5m, EmptyContainerCost = 43 },
                new PackagingOption { ProductDefinitionId = p1.Id, SizeLabel = "0.25L", Capacity = 0.25m, EmptyContainerCost = 29 }
            );

            // --- P2: HI-GLOSS BLACK ---
            var p2 = new ProductDefinition { ProductName = "HI-GLOSS ECONOMY BLACK", UnitOfMeasure = "KG", SpecificGravity = 1.10m, OverheadPercentage = 10 };
            db.Products.Add(p2); db.SaveChanges();

            db.ProductIngredients.AddRange(
                new ProductIngredient { ProductDefinitionId = p2.Id, RawMaterialId = GetRm("Long Oil Alkyd (Black)"), StandardQty = 34 },
                new ProductIngredient { ProductDefinitionId = p2.Id, RawMaterialId = GetRm("Kerosene"), StandardQty = 24 },
                new ProductIngredient { ProductDefinitionId = p2.Id, RawMaterialId = GetRm("Black Paste"), StandardQty = 12 }
            );

            db.PackagingOptions.AddRange(
                new PackagingOption { ProductDefinitionId = p2.Id, SizeLabel = "1L", Capacity = 1, EmptyContainerCost = 51 },
                new PackagingOption { ProductDefinitionId = p2.Id, SizeLabel = "4L", Capacity = 4, EmptyContainerCost = 121 }
            );

            // --- P3: TURPENTINE ---
            var p3 = new ProductDefinition { ProductName = "TURPENTINE THINNER", UnitOfMeasure = "KG", SpecificGravity = 0.80m, OverheadPercentage = 10 };
            db.Products.Add(p3); db.SaveChanges();

            db.ProductIngredients.AddRange(
                new ProductIngredient { ProductDefinitionId = p3.Id, RawMaterialId = GetRm("Kerosine (Turp)"), StandardQty = 156 },
                new ProductIngredient { ProductDefinitionId = p3.Id, RawMaterialId = GetRm("Acetone"), StandardQty = 3 }
            );

            db.PackagingOptions.AddRange(
                new PackagingOption { ProductDefinitionId = p3.Id, SizeLabel = "1L", Capacity = 1, EmptyContainerCost = 25.20m },
                new PackagingOption { ProductDefinitionId = p3.Id, SizeLabel = "5L", Capacity = 5, EmptyContainerCost = 61.44m },
                new PackagingOption { ProductDefinitionId = p3.Id, SizeLabel = "0.5L", Capacity = 0.5m, EmptyContainerCost = 14.00m }
            );

            // --- P4: ROCK ROUGH 2MM ---
            var p4 = new ProductDefinition { ProductName = "ROCK ROUGH 2MM", UnitOfMeasure = "KG", SpecificGravity = 1.0m, OverheadPercentage = 10 };
            db.Products.Add(p4); db.SaveChanges();

            db.ProductIngredients.AddRange(
                new ProductIngredient { ProductDefinitionId = p4.Id, RawMaterialId = GetRm("Water"), StandardQty = 44 },
                new ProductIngredient { ProductDefinitionId = p4.Id, RawMaterialId = GetRm("Dispersant-WD Eagle"), StandardQty = 4.40m },
                new ProductIngredient { ProductDefinitionId = p4.Id, RawMaterialId = GetRm("HEC Thickener"), StandardQty = 0.70m },
                new ProductIngredient { ProductDefinitionId = p4.Id, RawMaterialId = GetRm("White Spirit"), StandardQty = 4.90m },
                new ProductIngredient { ProductDefinitionId = p4.Id, RawMaterialId = GetRm("Melwit-34"), StandardQty = 71.80m },
                new ProductIngredient { ProductDefinitionId = p4.Id, RawMaterialId = GetRm("Styrene Acrylic"), StandardQty = 124 },
                new ProductIngredient { ProductDefinitionId = p4.Id, RawMaterialId = GetRm("Melgrit RT-A (30/80)"), StandardQty = 663.90m },
                new ProductIngredient { ProductDefinitionId = p4.Id, RawMaterialId = GetRm("Melgrit RT-D (3mm)"), StandardQty = 96 }
            );

            db.PackagingOptions.AddRange(
                new PackagingOption { ProductDefinitionId = p4.Id, SizeLabel = "30KG", Capacity = 30, EmptyContainerCost = 289 },
                new PackagingOption { ProductDefinitionId = p4.Id, SizeLabel = "5KG", Capacity = 5, EmptyContainerCost = 0 }
            );

            // --- P5: ROCK ROUGH 3MM ---
            var p5 = new ProductDefinition { ProductName = "ROCK ROUGH 3MM", UnitOfMeasure = "KG", SpecificGravity = 1.0m, OverheadPercentage = 10 };
            db.Products.Add(p5); db.SaveChanges();

            db.ProductIngredients.AddRange(
                new ProductIngredient { ProductDefinitionId = p5.Id, RawMaterialId = GetRm("Water"), StandardQty = 44 },
                new ProductIngredient { ProductDefinitionId = p5.Id, RawMaterialId = GetRm("Melgrit RT-A (30/80)"), StandardQty = 663.90m },
                new ProductIngredient { ProductDefinitionId = p5.Id, RawMaterialId = GetRm("Melgrit RT-D (3mm)"), StandardQty = 96 },
                new ProductIngredient { ProductDefinitionId = p5.Id, RawMaterialId = GetRm("Work Off"), StandardQty = 45 }
            );

            db.PackagingOptions.AddRange(
                new PackagingOption { ProductDefinitionId = p5.Id, SizeLabel = "30KG", Capacity = 30, EmptyContainerCost = 289 },
                new PackagingOption { ProductDefinitionId = p5.Id, SizeLabel = "5KG", Capacity = 5, EmptyContainerCost = 0 }
            );

            // --- P6: PLASTIC EMULSION ---
            var p6 = new ProductDefinition { ProductName = "PLASTIC EMULSION WHITE", UnitOfMeasure = "KG", SpecificGravity = 1.50m, OverheadPercentage = 10 };
            db.Products.Add(p6); db.SaveChanges();

            db.ProductIngredients.AddRange(
                new ProductIngredient { ProductDefinitionId = p6.Id, RawMaterialId = GetRm("Water"), StandardQty = 474.70m },
                new ProductIngredient { ProductDefinitionId = p6.Id, RawMaterialId = GetRm("Calgon"), StandardQty = 2.90m },
                new ProductIngredient { ProductDefinitionId = p6.Id, RawMaterialId = GetRm("Sodium Benzoate"), StandardQty = 2.90m },
                new ProductIngredient { ProductDefinitionId = p6.Id, RawMaterialId = GetRm("Dispex N40"), StandardQty = 1.30m },
                new ProductIngredient { ProductDefinitionId = p6.Id, RawMaterialId = GetRm("Bermocol(Tylose)"), StandardQty = 5.30m },
                new ProductIngredient { ProductDefinitionId = p6.Id, RawMaterialId = GetRm("Ammonia Sol"), StandardQty = 1.40m },
                new ProductIngredient { ProductDefinitionId = p6.Id, RawMaterialId = GetRm("Antifoam"), StandardQty = 2.90m },
                new ProductIngredient { ProductDefinitionId = p6.Id, RawMaterialId = GetRm("Melwit-34"), StandardQty = 639.30m },
                new ProductIngredient { ProductDefinitionId = p6.Id, RawMaterialId = GetRm("Titanium"), StandardQty = 2.90m },
                new ProductIngredient { ProductDefinitionId = p6.Id, RawMaterialId = GetRm("NPG"), StandardQty = 0.27m },
                new ProductIngredient { ProductDefinitionId = p6.Id, RawMaterialId = GetRm("Mergal K6N"), StandardQty = 2.90m },
                new ProductIngredient { ProductDefinitionId = p6.Id, RawMaterialId = GetRm("Styrene Acrylic"), StandardQty = 29.70m },
                new ProductIngredient { ProductDefinitionId = p6.Id, RawMaterialId = GetRm("MPG"), StandardQty = 0.27m },
                new ProductIngredient { ProductDefinitionId = p6.Id, RawMaterialId = GetRm("Pine Oil"), StandardQty = 0.07m }
            );

            db.PackagingOptions.AddRange(
                new PackagingOption { ProductDefinitionId = p6.Id, SizeLabel = "1L", Capacity = 1, EmptyContainerCost = 30 },
                new PackagingOption { ProductDefinitionId = p6.Id, SizeLabel = "4L", Capacity = 4, EmptyContainerCost = 93 },
                new PackagingOption { ProductDefinitionId = p6.Id, SizeLabel = "20L", Capacity = 20, EmptyContainerCost = 270 },
                new PackagingOption { ProductDefinitionId = p6.Id, SizeLabel = "10L", Capacity = 10, EmptyContainerCost = 170 }
            );

            Console.WriteLine("--- SEEDING COMPLETE SUCCESSFULLY ---");

        }
        catch (Exception ex)
        {
            Console.WriteLine("------------------------------------------------");
            Console.WriteLine($"SEEDING ERROR: {ex.Message}");
            Console.WriteLine("------------------------------------------------");
        }

        if (!db.Users.Any()) db.Users.Add(new User { Username = "admin", Password = "password" });
        db.SaveChanges();
    }
}
app.UseCors("AllowAll");
app.UseDefaultFiles(); app.UseStaticFiles();
app.MapControllers();
app.Run();