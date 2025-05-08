using TravelAgencyApi.DTOs;
using TravelAgencyApi.Models;

namespace TravelAgencyApi.Services
{
    public interface IDatabaseService
    {
        Task<IEnumerable<TripResponseDto>> GetTripsAsync();
        Task<Client?> GetClientByIdAsync(int idClient);
        Task<IEnumerable<ClientTripResponseDto>> GetClientTripsAsync(int idClient);
        Task<bool> ClientExistsAsync(int idClient);
        Task<bool> TripExistsAsync(int idTrip);
        Task<Trip?> GetTripByIdAsync(int idTrip);
        Task<bool> IsClientRegisteredForTripAsync(int idClient, int idTrip);
        Task<int> GetCurrentTripParticipantCountAsync(int idTrip);
        Task<int> AddClientAsync(ClientCreateRequestDto clientDto);
        Task<bool> AssignClientToTripAsync(int idClient, int idTrip);
        Task<bool> DeleteClientTripAssignmentAsync(int idClient, int idTrip);
        Task<bool> ClientWithPeselExistsAsync(string pesel);
        Task<bool> ClientWithEmailExistsAsync(string email);
    }
}