namespace TravelAgencyApi.DTOs
{
    public class TripResponseDto
    {
        public int IdTrip { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public int MaxPeople { get; set; }
        public IEnumerable<CountryDto> Countries { get; set; } = new List<CountryDto>();
    }
}