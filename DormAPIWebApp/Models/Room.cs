using System.Text.Json.Serialization;

namespace DormAPIWebApp.Models
{
    public class Room
    {
        public int Id { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public int Floor { get; set; }
        public int Capacity { get; set; }
        public string RoomType { get; set; } = string.Empty;

        public int DormId { get; set; }
        public Dorm? Dorm { get; set; }

        [JsonIgnore]
        public ICollection<CheckIn> CheckIns { get; set; } = new List<CheckIn>();
    }
}
