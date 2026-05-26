using System.Text.Json.Serialization;

namespace DormAPIWebApp.Models
{
    public class Student
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string TicketNumber { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string Faculty { get; set; } = string.Empty;
        public string ContactInfo { get; set; } = string.Empty;

        [JsonIgnore]
        public ICollection<CheckIn> CheckIns { get; set; } = new List<CheckIn>();
        [JsonIgnore]
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
