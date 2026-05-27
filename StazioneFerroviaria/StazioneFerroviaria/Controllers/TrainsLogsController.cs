using FerroviaApi.Data;
using FerroviaApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FerroviaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TrainsLogsController : ControllerBase
    {
        private readonly FerroviaApiDbContext _context;
        public TrainsLogsController(FerroviaApiDbContext context) => _context = context;

        // GET: api/TrainsLogs
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TrainsLog>>> GetTrainsLogs() =>
            await _context.TrainsLogs.ToListAsync();

        // GET: api/TrainsLogs/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TrainsLog>> GetTrainsLog(int id)
        {
            var log = await _context.TrainsLogs.FindAsync(id);
            if (log == null) return NotFound();
            return log;
        }

        // POST: api/TrainsLogs
        [HttpPost]
        public async Task<ActionResult<TrainsLog>> PostTrainsLog(TrainsLog log)
        {
            _context.TrainsLogs.Add(log);
            try { await _context.SaveChangesAsync(); }
            catch (DbUpdateException)
            {
                return Conflict(new { message = "Conflitto: Log non creato. Verifica i vincoli." });
            }
            return CreatedAtAction(nameof(GetTrainsLog), new { id = log.Id }, log);
        }

        // PUT: api/TrainsLogs/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTrainsLog(int id, TrainsLog log)
        {
            if (id != log.Id) return BadRequest();
            _context.Entry(log).State = EntityState.Modified;
            try { await _context.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.TrainsLogs.Any(e => e.Id == id)) return NotFound();
                throw;
            }
            catch (DbUpdateException)
            {
                return Conflict(new { message = "Conflitto: Aggiornamento Log fallito. Verifica i vincoli." });
            }
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTrainsLog(int id)
        {
            var log = await _context.TrainsLogs.FindAsync(id);
            if (log == null) return NotFound();
            try
            {
                _context.TrainsLogs.Remove(log);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Cattura la violazione di Foreign Key (log usato da Maintenance)
                return Conflict(new { message = "Impossibile eliminare: Log referenziato da Maintenance." });
            }
            return NoContent();
        }
    }
}