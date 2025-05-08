using Microsoft.AspNetCore.Mvc;
using TravelAgencyApi.Services;
using TravelAgencyApi.DTOs;

namespace TravelAgencyApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TripsController : ControllerBase
    {
        private readonly IDatabaseService _dbService;

        public TripsController(IDatabaseService dbService)
        {
            _dbService = dbService;
        }
        // Endpoint pobierający wszystkie dostępne wycieczki (podstawowe info + kraj)
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<TripResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetTrips()
        {
            try
            {
                var trips = await _dbService.GetTripsAsync();
                return Ok(trips);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetTrips: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, "Wystąpił błąd serwera podczas pobierania wycieczek.");
            }
        }
    }
}