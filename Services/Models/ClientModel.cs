namespace Services.Models
{
    public class ClientModel
    {
        public int Id { get; set; }
        public DateTime Created { get; set; }
        public Boolean IsActive { get; set; }
        public Boolean IsRegistered { get; set; }
        public DateTime? DateRegistration { get; set; }
        public string? Email { get; set; } = string.Empty;
        public DateOnly RegistrationDocumentStartDate { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DateOnly? DateOfBirth { get; set; }
        public string? Citizenship { get; set; } = string.Empty;
        public bool? Sex { get; set; }
        public string? PassportNumber { get; set; } = string.Empty;
        public string? PassportIdNumber { get; set; } = string.Empty;
        public string? Street { get; set; } = string.Empty;
        public string? HouseNumber { get; set; } = string.Empty;
        public string? ApartmentNumber { get; set; }
        public string? ZipCode { get; set; } = string.Empty;
        public string? City { get; set; } = string.Empty;
        public string? PhoneNumberPrefix { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Result { get; set; } = string.Empty;
    }
}
