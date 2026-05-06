using FerroviaApi.Data;
using FerroviaApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// CORREZIONE: Alias per risolvere l'ambiguità con System.Threading.Semaphore
using SemaphoreModel = FerroviaApi.Models.Semaphore;

namespace FerroviaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SemaphoresController : ControllerBase
    {
        private readonly FerroviaApiDbContext _context;
        public SemaphoresController(FerroviaApiDbContext context) => _context = context;

        // GET: api/Semaphores
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SemaphoreModel>>> GetSemaphores() =>
            await _context.Semaphores.ToListAsync();

        // GET: api/Semaphores/5
        [HttpGet("{id}")]
        public async Task<ActionResult<SemaphoreModel>> GetSemaphore(int id)
        {
            var semaphore = await _context.Semaphores.FindAsync(id);
            if (semaphore == null) return NotFound();
            return semaphore;
        }

        // POST: api/Semaphores
        [HttpPost]
        public async Task<ActionResult<SemaphoreModel>> PostSemaphore(SemaphoreModel semaphore)
        {
            _context.Semaphores.Add(semaphore);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetSemaphore), new { id = semaphore.Id }, semaphore);
        }

        // PUT: api/Semaphores/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutSemaphore(int id, SemaphoreModel semaphore)
        {
            if (id != semaphore.Id) return BadRequest();
            _context.Entry(semaphore).State = EntityState.Modified;
            try { await _context.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Semaphores.Any(e => e.Id == id)) return NotFound();
                throw;
            }
            return NoContent();
        }

        // DELETE: api/Semaphores/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSemaphore(int id)
        {
            var semaphore = await _context.Semaphores.FindAsync(id);
            if (semaphore == null) return NotFound();
            try
            {
                _context.Semaphores.Remove(semaphore);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Conflict(new { message = "Impossibile eliminare: Semaforo referenziato da Switch." });
            }
            return NoContent();
        }
    }
}