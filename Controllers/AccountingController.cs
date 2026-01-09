using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductionApi.Data;
using ProductionApi.Models;

namespace ProductionApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountingController : ControllerBase
    {
        private readonly AppDbContext _context;
        public AccountingController(AppDbContext context) { _context = context; }

        [HttpPost("expense")]
        public async Task<IActionResult> AddExpense([FromBody] CreateExpenseDto dto)
        {
            var exp = new Expense { Description = dto.Description, Amount = dto.Amount, Category = dto.Category, Date = DateTime.Now };
            _context.Expenses.Add(exp);
            await _context.SaveChangesAsync();
            return Ok(exp);
        }

        [HttpGet("expenses")]
        public async Task<IActionResult> GetExpenses()
        {
            return Ok(await _context.Expenses.OrderByDescending(e => e.Date).ToListAsync());
        }
    }
}