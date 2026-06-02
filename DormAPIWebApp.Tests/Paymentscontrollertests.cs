using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DormAPIWebApp.Controllers;
using DormAPIWebApp.Models;
using Xunit;

namespace DormAPIWebApp.Tests
{
    public class PaymentsControllerTests : IDisposable
    {
        private readonly DormContext _context;
        private readonly PaymentsController _controller;

        public PaymentsControllerTests()
        {
            var options = new DbContextOptionsBuilder<DormContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new DormContext(options);
            _controller = new PaymentsController(_context);
        }

        public void Dispose() => _context.Dispose();

        // ─── Seed helpers ─────────────────────────────────────────
        private async Task SeedPaymentsAsync()
        {
            _context.Payments.AddRange(
                new Payment { Id = 1, StudentId = 1, Amount = 1200m, Status = "Сплачено", PaymentDate = DateTime.Today.AddDays(-10) },
                new Payment { Id = 2, StudentId = 1, Amount = 800m, Status = "Сплачено", PaymentDate = DateTime.Today.AddDays(-5) },
                new Payment { Id = 3, StudentId = 1, Amount = 500m, Status = "Очікується", PaymentDate = DateTime.Today },
                new Payment { Id = 4, StudentId = 2, Amount = 1500m, Status = "Сплачено", PaymentDate = DateTime.Today.AddDays(-3) },
                new Payment { Id = 5, StudentId = 2, Amount = 300m, Status = "Прострочено", PaymentDate = DateTime.Today.AddDays(-20) }
            );
            await _context.SaveChangesAsync();
        }

        // ═══════════════════════════════════════════════════════════
        // GET /api/Payments  — повертає всі платежі
        // ═══════════════════════════════════════════════════════════

        [Fact]
        public async Task GetPayments_ReturnsAllPayments()
        {
            // Arrange
            await SeedPaymentsAsync();

            // Act
            var result = await _controller.GetPayments();

            // Assert
            var ok = Assert.IsType<ActionResult<IEnumerable<Payment>>>(result);
            var list = Assert.IsAssignableFrom<IEnumerable<Payment>>(ok.Value);
            Assert.Equal(5, list.Count());
        }

        [Fact]
        public async Task GetPayments_ReturnsEmpty_WhenNoPayments()
        {
            // Act
            var result = await _controller.GetPayments();

            // Assert
            var ok = Assert.IsType<ActionResult<IEnumerable<Payment>>>(result);
            Assert.Empty(ok.Value!);
        }

        // ═══════════════════════════════════════════════════════════
        // GET /api/Payments/student/{studentId}/total
        // ═══════════════════════════════════════════════════════════

        [Fact]
        public async Task GetTotalPayments_ReturnsSumOfPaidOnly_ForStudent1()
        {
            // Arrange — студент 1 має два "Сплачено": 1200 + 800 = 2000
            await SeedPaymentsAsync();

            // Act
            var result = await _controller.GetTotalPayments(studentId: 1);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(2000m, (decimal)ok.Value!);
        }

        [Fact]
        public async Task GetTotalPayments_ReturnsSumOfPaidOnly_ForStudent2()
        {
            // Arrange — студент 2 має одне "Сплачено": 1500, решта ігноруються
            await SeedPaymentsAsync();

            // Act
            var result = await _controller.GetTotalPayments(studentId: 2);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(1500m, (decimal)ok.Value!);
        }

        [Fact]
        public async Task GetTotalPayments_ReturnsZero_WhenStudentHasNoPayments()
        {
            // Arrange — студент 99 не має жодного платежу
            await SeedPaymentsAsync();

            // Act
            var result = await _controller.GetTotalPayments(studentId: 99);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(0m, (decimal)ok.Value!);
        }

        [Fact]
        public async Task GetTotalPayments_DoesNotCount_PendingOrOverdue()
        {
            // Arrange — студент 1 має "Очікується" на 500, воно НЕ має рахуватись
            await SeedPaymentsAsync();

            // Act
            var result = await _controller.GetTotalPayments(studentId: 1);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var total = (decimal)ok.Value!;
            Assert.NotEqual(2500m, total); // якби рахувало Очікується — було б 2500
            Assert.Equal(2000m, total);
        }

        // ═══════════════════════════════════════════════════════════
        // POST /api/Payments
        // ═══════════════════════════════════════════════════════════

        [Fact]
        public async Task PostPayment_CreatesPayment_AndReturns201()
        {
            // Arrange
            var payment = new Payment
            {
                StudentId = 3,
                Amount = 950m,
                Status = "Сплачено",
                PaymentDate = DateTime.Today
            };

            // Act
            var result = await _controller.PostPayment(payment);

            // Assert
            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var saved = Assert.IsType<Payment>(created.Value);
            Assert.Equal(950m, saved.Amount);
            Assert.Equal(1, await _context.Payments.CountAsync());
        }

        [Fact]
        public async Task PostPayment_PersistsStatusCorrectly()
        {
            // Arrange
            var payment = new Payment { StudentId = 1, Amount = 100m, Status = "Прострочено", PaymentDate = DateTime.Today };

            // Act
            await _controller.PostPayment(payment);

            // Assert
            var inDb = await _context.Payments.FirstOrDefaultAsync();
            Assert.NotNull(inDb);
            Assert.Equal("Прострочено", inDb!.Status);
        }

        [Fact]
        public async Task PostPayment_AddsToExistingList()
        {
            // Arrange
            await SeedPaymentsAsync();
            var payment = new Payment { StudentId = 3, Amount = 400m, Status = "Сплачено", PaymentDate = DateTime.Today };

            // Act
            await _controller.PostPayment(payment);

            // Assert
            Assert.Equal(6, await _context.Payments.CountAsync());
        }
    }
}