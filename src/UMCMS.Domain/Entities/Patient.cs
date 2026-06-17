namespace UMCMS.Domain.Entities
{
    public class Patient
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;

        /// <summary>"Internal" (university student/staff) or "External" (visitor).</summary>
        public string PatientType { get; set; } = "External";
        public string? RegistrationNumber { get; set; }
        public string? Faculty { get; set; }
        public string? Program { get; set; }
        public int? YearOfStudy { get; set; }

        /// <summary>Parent/guardian/next-of-kin — notified on emergencies.</summary>
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }

        public bool IsArchived { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string FullName => $"{FirstName} {LastName}".Trim();

        public int? Age
        {
            get
            {
                if (DateOfBirth is null) return null;
                var today = DateTime.Today;
                int age = today.Year - DateOfBirth.Value.Year;
                if (DateOfBirth.Value.Date > today.AddYears(-age)) age--;
                return age;
            }
        }
    }
}
