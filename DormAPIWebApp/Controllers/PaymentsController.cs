using DormAPIWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DormAPIWebApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        private readonly DormContext _context;

        public PaymentsController(DormContext context)
        {
            _context = context;
        }

        // GET: api/Payments/student/1/total
        [HttpGet("student/{studentId}/total")]
        public async Task<ActionResult<decimal>> GetTotalPayments(int studentId)
        {
            var total = await _context.Payments
                .Where(p => p.StudentId == studentId && p.Status == "Сплачено")
                .SumAsync(p => p.Amount);
            return Ok(total);
        }

        // GET: api/Payments
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Payment>>> GetPayments()
        {
            return await _context.Payments.ToListAsync();
        }

        // POST: api/Payments
        [HttpPost]
        public async Task<ActionResult<Payment>> PostPayment(Payment payment)
        {
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetPayments), new { id = payment.Id }, payment);
        }
    }
}