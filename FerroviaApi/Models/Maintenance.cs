using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FerroviaApi.Models
{
    public class Maintenance
    {
        [Key]
        public int Id { get; set; }

        [Column("train_d")] // Mappatura esplicita per il campo SQL
        [Required]
        public int TrainId { get; set; }

        [Required]
        public DateTime MaintenanceStart { get; set; }

        [Required]
        public DateTime MaintenanceEnd { get; set; }

        public int? MaintenancePlatform { get; set; } // FK a sectors(id)

        // Navigation Properties
        [ForeignKey(nameof(TrainId))]
        public TrainsLog? TrainLog { get; set; }

        [ForeignKey(nameof(MaintenancePlatform))]
        public Sector? MaintenanceSector { get; set; }
    }
}