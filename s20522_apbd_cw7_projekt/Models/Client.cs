namespace TravelAgencyApi.Models
{
    public class Client
    {
        public int IdClient { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Telephone { get; set; }
        public string? Pesel { get; set; }
    }
}
