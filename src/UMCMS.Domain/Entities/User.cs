namespace UMCMS.Domain.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;          // Admin, Doctor, Receptionist

        /// <summary>For doctors: "General" or "Dental". Null for other roles.</summary>
        public string? Specialization { get; set; }

        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        public string FullName => $"{FirstName} {LastName}".Trim();
        public bool IsGeneralDoctor => Role == "Doctor" && Specialization == "General";
        public bool IsDentist => Role == "Doctor" && Specialization == "Dental";
    }
}
