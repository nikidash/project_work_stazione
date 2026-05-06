using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FerroviaApi.Models
{
    [Table("switches")]
    public class Switch
    {
        [Key]
        public int Id { get; set; }

        // Foreign Keys
        [Required] public int InputId { get; set; }
        [Required] public int StraightId { get; set; }
        [Required] public int DeviationId { get; set; }
        [Required] public int SemaphoreId { get; set; }

        public bool IsWorking { get; set; }
        public bool IsDeviating { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(InputId))] public Sector? InputSector { get; set; }
        [ForeignKey(nameof(StraightId))] public Sector? StraightSector { get; set; }
        [ForeignKey(nameof(DeviationId))] public Sector? DeviationSector { get; set; }
        [ForeignKey(nameof(SemaphoreId))] public Semaphore? Semaphore { get; set; }
    }
}