using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DormAPIWebApp.Controllers;
using DormAPIWebApp.Models;
using Xunit;

namespace DormAPIWebApp.Tests
{
    public class RoomsControllerTests : IDisposable
    {
        private readonly DormContext _context;
        private readonly RoomsController _controller;

        public RoomsControllerTests()
        {
            var options = new DbContextOptionsBuilder<DormContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new DormContext(options);
            _controller = new RoomsController(_context);
        }

        public void Dispose() => _context.Dispose();

        private async Task SeedBaseDataAsync()
        {
            _context.Rooms.AddRange(
                new Room { Id = 1, RoomNumber = "101", Floor = 1, Capacity = 2, RoomType = "Звичайна", DormId = 1 },
                new Room { Id = 2, RoomNumber = "102", Floor = 1, Capacity = 3, RoomType = "Звичайна", DormId = 1 },
                new Room { Id = 3, RoomNumber = "201", Floor = 2, Capacity = 1, RoomType = "Люкс", DormId = 1 }
            );
            _context.Rooms.Add(
                new Room { Id = 4, RoomNumber = "101", Floor = 1, Capacity = 2, RoomType = "Звичайна", DormId = 2 }
            );
            await _context.SaveChangesAsync();
        }

        private async Task CheckInStudentAsync(int studentId, int roomId)
        {
            _context.CheckIns.Add(new CheckIn
            {
                StudentId = studentId,
                RoomId = roomId,
                CheckInDate = DateTime.Today.AddDays(-5),
                CheckOutDate = null,              
                ContractNumber = $"ДГ-{studentId}-{roomId}"
            });
            await _context.SaveChangesAsync();
        }

        private async Task CheckOutStudentAsync(int studentId, int roomId)
        {
            _context.CheckIns.Add(new CheckIn
            {
                StudentId = studentId,
                RoomId = roomId,
                CheckInDate = DateTime.Today.AddDays(-30),
                CheckOutDate = DateTime.Today.AddDays(-1),
                ContractNumber = $"ДГ-OUT-{studentId}-{roomId}"
            });
            await _context.SaveChangesAsync();
        }


        [Fact]
        public async Task GetRooms_ReturnsAllRooms()
        {
            await SeedBaseDataAsync();

            var result = await _controller.GetRooms();

            var ok = Assert.IsType<ActionResult<IEnumerable<Room>>>(result);
            var list = Assert.IsAssignableFrom<IEnumerable<Room>>(ok.Value);
            Assert.Equal(4, list.Count());
        }

        [Fact]
        public async Task GetRooms_ReturnsEmpty_WhenNoRooms()
        {
            var result = await _controller.GetRooms();

            var ok = Assert.IsType<ActionResult<IEnumerable<Room>>>(result);
            Assert.Empty(ok.Value!);
        }

        // GET /api/Rooms/dorm/{dormId}/available

        [Fact]
        public async Task GetAvailableRooms_ReturnsAllRooms_WhenNoneOccupied()
        {
            await SeedBaseDataAsync();

            var result = await _controller.GetAvailableRooms(dormId: 1);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var list = Assert.IsAssignableFrom<IEnumerable<Room>>(ok.Value);
            Assert.Equal(3, list.Count());
        }

        [Fact]
        public async Task GetAvailableRooms_ExcludesFullyOccupiedRoom()
        {
            await SeedBaseDataAsync();
            await CheckInStudentAsync(studentId: 1, roomId: 3);

            var result = await _controller.GetAvailableRooms(dormId: 1);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var list = Assert.IsAssignableFrom<IEnumerable<Room>>(ok.Value).ToList();
            Assert.Equal(2, list.Count);
            Assert.DoesNotContain(list, r => r.Id == 3);
        }

        [Fact]
        public async Task GetAvailableRooms_IncludesPartiallyOccupiedRoom()
        {
            await SeedBaseDataAsync();
            await CheckInStudentAsync(studentId: 1, roomId: 1);

            var result = await _controller.GetAvailableRooms(dormId: 1);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var list = Assert.IsAssignableFrom<IEnumerable<Room>>(ok.Value).ToList();
            Assert.Contains(list, r => r.Id == 1);
        }

        [Fact]
        public async Task GetAvailableRooms_IgnoresCheckedOutResidents()
        {
            await SeedBaseDataAsync();
            await CheckOutStudentAsync(studentId: 1, roomId: 3);

            var result = await _controller.GetAvailableRooms(dormId: 1);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var list = Assert.IsAssignableFrom<IEnumerable<Room>>(ok.Value).ToList();
            Assert.Equal(3, list.Count);
            Assert.Contains(list, r => r.Id == 3);
        }

        [Fact]
        public async Task GetAvailableRooms_ReturnsEmpty_WhenAllRoomsFull()
        {
            await SeedBaseDataAsync();
            await CheckInStudentAsync(1, roomId: 3);           
            await CheckInStudentAsync(2, roomId: 1);           
            await CheckInStudentAsync(3, roomId: 1);           
            await CheckInStudentAsync(4, roomId: 2);           
            await CheckInStudentAsync(5, roomId: 2);           
            await CheckInStudentAsync(6, roomId: 2);           

            
            var result = await _controller.GetAvailableRooms(dormId: 1);

            
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var list = Assert.IsAssignableFrom<IEnumerable<Room>>(ok.Value).ToList();
            Assert.Empty(list);
        }

        [Fact]
        public async Task GetAvailableRooms_OnlyReturnRoomsForRequestedDorm()
        {
            await SeedBaseDataAsync();

            var result = await _controller.GetAvailableRooms(dormId: 2);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var list = Assert.IsAssignableFrom<IEnumerable<Room>>(ok.Value).ToList();
            Assert.Single(list);
            Assert.All(list, r => Assert.Equal(2, r.DormId));
        }

        
        // POST /api/Rooms
        
        [Fact]
        public async Task PostRoom_CreatesRoom_AndReturns201()
        {
            var room = new Room
            {
                RoomNumber = "301",
                Floor = 3,
                Capacity = 2,
                RoomType = "Покращена",
                DormId = 1
            };

            var result = await _controller.PostRoom(room);

            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var saved = Assert.IsType<Room>(created.Value);
            Assert.Equal("301", saved.RoomNumber);
            Assert.Equal(1, await _context.Rooms.CountAsync());
        }

        [Fact]
        public async Task PostRoom_PersistsAllFields()
        {
            var room = new Room { RoomNumber = "401", Floor = 4, Capacity = 3, RoomType = "Люкс", DormId = 2 };

            await _controller.PostRoom(room);

            var inDb = await _context.Rooms.FirstOrDefaultAsync();
            Assert.NotNull(inDb);
            Assert.Equal("Люкс", inDb!.RoomType);
            Assert.Equal(3, inDb.Capacity);
            Assert.Equal(2, inDb.DormId);
        }
    }
}