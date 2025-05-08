using System.ComponentModel.DataAnnotations;

namespace TravelAgencyApi.DTOs
{
    public class ClientCreateRequestDto
    {
        [Required(ErrorMessage = "Imię jest wymagane.")]
        [MaxLength(120, ErrorMessage = "Imię nie może być dłuższe niż 120 znaków.")]
        public string FirstName { get; set; } = null!;

        [Required(ErrorMessage = "Nazwisko jest wymagane.")]
        [MaxLength(120, ErrorMessage = "Nazwisko nie może być dłuższe niż 120 znaków.")]
        public string LastName { get; set; } = null!;

        [Required(ErrorMessage = "Adres email jest wymagany.")]
        [EmailAddress(ErrorMessage = "Niepoprawny format adresu email.")]
        [MaxLength(120, ErrorMessage = "Adres email nie może być dłuższy niż 120 znaków.")]
        public string Email { get; set; } = null!;

        [MaxLength(120, ErrorMessage = "Numer telefonu nie może być dłuższy niż 120 znaków.")]
        public string? Telephone { get; set; }

        [Required(ErrorMessage = "PESEL jest wymagany.")]
        [MaxLength(120, ErrorMessage = "PESEL nie może być dłuższy niż 120 znaków.")]
        [RegularExpression(@"^\d{11}$", ErrorMessage = "PESEL musi składać się z 11 cyfr.")]
        public string Pesel { get; set; } = null!;
    }
}
