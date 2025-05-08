using Microsoft.Data.SqlClient;
// using System.Data.SqlClient;
using TravelAgencyApi.DTOs;
using TravelAgencyApi.Models;
using System.Data;

namespace TravelAgencyApi.Services
{
    public class DatabaseService : IDatabaseService
    {
        private readonly IConfiguration _configuration;

        public DatabaseService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private SqlConnection GetConnection()
        {
            return new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        }

        // Pobiera wszystkie wycieczki wraz z krajami
        public async Task<IEnumerable<TripResponseDto>> GetTripsAsync()
        {
            var trips = new Dictionary<int, TripResponseDto>();

            using (var connection = GetConnection())
            {
                await connection.OpenAsync();
                
                var commandText = @"
                    SELECT T.IdTrip, T.Name AS TripName, T.Description, T.DateFrom, T.DateTo, T.MaxPeople,C.Name AS CountryName
                    FROM Trip T
                    LEFT JOIN Country_Trip CT ON T.IdTrip = CT.IdTrip
                    LEFT JOIN Country C ON CT.IdCountry = C.IdCountry
                    ORDER BY T.DateFrom DESC, T.IdTrip, C.Name;";
                
                using (var command = new SqlCommand(commandText, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var tripId = reader.GetInt32(reader.GetOrdinal("IdTrip"));
                            if (!trips.TryGetValue(tripId, out var tripDto))
                            {
                                tripDto = new TripResponseDto
                                {
                                    IdTrip = tripId,
                                    Name = reader.GetString(reader.GetOrdinal("TripName")),
                                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                                    DateFrom = reader.GetDateTime(reader.GetOrdinal("DateFrom")),
                                    DateTo = reader.GetDateTime(reader.GetOrdinal("DateTo")),
                                    MaxPeople = reader.GetInt32(reader.GetOrdinal("MaxPeople")),
                                    Countries = new List<CountryDto>()
                                };
                                trips.Add(tripId, tripDto);
                            }

                            if (!reader.IsDBNull(reader.GetOrdinal("CountryName")))
                            {
                                ((List<CountryDto>)tripDto.Countries).Add(new CountryDto { Name = reader.GetString(reader.GetOrdinal("CountryName")) });
                            }
                        }
                    }
                }
            }
            return trips.Values;
        }
        
        // Pobiera klienta po ID (używane wewnętrznie)
        public async Task<Client?> GetClientByIdAsync(int idClient)
        {
            Client? client = null;
            using (var connection = GetConnection())
            {
                await connection.OpenAsync();
                
                var commandText = "SELECT IdClient, FirstName, LastName, Email, Telephone, Pesel FROM Client WHERE IdClient = @IdClient";
                using (var command = new SqlCommand(commandText, connection))
                {
                    command.Parameters.AddWithValue("@IdClient", idClient);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            client = new Client
                            {
                                IdClient = reader.GetInt32(0),
                                FirstName = reader.GetString(1),
                                LastName = reader.GetString(2),
                                Email = reader.GetString(3),
                                Telephone = reader.IsDBNull(4) ? null : reader.GetString(4),
                                Pesel = reader.IsDBNull(5) ? null : reader.GetString(5)
                            };
                        }
                    }
                }
            }
            return client;
        }

        // Pobiera wycieczki, na które zapisany jest dany klient
        public async Task<IEnumerable<ClientTripResponseDto>> GetClientTripsAsync(int idClient)
        {
            var clientTrips = new List<ClientTripResponseDto>();
            using (var connection = GetConnection())
            {
                await connection.OpenAsync();
                
                var commandText = @"
                    SELECT T.IdTrip, T.Name, T.Description, T.DateFrom, T.DateTo, T.MaxPeople, CTrip.RegisteredAt, CTrip.PaymentDate
                    FROM Client_Trip CTrip
                    JOIN Trip T ON CTrip.IdTrip = T.IdTrip
                    WHERE CTrip.IdClient = @IdClient
                    ORDER BY T.DateFrom DESC;";
                using (var command = new SqlCommand(commandText, connection))
                {
                    command.Parameters.AddWithValue("@IdClient", idClient);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            clientTrips.Add(new ClientTripResponseDto
                            {
                                IdTrip = reader.GetInt32(reader.GetOrdinal("IdTrip")),
                                Name = reader.GetString(reader.GetOrdinal("Name")),
                                Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                                DateFrom = reader.GetDateTime(reader.GetOrdinal("DateFrom")),
                                DateTo = reader.GetDateTime(reader.GetOrdinal("DateTo")),
                                MaxPeople = reader.GetInt32(reader.GetOrdinal("MaxPeople")),
                                RegisteredAt = reader.GetInt32(reader.GetOrdinal("RegisteredAt")),
                                PaymentDate = reader.IsDBNull(reader.GetOrdinal("PaymentDate")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("PaymentDate"))
                            });
                        }
                    }
                }
            }
            return clientTrips;
        }

        // Sprawdza, czy klient o danym ID istnieje
        public async Task<bool> ClientExistsAsync(int idClient)
        {
            using (var connection = GetConnection())
            {
                await connection.OpenAsync();
                
                var commandText = "SELECT 1 FROM Client WHERE IdClient = @IdClient";
                using (var command = new SqlCommand(commandText, connection))
                {
                    command.Parameters.AddWithValue("@IdClient", idClient);
                    return await command.ExecuteScalarAsync() != null;
                }
            }
        }

        // Sprawdza, czy wycieczka o danym ID istnieje
        public async Task<bool> TripExistsAsync(int idTrip)
        {
            using (var connection = GetConnection())
            {
                await connection.OpenAsync();
                
                var commandText = "SELECT 1 FROM Trip WHERE IdTrip = @IdTrip";
                using (var command = new SqlCommand(commandText, connection))
                {
                    command.Parameters.AddWithValue("@IdTrip", idTrip);
                    return await command.ExecuteScalarAsync() != null;
                }
            }
        }
        
        // Pobiera szczegóły wycieczki (po ID)
        public async Task<Trip?> GetTripByIdAsync(int idTrip)
        {
            Trip? trip = null;
            using (var connection = GetConnection())
            {
                await connection.OpenAsync();
                
                var commandText = "SELECT IdTrip, Name, Description, DateFrom, DateTo, MaxPeople FROM Trip WHERE IdTrip = @IdTrip";
                using (var command = new SqlCommand(commandText, connection))
                {
                    command.Parameters.AddWithValue("@IdTrip", idTrip);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            trip = new Trip
                            {
                                IdTrip = reader.GetInt32(0),
                                Name = reader.GetString(1),
                                Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                                DateFrom = reader.GetDateTime(3),
                                DateTo = reader.GetDateTime(4),
                                MaxPeople = reader.GetInt32(5)
                            };
                        }
                    }
                }
            }
            return trip;
        }

        // Sprawdza, czy klient jest już zapisany na daną wycieczkę
        public async Task<bool> IsClientRegisteredForTripAsync(int idClient, int idTrip)
        {
            using (var connection = GetConnection())
            {
                await connection.OpenAsync();

                var commandText = "SELECT 1 FROM Client_Trip WHERE IdClient = @IdClient AND IdTrip = @IdTrip";
                using (var command = new SqlCommand(commandText, connection))
                {
                    command.Parameters.AddWithValue("@IdClient", idClient);
                    command.Parameters.AddWithValue("@IdTrip", idTrip);
                    return await command.ExecuteScalarAsync() != null;
                }
            }
        }

        // Pobiera aktualną liczbę uczestników danej wycieczki
        public async Task<int> GetCurrentTripParticipantCountAsync(int idTrip)
        {
            using (var connection = GetConnection())
            {
                await connection.OpenAsync();

                var commandText = "SELECT COUNT(*) FROM Client_Trip WHERE IdTrip = @IdTrip";
                using (var command = new SqlCommand(commandText, connection))
                {
                    command.Parameters.AddWithValue("@IdTrip", idTrip);
                    var result = await command.ExecuteScalarAsync();
                    return result != null ? (int)result : 0;
                }
            }
        }

        // Dodaje nowego klienta do bazy danych
        public async Task<int> AddClientAsync(ClientCreateRequestDto clientDto)
        {
            using (var connection = GetConnection())
            {
                await connection.OpenAsync();

                var commandText = @"
                    INSERT INTO Client (FirstName, LastName, Email, Telephone, Pesel) 
                    OUTPUT INSERTED.IdClient 
                    VALUES (@FirstName, @LastName, @Email, @Telephone, @Pesel);";
                using (var command = new SqlCommand(commandText, connection))
                {
                    command.Parameters.AddWithValue("@FirstName", clientDto.FirstName);
                    command.Parameters.AddWithValue("@LastName", clientDto.LastName);
                    command.Parameters.AddWithValue("@Email", clientDto.Email);
                    command.Parameters.AddWithValue("@Telephone", (object)clientDto.Telephone ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Pesel", clientDto.Pesel);
                    
                    var newClientId = (int?)await command.ExecuteScalarAsync();
                    if (newClientId == null)
                    {
                        throw new Exception("Nie udało się utworzyć klienta.");
                    }
                    return newClientId.Value;
                }
            }
        }

        // Przypisuje klienta do wycieczki
        public async Task<bool> AssignClientToTripAsync(int idClient, int idTrip)
        {
            using (var connection = GetConnection())
            {
                await connection.OpenAsync();

                var commandText = @"
                    INSERT INTO Client_Trip (IdClient, IdTrip, RegisteredAt) 
                    VALUES (@IdClient, @IdTrip, @RegisteredAt);";
                using (var command = new SqlCommand(commandText, connection))
                {
                    // Data rejestracji w formacie YYYYMMDD
                    int registeredAt = int.Parse(DateTime.Now.ToString("yyyyMMdd"));

                    command.Parameters.AddWithValue("@IdClient", idClient);
                    command.Parameters.AddWithValue("@IdTrip", idTrip);
                    command.Parameters.AddWithValue("@RegisteredAt", registeredAt);
                    
                    return await command.ExecuteNonQueryAsync() > 0;
                }
            }
        }

        // Usuwa przypisanie klienta do wycieczki
        public async Task<bool> DeleteClientTripAssignmentAsync(int idClient, int idTrip)
        {
            using (var connection = GetConnection())
            {
                await connection.OpenAsync();

                var commandText = "DELETE FROM Client_Trip WHERE IdClient = @IdClient AND IdTrip = @IdTrip;";
                using (var command = new SqlCommand(commandText, connection))
                {
                    command.Parameters.AddWithValue("@IdClient", idClient);
                    command.Parameters.AddWithValue("@IdTrip", idTrip);
                    return await command.ExecuteNonQueryAsync() > 0;
                }
            }
        }

        // Sprawdza, czy klient o danym numerze PESEL już istnieje
        public async Task<bool> ClientWithPeselExistsAsync(string pesel)
        {
            using (var connection = GetConnection())
            {
                await connection.OpenAsync();
                
                var commandText = "SELECT 1 FROM Client WHERE Pesel = @Pesel";
                using (var command = new SqlCommand(commandText, connection))
                {
                    command.Parameters.AddWithValue("@Pesel", pesel);
                    return await command.ExecuteScalarAsync() != null;
                }
            }
        }

        // Sprawdza, czy klient o danym adresie email już istnieje
        public async Task<bool> ClientWithEmailExistsAsync(string email)
        {
            using (var connection = GetConnection())
            {
                await connection.OpenAsync();
                
                var commandText = "SELECT 1 FROM Client WHERE Email = @Email";
                using (var command = new SqlCommand(commandText, connection))
                {
                    command.Parameters.AddWithValue("@Email", email);
                    return await command.ExecuteScalarAsync() != null;
                }
            }
        }
    }
}
