using FerroviaApi.Data;
using FerroviaApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FerroviaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SwitchesController : ControllerBase
    {
        private readonly FerroviaApiDbContext _context;
        public SwitchesController(FerroviaApiDbContext context) => _context = context;

        // GET: api/Switches (Include le relazioni FK)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Switch>>> GetSwitches()
        {
            return await _context.Switches
                .Include(s => s.InputSector)
                .Include(s => s.StraightSector)
                .Include(s => s.DeviationSector)
                .Include(s => s.Semaphore)
                .ToListAsync();
        }

        // GET: api/Switches/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Switch>> GetSwitch(int id)
        {
            var @switch = await _context.Switches
                .Include(s => s.InputSector)
                .Include(s => s.StraightSector)
                .Include(s => s.DeviationSector)
                .Include(s => s.Semaphore)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (@switch == null) return NotFound();
            return @switch;
        }

        // POST: api/Switches
        [HttpPost]
        public async Task<ActionResult<Switch>> PostSwitch(Switch @switch)
        {
            _context.Switches.Add(@switch);
            try { await _context.SaveChangesAsync(); }
            catch (DbUpdateException)
            {
                // Cattura violazione FK se InputId, StraightId, DeviationId o SemaphoreId non esistono
                return Conflict(new { message = "Conflitto: Switch non creato. Assicurati che InputId, StraightId, DeviationId e SemaphoreId esistano (Foreign Key error)." });
            }
            return CreatedAtAction(nameof(GetSwitch), new { id = @switch.Id }, @switch);
        }

        // PUT: api/Switches/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutSwitch(int id, Switch @switch)
        {
            if (id != @switch.Id) return BadRequest();
            _context.Entry(@switch).State = EntityState.Modified;
            try { await _context.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Switches.Any(e => e.Id == id)) return NotFound();
                throw;
            }
            catch (DbUpdateException)
            {
                return Conflict(new { message = "Conflitto: Aggiornamento Switch fallito. Assicurati che tutti gli ID FK esistano." });
            }
            return NoContent();
        }

        // DELETE: api/Switches/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSwitch(int id)
        {
            var @switch = await _context.Switches.FindAsync(id);
            if (@switch == null) return NotFound();

            _context.Switches.Remove(@switch);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}