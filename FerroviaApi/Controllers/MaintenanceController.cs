using FerroviaApi.Data;
using FerroviaApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FerroviaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MaintenanceController : ControllerBase
    {
        private readonly FerroviaApiDbContext _context;
        public MaintenanceController(FerroviaApiDbContext context) => _context = context;

        // GET: api/Maintenance
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Maintenance>>> GetMaintenanceRecords() =>
            await _context.Maintenance
                .Include(m => m.TrainLog) // Include il log del treno correlato
                .Include(m => m.MaintenanceSector) // Include il settore di manutenzione
                .ToListAsync();

        // GET: api/Maintenance/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Maintenance>> GetMaintenance(int id)
        {
            var record = await _context.Maintenance
                .Include(m => m.TrainLog)
                .Include(m => m.MaintenanceSector)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (record == null) return NotFound();
            return record;
        }

        // POST: api/Maintenance
        [HttpPost]
        public async Task<ActionResult<Maintenance>> PostMaintenance(Maintenance record)
        {
            _context.Maintenance.Add(record);
            try { await _context.SaveChangesAsync(); }
            catch (DbUpdateException)
            {
                // Cattura violazione FK (TrainId non esiste in TrainsLog o MaintenancePlatform non esiste in Sectors)
                return Conflict(new { message = "Conflitto: Record di manutenzione non creato. Assicurati che TrainId esista in TrainsLog e MaintenancePlatform esista in Sectors." });
            }
            return CreatedAtAction(nameof(GetMaintenance), new { id = record.Id }, record);
        }

        // ... Implementare i metodi PUT e DELETE ...

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMaintenance(int id)
        {
            var record = await _context.Maintenance.FindAsync(id);
            if (record == null) return NotFound();

            _context.Maintenance.Remove(record);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}