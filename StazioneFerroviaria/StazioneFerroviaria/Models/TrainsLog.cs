using System.ComponentModel.DataAnnotations;

namespace FerroviaApi.Models
{
    public class TrainsLog
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(10)] public string? TrainNumber { get; set; }
        [Required, Range(6, 16)] public int Length { get; set; }
        [Required, MaxLength(2)] public string Origin { get; set; } = string.Empty;
        [Required, MaxLength(1)] public string Destination { get; set; } = string.Empty;
        [Required, MaxLength(1)] public string TrainType { get; set; } = string.Empty;
        [Required] public DateTime ScheduledArrivalTime { get; set; }
        [Required] public DateTime ScheduledDepartureTime { get; set; }
        [Required] public bool IsTransit { get; set; }

        // Dati Log:
        [Required] public DateTime ActualDepartureTime { get; set; }
        [Required] public DateTime ActualArrivalTime { get; set; }

        public int? AssignedPlatform { get; set; }

        public ICollection<Maintenance> MaintenanceRecords { get; set; } = new List<Maintenance>();
    }
}