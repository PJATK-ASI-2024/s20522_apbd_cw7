using Microsoft.AspNetCore.Mvc;
using TravelAgencyApi.Services;
using TravelAgencyApi.DTOs;
using TravelAgencyApi.Models;

namespace TravelAgencyApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClientsController : ControllerBase
    {
        private readonly IDatabaseService _dbService;

        public ClientsController(IDatabaseService dbService)
        {
            _dbService = dbService;
        }
        
        [HttpGet("{idClient}/trips")]
        [ProducesResponseType(typeof(IEnumerable<ClientTripResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetClientTrips(int idClient)
        {
            try
            {
                //czy klient istnieje
                if (!await _dbService.ClientExistsAsync(idClient))
                {
                    return NotFound($"Klient o ID {idClient} nie został znaleziony.");
                }

                var clientTrips = await _dbService.GetClientTripsAsync(idClient);
                return Ok(clientTrips);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetClientTrips for client {idClient}: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, "Wystąpił błąd serwera podczas pobierania wycieczek klienta.");
            }
        }
        
        [HttpPost]
        [ProducesResponseType(typeof(Client), StatusCodes.Status201Created)] // Zwraca nowo utworzonego klienta lub jego ID
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateClient([FromBody] ClientCreateRequestDto clientDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // Walidacja: Sprawdzenie, czy klient o danym PESELu już istnieje
                if (await _dbService.ClientWithPeselExistsAsync(clientDto.Pesel))
                {
                    return Conflict($"Klient o numerze PESEL '{clientDto.Pesel}' już istnieje.");
                }

                // Walidacja: Sprawdzenie, czy klient o danym emailu już istnieje
                if (await _dbService.ClientWithEmailExistsAsync(clientDto.Email))
                {
                    return Conflict($"Klient o adresie email '{clientDto.Email}' już istnieje.");
                }
                
                var newClientId = await _dbService.AddClientAsync(clientDto);
                var newClient = await _dbService.GetClientByIdAsync(newClientId); // Pobierz pełne dane nowego klienta

                // Zwrócenie 201 Created z lokalizacją nowego zasobu i danymi klienta
                return CreatedAtAction(nameof(GetClientTrips), new { idClient = newClientId }, newClient);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CreateClient: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, "Wystąpił błąd serwera podczas tworzenia klienta.");
            }
        }
        
        [HttpPut("{idClient}/trips/{idTrip}")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RegisterClientForTrip(int idClient, int idTrip)
        {
            try
            {
                // czy klient istnieje
                if (!await _dbService.ClientExistsAsync(idClient))
                {
                    return NotFound($"Klient o ID {idClient} nie został znaleziony.");
                }

                //czy wycieczka istnieje
                var trip = await _dbService.GetTripByIdAsync(idTrip);
                if (trip == null)
                {
                    return NotFound($"Wycieczka o ID {idTrip} nie została znaleziona.");
                }

                // czy klient jest już zapisany na tę wycieczkę
                if (await _dbService.IsClientRegisteredForTripAsync(idClient, idTrip))
                {
                    return Conflict($"Klient o ID {idClient} jest już zapisany na wycieczkę o ID {idTrip}.");
                }

                //czy nie została osiągnięta maksymalna liczba uczestników
                var currentParticipants = await _dbService.GetCurrentTripParticipantCountAsync(idTrip);
                if (currentParticipants >= trip.MaxPeople)
                {
                    return Conflict($"Wycieczka o ID {idTrip} osiągnęła maksymalną liczbę uczestników ({trip.MaxPeople}).");
                }

                // rejestracja klienta na wycieczkę
                var success = await _dbService.AssignClientToTripAsync(idClient, idTrip);
                if (success) {
                    return StatusCode(StatusCodes.Status201Created, $"Klient o ID {idClient} został pomyślnie zarejestrowany na wycieczkę o ID {idTrip}.");
                }else {
                    return StatusCode(StatusCodes.Status500InternalServerError, "Nie udało się zarejestrować klienta na wycieczkę z nieznanego powodu.");
                }
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in RegisterClientForTrip for client {idClient}, trip {idTrip}: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, "Wystąpił błąd serwera podczas rejestracji klienta na wycieczkę.");
            }
        }
        
        // usunięcie klienta z wycieczki.
        [HttpDelete("{idClient}/trips/{idTrip}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UnregisterClientFromTrip(int idClient, int idTrip)
        {
            try
            {
                //czy klient istnieje (opcjonalne, ale dobre dla spójności)
                if (!await _dbService.ClientExistsAsync(idClient))
                {
                    return NotFound($"Klient o ID {idClient} nie został znaleziony.");
                }

                //czy wycieczka istnieje (opcjonalne)
                if (!await _dbService.TripExistsAsync(idTrip))
                {
                    return NotFound($"Wycieczka o ID {idTrip} nie została znaleziona.");
                }

                //czy rejestracja istnieje
                if (!await _dbService.IsClientRegisteredForTripAsync(idClient, idTrip))
                {
                    return NotFound($"Klient o ID {idClient} nie jest zarejestrowany na wycieczkę o ID {idTrip}, więc nie można usunąć rejestracji.");
                }

                // Usunięcie rejestracji
                var success = await _dbService.DeleteClientTripAssignmentAsync(idClient, idTrip);
                if (success)
                {
                    return NoContent();
                }
                else
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, "Nie udało się usunąć rejestracji klienta z wycieczki.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UnregisterClientFromTrip for client {idClient}, trip {idTrip}: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, "Wystąpił błąd serwera podczas usuwania rejestracji klienta z wycieczki.");
            }
        }
    }
}
