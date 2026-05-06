using System.ComponentModel.DataAnnotations;

namespace FerroviaApi.Models
{
    public class Train
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(10)]
        public string? TrainNumber { get; set; }

        [Required]
        [Range(6, 16)] // CHECK (length BETWEEN 6 AND 16)
        public int Length { get; set; }

        [Required, MaxLength(2)] // 'L1', 'R1', 'L2', 'R2'
        public string Origin { get; set; } = string.Empty;

        [Required, MaxLength(1)] // 'L', 'R'
        public string Destination { get; set; } = string.Empty;

        [Required, MaxLength(1)] // 'R', 'V', 'F', 'T', 'S'
        public string TrainType { get; set; } = string.Empty;

        [Required]
        public DateTime ScheduledArrivalTime { get; set; }

        [Required]
        public DateTime ScheduledDepartureTime { get; set; } // CHECK(scheduledDepartureTime > scheduledArrivalTime)

        [Required]
        public bool IsTransit { get; set; }

        public int? AssignedPlatform { get; set; }
    }
}