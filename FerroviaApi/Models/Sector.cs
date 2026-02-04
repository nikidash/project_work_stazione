using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FerroviaApi.Models
{
    public class Sector
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int Length { get; set; }
        public bool IsAvailable { get; set; }
        public bool IsStop { get; set; }
        [Range(1, 4)]
        public int? PlatformNumber { get; set; }

        public ICollection<Switch> InputSwitches { get; set; } = new List<Switch>();
        public ICollection<Switch> StraightSwitches { get; set; } = new List<Switch>();
        public ICollection<Switch> DeviationSwitches { get; set; } = new List<Switch>();
        public ICollection<Maintenance> MaintenancePlatforms { get; set; } = new List<Maintenance>();
    }
}