using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DormAPIWebApp.Models;

namespace DormAPIWebApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CheckInsController : ControllerBase
    {
        private readonly DormContext _context;

        public CheckInsController(DormContext context)
        {
            _context = context;
        }

        // GET: api/CheckIns
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CheckIn>>> GetCheckIns()
        {
            return await _context.CheckIns.ToListAsync();
        }

        // POST: api/CheckIns
        [HttpPost]
        public async Task<ActionResult<CheckIn>> PostCheckIn(CheckIn checkIn)
        {
            _context.CheckIns.Add(checkIn);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetCheckIns), new { id = checkIn.Id }, checkIn);
        }
    }
}