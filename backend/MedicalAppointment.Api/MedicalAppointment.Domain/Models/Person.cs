using System;

namespace MedicalAppointment.Domain.Models
{
    public class Person
    {
        public Guid PersonId { get; set; }              // Independent key
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string NormalizedName { get; set; } = string.Empty;
        public string PersonalIdentifier { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public DateTime BirthDate { get; set; }
    }
}
