using System.Text.Json.Serialization;

namespace DormAPIWebApp.Models
{
    public class Dorm
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public int Floors { get; set; }
        public string Manager { get; set; } = string.Empty;

        [JsonIgnore]
        public ICollection<Room> Rooms { get; set; } = new List<Room>();
    }
}
