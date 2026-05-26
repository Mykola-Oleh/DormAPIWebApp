using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DormAPIWebApp.Models;

namespace DormAPIWebApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoomsController : ControllerBase
    {
        private readonly DormContext _context;

        public RoomsController(DormContext context)
        {
            _context = context;
        }

        // GET: api/Rooms/dorm/1/available
        [HttpGet("dorm/{dormId}/available")]
        public async Task<ActionResult<IEnumerable<Room>>> GetAvailableRooms(int dormId)
        {
            var rooms = await _context.Rooms
                .Where(r => r.DormId == dormId)
                .Include(r => r.CheckIns)
                .ToListAsync();

            var availableRooms = rooms
                .Where(r => r.CheckIns.Count(c => c.CheckOutDate == null) < r.Capacity)
                .ToList();

            return Ok(availableRooms);
        }

        // GET: api/Rooms
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Room>>> GetRooms()
        {
            return await _context.Rooms.ToListAsync();
        }

        // POST: api/Rooms
        [HttpPost]
        public async Task<ActionResult<Room>> PostRoom(Room room)
        {
            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetRooms), new { id = room.Id }, room);
        }
    }
}