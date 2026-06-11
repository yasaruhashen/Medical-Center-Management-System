namespace Project_.src.Models
{
    public class Staff : User
    {
        public string JobTitle { get; set; } = string.Empty;
        public string Shift { get; set; } = string.Empty;
    }
}
