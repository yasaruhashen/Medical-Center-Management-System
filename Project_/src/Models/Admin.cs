namespace Project_.src.Models
{
    public class Admin : User
    {
        public string Department { get; set; } = string.Empty;
        public string AdminLevel { get; set; } = string.Empty;
    }
}
