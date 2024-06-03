using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using cwiczenie9.Data;
using cwiczenie9.Models;

namespace cwiczenie9.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TripController : ControllerBase
{
    private readonly ApbdContext _context;

    public TripController(ApbdContext context)
    {
        _context = context;
    }

    [HttpGet("trips")]
    public async Task<IActionResult> GetTrips([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        // var trips = _context.Trips.ToList();
        // var trips = await _context.Trips.ToListAsync(); // pobralo wszystko

        if (page < 1 || pageSize < 1)
        {
            return BadRequest("Page and pageSize parameters must be greater than 0");
        }

        var trips = await _context.Trips
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .OrderByDescending(t => t.DateFrom)
            .ToListAsync();

        return Ok(trips);
    }

    [HttpDelete("clients/{idClient}")]
    public async Task<IActionResult> DeleteClient(int idClient)
    {
        var client = await _context.Clients
            .Include(c => c.ClientTrips)
            .FirstOrDefaultAsync(c => c.IdClient == idClient);

        if (client == null)
        {
            return NotFound($"Client with ID {idClient} not found.");
        }

        if (client.ClientTrips.Any())
        {
            return BadRequest("Cannot delete client with existing trips.");
        }

        _context.Clients.Remove(client);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("trips/{idTrip}/clients")]
    public async Task<IActionResult> AssignClientToTrip(int idTrip, [FromBody] Client client)
    {
        var trip = await _context.Trips.FindAsync(idTrip);
        if (trip == null || trip.DateFrom <= DateTime.Now)
        {
            return BadRequest("The trip does not exist or has already taken place.");
        }

        var existingClient = await _context.Clients.FirstOrDefaultAsync(c => c.Pesel == client.Pesel);
        if (existingClient != null)
        {
            return BadRequest("A client with this PESEL number already exists.");
        }

        var isClientAlreadyRegistered =
            await _context.ClientTrips.AnyAsync(ct => ct.IdClient == client.IdClient && ct.IdTrip == idTrip);
        if (isClientAlreadyRegistered)
        {
            return BadRequest("The client is already registered for this trip.");
        }

        var clientTrip = new ClientTrip
        {
            IdClient = client.IdClient,
            IdTrip = idTrip,
            RegisteredAt = DateTime.Now,
            PaymentDate = null
        };

        _context.ClientTrips.Add(clientTrip);
        await _context.SaveChangesAsync();

        return Ok(clientTrip);
    }
}
