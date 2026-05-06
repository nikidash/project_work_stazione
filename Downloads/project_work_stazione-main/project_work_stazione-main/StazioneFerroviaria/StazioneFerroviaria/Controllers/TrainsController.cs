using FerroviaApi.Data;
using FerroviaApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FerroviaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TrainsController : ControllerBase
    {
        private readonly FerroviaApiDbContext _context;
        public TrainsController(FerroviaApiDbContext context) => _context = context;

        // GET: api/Trains
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Train>>> GetTrains() =>
            await _context.Trains.ToListAsync();

        // GET: api/Trains/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Train>> GetTrain(int id)
        {
            var train = await _context.Trains.FindAsync(id);
            if (train == null) return NotFound();
            return train;
        }

        // POST: api/Trains
        [HttpPost]
        public async Task<ActionResult<Train>> PostTrain(Train train)
        {
            _context.Trains.Add(train);
            try { await _context.SaveChangesAsync(); }
            catch (DbUpdateException)
            {
                // La validazione di CHECK e FK � cruciale qui (Length, Origin, Destination, Times)
                return Conflict(new { message = "Conflitto: Treno non creato. Verifica i vincoli: lunghezza, origine/destinazione, orari (ScheduledDeparture > Arrival) e piattaforma assegnata." });
            }
            return CreatedAtAction(nameof(GetTrain), new { id = train.Id }, train);
        }

        // PUT: api/Trains/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTrain(int id, Train train)
        {
            if (id != train.Id) return BadRequest();
            _context.Entry(train).State = EntityState.Modified;
            try { await _context.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Trains.Any(e => e.Id == id)) return NotFound();
                throw;
            }
            catch (DbUpdateException)
            {
                return Conflict(new { message = "Conflitto: Aggiornamento Treno fallito. Verifica i vincoli." });
            }
            return NoContent();
        }

        // DELETE: api/Trains/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTrain(int id)
        {
            var train = await _context.Trains.FindAsync(id);
            if (train == null) return NotFound();

            _context.Trains.Remove(train);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}