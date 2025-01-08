using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Data.Entities
{
    [Table("Client")]
    public class Client
    {
        [Key]
        [Required]
        public int Id { get; set; }
        public DateTime Created { get; set; }
        public Boolean IsActive {  get; set; }
        public Boolean IsRegistered { get; set; }
        public DateTime? DateRegistration { get; set; }
        public string Code { get; set; }
        public string Surname {  get; set; }
        public string Name { get; set; }
        public DateOnly DateOfBirth { get; set; }
        public string Citizenship { get; set; }
        public bool Sex {  get; set; }
        public string PassportNumber { get; set; }
        public string PassportIdNumber { get; set; }
        public string Street { get; set; }
        public string HouseNumber { get; set; }
        public string? AppartmentNumber { get; set; }
        public string ZipCode { get; set; }
        public string City { get; set; }
        public string PhoneNumberPrefix { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string? Description { get; set; }
    }
}
