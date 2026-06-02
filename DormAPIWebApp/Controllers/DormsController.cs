using DormAPIWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DormAPIWebApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DormsController : ControllerBase
    {
        private readonly DormContext _context;

        public DormsController(DormContext context)
        {
            _context = context;
        }

        // GET: api/Dorms
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Dorm>>> GetDorms()
        {
            return await _context.Dorms.ToListAsync();
        }

        // POST: api/Dorms
        [HttpPost]
        public async Task<ActionResult<Dorm>> PostDorm(Dorm dorm)
        {
            _context.Dorms.Add(dorm);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetDorms), new { id = dorm.Id }, dorm);
        }
    }
}