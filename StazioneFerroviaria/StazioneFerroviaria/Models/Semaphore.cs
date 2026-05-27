using System.ComponentModel.DataAnnotations;

namespace FerroviaApi.Models
{
    public class Semaphore
    {
        [Key]
        public int Id { get; set; }
        public bool IsWorking { get; set; }
        public bool IsGreen { get; set; }
        public bool IsDeviating { get; set; }

        public ICollection<Switch> Switches { get; set; } = new List<Switch>();
    }
}