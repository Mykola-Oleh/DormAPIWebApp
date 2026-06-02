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

        // ─── Seed helpers ─────────────────────────────────────────

        private async Task SeedBaseDataAsync()
        {
            // Гуртожиток 1 — 3 кімнати
            _context.Rooms.AddRange(
                new Room { Id = 1, RoomNumber = "101", Floor = 1, Capacity = 2, RoomType = "Звичайна", DormId = 1 },
                new Room { Id = 2, RoomNumber = "102", Floor = 1, Capacity = 3, RoomType = "Звичайна", DormId = 1 },
                new Room { Id = 3, RoomNumber = "201", Floor = 2, Capacity = 1, RoomType = "Люкс", DormId = 1 }
            );
            // Гуртожиток 2 — 1 кімната
            _context.Rooms.Add(
                new Room { Id = 4, RoomNumber = "101", Floor = 1, Capacity = 2, RoomType = "Звичайна", DormId = 2 }
            );
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Додає активне заселення (без checkOutDate) для вказаної кімнати.
        /// </summary>
        private async Task CheckInStudentAsync(int studentId, int roomId)
        {
            _context.CheckIns.Add(new CheckIn
            {
                StudentId = studentId,
                RoomId = roomId,
                CheckInDate = DateTime.Today.AddDays(-5),
                CheckOutDate = null,              // активне
                ContractNumber = $"ДГ-{studentId}-{roomId}"
            });
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Додає завершене заселення (з checkOutDate) — НЕ має впливати на місткість.
        /// </summary>
        private async Task CheckOutStudentAsync(int studentId, int roomId)
        {
            _context.CheckIns.Add(new CheckIn
            {
                StudentId = studentId,
                RoomId = roomId,
                CheckInDate = DateTime.Today.AddDays(-30),
                CheckOutDate = DateTime.Today.AddDays(-1), // завершене
                ContractNumber = $"ДГ-OUT-{studentId}-{roomId}"
            });
            await _context.SaveChangesAsync();
        }

        // ═══════════════════════════════════════════════════════════
        // GET /api/Rooms — повертає всі кімнати
        // ═══════════════════════════════════════════════════════════

        [Fact]
        public async Task GetRooms_ReturnsAllRooms()
        {
            // Arrange
            await SeedBaseDataAsync();

            // Act
            var result = await _controller.GetRooms();

            // Assert
            var ok = Assert.IsType<ActionResult<IEnumerable<Room>>>(result);
            var list = Assert.IsAssignableFrom<IEnumerable<Room>>(ok.Value);
            Assert.Equal(4, list.Count());
        }

        [Fact]
        public async Task GetRooms_ReturnsEmpty_WhenNoRooms()
        {
            // Act
            var result = await _controller.GetRooms();

            // Assert
            var ok = Assert.IsType<ActionResult<IEnumerable<Room>>>(result);
            Assert.Empty(ok.Value!);
        }

        // ═══════════════════════════════════════════════════════════
        // GET /api/Rooms/dorm/{dormId}/available
        // ═══════════════════════════════════════════════════════════

        [Fact]
        public async Task GetAvailableRooms_ReturnsAllRooms_WhenNoneOccupied()
        {
            // Arrange — 3 кімнати в гуртожитку 1, жодного заселення
            await SeedBaseDataAsync();

            // Act
            var result = await _controller.GetAvailableRooms(dormId: 1);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var list = Assert.IsAssignableFrom<IEnumerable<Room>>(ok.Value);
            Assert.Equal(3, list.Count());
        }

        [Fact]
        public async Task GetAvailableRooms_ExcludesFullyOccupiedRoom()
        {
            // Arrange
            // Кімната 3 (Люкс, capacity=1): заселяємо 1 студента → повна
            await SeedBaseDataAsync();
            await CheckInStudentAsync(studentId: 1, roomId: 3);

            // Act
            var result = await _controller.GetAvailableRooms(dormId: 1);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var list = Assert.IsAssignableFrom<IEnumerable<Room>>(ok.Value).ToList();
            Assert.Equal(2, list.Count);
            Assert.DoesNotContain(list, r => r.Id == 3);
        }

        [Fact]
        public async Task GetAvailableRooms_IncludesPartiallyOccupiedRoom()
        {
            // Arrange
            // Кімната 1 (capacity=2): заселяємо 1 з 2 — ще є місце
            await SeedBaseDataAsync();
            await CheckInStudentAsync(studentId: 1, roomId: 1);

            // Act
            var result = await _controller.GetAvailableRooms(dormId: 1);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var list = Assert.IsAssignableFrom<IEnumerable<Room>>(ok.Value).ToList();
            Assert.Contains(list, r => r.Id == 1);
        }

        [Fact]
        public async Task GetAvailableRooms_IgnoresCheckedOutResidents()
        {
            // Arrange
            // Кімната 3 (capacity=1): є завершене заселення → вважається вільною
            await SeedBaseDataAsync();
            await CheckOutStudentAsync(studentId: 1, roomId: 3);

            // Act
            var result = await _controller.GetAvailableRooms(dormId: 1);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var list = Assert.IsAssignableFrom<IEnumerable<Room>>(ok.Value).ToList();
            Assert.Equal(3, list.Count);
            Assert.Contains(list, r => r.Id == 3);
        }

        [Fact]
        public async Task GetAvailableRooms_ReturnsEmpty_WhenAllRoomsFull()
        {
            // Arrange
            // Кімната 3 (capacity=1) + кімната 1 (capacity=2) + кімната 2 (capacity=3)
            await SeedBaseDataAsync();
            await CheckInStudentAsync(1, roomId: 3);           // кімн.3 повна (1/1)
            await CheckInStudentAsync(2, roomId: 1);           // кімн.1: 1/2
            await CheckInStudentAsync(3, roomId: 1);           // кімн.1: 2/2 — повна
            await CheckInStudentAsync(4, roomId: 2);           // кімн.2: 1/3
            await CheckInStudentAsync(5, roomId: 2);           // кімн.2: 2/3
            await CheckInStudentAsync(6, roomId: 2);           // кімн.2: 3/3 — повна

            // Act
            var result = await _controller.GetAvailableRooms(dormId: 1);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var list = Assert.IsAssignableFrom<IEnumerable<Room>>(ok.Value).ToList();
            Assert.Empty(list);
        }

        [Fact]
        public async Task GetAvailableRooms_OnlyReturnRoomsForRequestedDorm()
        {
            // Arrange — дані для двох гуртожитків, запитуємо лише dorm 2
            await SeedBaseDataAsync();

            // Act
            var result = await _controller.GetAvailableRooms(dormId: 2);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var list = Assert.IsAssignableFrom<IEnumerable<Room>>(ok.Value).ToList();
            Assert.Single(list);
            Assert.All(list, r => Assert.Equal(2, r.DormId));
        }

        // ═══════════════════════════════════════════════════════════
        // POST /api/Rooms
        // ═══════════════════════════════════════════════════════════

        [Fact]
        public async Task PostRoom_CreatesRoom_AndReturns201()
        {
            // Arrange
            var room = new Room
            {
                RoomNumber = "301",
                Floor = 3,
                Capacity = 2,
                RoomType = "Покращена",
                DormId = 1
            };

            // Act
            var result = await _controller.PostRoom(room);

            // Assert
            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var saved = Assert.IsType<Room>(created.Value);
            Assert.Equal("301", saved.RoomNumber);
            Assert.Equal(1, await _context.Rooms.CountAsync());
        }

        [Fact]
        public async Task PostRoom_PersistsAllFields()
        {
            // Arrange
            var room = new Room { RoomNumber = "401", Floor = 4, Capacity = 3, RoomType = "Люкс", DormId = 2 };

            // Act
            await _controller.PostRoom(room);

            // Assert
            var inDb = await _context.Rooms.FirstOrDefaultAsync();
            Assert.NotNull(inDb);
            Assert.Equal("Люкс", inDb!.RoomType);
            Assert.Equal(3, inDb.Capacity);
            Assert.Equal(2, inDb.DormId);
        }
    }
}