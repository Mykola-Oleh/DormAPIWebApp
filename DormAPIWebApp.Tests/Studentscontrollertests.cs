using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DormAPIWebApp.Controllers;
using DormAPIWebApp.Models;
using Xunit;

namespace DormAPIWebApp.Tests
{
    public class StudentsControllerTests : IDisposable
    {
        private readonly DormContext _context;
        private readonly StudentsController _controller;

        public StudentsControllerTests()
        {
            // Кожен тест отримує свою ізольовану InMemory базу
            var options = new DbContextOptionsBuilder<DormContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new DormContext(options);
            _controller = new StudentsController(_context);
        }

        public void Dispose() => _context.Dispose();

        // ─── Seed helper ──────────────────────────────────────────
        private async Task SeedStudentsAsync()
        {
            _context.Students.AddRange(
                new Student { Id = 1, FullName = "Шевченко Тарас", TicketNumber = "КН-001", Faculty = "ФІОТ", ContactInfo = "+380001" },
                new Student { Id = 2, FullName = "Франко Іван", TicketNumber = "КН-002", Faculty = "ФМФ", ContactInfo = "+380002" },
                new Student { Id = 3, FullName = "Леся Українка", TicketNumber = "КН-003", Faculty = "ФГФ", ContactInfo = "+380003" }
            );
            await _context.SaveChangesAsync();
        }

        // ═══════════════════════════════════════════════════════════
        // GET /api/Students  — повертає всіх студентів
        // ═══════════════════════════════════════════════════════════

        [Fact]
        public async Task GetStudents_ReturnsAllStudents_WhenNoDormFilter()
        {
            // Arrange
            await SeedStudentsAsync();

            // Act
            var result = await _controller.GetStudents(dormId: null);

            // Assert
            var ok = Assert.IsType<ActionResult<IEnumerable<Student>>>(result);
            var list = Assert.IsAssignableFrom<IEnumerable<Student>>(ok.Value);
            Assert.Equal(3, list.Count());
        }

        [Fact]
        public async Task GetStudents_ReturnsEmpty_WhenNoStudentsExist()
        {
            // Act
            var result = await _controller.GetStudents(dormId: null);

            // Assert
            var ok = Assert.IsType<ActionResult<IEnumerable<Student>>>(result);
            var list = Assert.IsAssignableFrom<IEnumerable<Student>>(ok.Value);
            Assert.Empty(list);
        }

        // ═══════════════════════════════════════════════════════════
        // GET /api/Students/{id}
        // ═══════════════════════════════════════════════════════════

        [Fact]
        public async Task GetStudent_ReturnsStudent_WhenExists()
        {
            // Arrange
            await SeedStudentsAsync();

            // Act
            var result = await _controller.GetStudent(1);

            // Assert
            var ok = Assert.IsType<ActionResult<Student>>(result);
            var student = Assert.IsType<Student>(ok.Value);
            Assert.Equal("Шевченко Тарас", student.FullName);
        }

        [Fact]
        public async Task GetStudent_ReturnsNotFound_WhenMissing()
        {
            // Act
            var result = await _controller.GetStudent(999);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        // ═══════════════════════════════════════════════════════════
        // POST /api/Students
        // ═══════════════════════════════════════════════════════════

        [Fact]
        public async Task PostStudent_CreatesStudent_AndReturns201()
        {
            // Arrange
            var newStudent = new Student
            {
                FullName = "Коцюбинський Михайло",
                TicketNumber = "КН-004",
                Faculty = "ФІОТ",
                ContactInfo = "+380004"
            };

            // Act
            var result = await _controller.PostStudent(newStudent);

            // Assert
            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var student = Assert.IsType<Student>(created.Value);
            Assert.Equal("Коцюбинський Михайло", student.FullName);
            Assert.Equal(1, await _context.Students.CountAsync());
        }

        [Fact]
        public async Task PostStudent_PersistsToDatabase()
        {
            var newStudent = new Student { FullName = "Тест", TicketNumber = "T-001", Faculty = "ФМФ", ContactInfo = "test" };

            await _controller.PostStudent(newStudent);

            var saved = await _context.Students.FirstOrDefaultAsync(s => s.TicketNumber == "T-001");
            Assert.NotNull(saved);
            Assert.Equal("Тест", saved!.FullName);
        }


        [Fact]
        public async Task PutStudent_ReturnsBadRequest_WhenIdMismatch()
        {
            var student = new Student { Id = 5, FullName = "X", TicketNumber = "X", Faculty = "X", ContactInfo = "X" };

            var result = await _controller.PutStudent(99, student);

            Assert.IsType<BadRequestResult>(result);
        }

        // DELETE /api/Students/{id}

        [Fact]
        public async Task DeleteStudent_RemovesStudent_ReturnsNoContent()
        {
            await SeedStudentsAsync();

            var result = await _controller.DeleteStudent(2);

            Assert.IsType<NoContentResult>(result);
            Assert.Equal(2, await _context.Students.CountAsync());
            Assert.Null(await _context.Students.FindAsync(2));
        }

        [Fact]
        public async Task DeleteStudent_ReturnsNotFound_WhenMissing()
        {
            var result = await _controller.DeleteStudent(999);

            Assert.IsType<NotFoundResult>(result);
        }
    }
}