using FerroviaApi.Data;
using FerroviaApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FerroviaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SectorsController : ControllerBase
    {
        private readonly FerroviaApiDbContext _context;
        public SectorsController(FerroviaApiDbContext context) => _context = context;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Sector>>> GetSectors()
        {
            return await _context.Sectors.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Sector>> GetSector(int id)
        {
            var sector = await _context.Sectors.FindAsync(id);
            if (sector == null) return NotFound();
            return sector;
        }

        [HttpPost]
        public async Task<ActionResult<Sector>> PostSector(Sector sector)
        {
            _context.Sectors.Add(sector);
            try { await _context.SaveChangesAsync(); }
            catch (DbUpdateException)
            {
                return Conflict(new { message = "Conflitto: Sector non creato (es. PlatformNumber duplicato)." });
            }
            return CreatedAtAction(nameof(GetSector), new { id = sector.Id }, sector);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutSector(int id, Sector sector)
        {
            if (id != sector.Id) return BadRequest();
            _context.Entry(sector).State = EntityState.Modified;
            try { await _context.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Sectors.Any(e => e.Id == id)) return NotFound();
                throw;
            }
            catch (DbUpdateException)
            {
                return Conflict(new { message = "Conflitto: Aggiornamento Sector fallito (es. PlatformNumber duplicato)." });
            }
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSector(int id)
        {
            var sector = await _context.Sectors.FindAsync(id);
            if (sector == null) return NotFound();
            try
            {
                _context.Sectors.Remove(sector);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Conflict(new { message = "Impossibile eliminare: Sector referenziato da Switch o Maintenance." });
            }
            return NoContent();
        }
    }
}