namespace UMCMS.Domain
{
    /// <summary>A single entry in the role-scoped navigation rail.</summary>
    public class NavItem
    {
        public string Key { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string Icon { get; init; } = string.Empty;

        public NavItem() { }
        public NavItem(string key, string title, string icon)
        {
            Key = key; Title = title; Icon = icon;
        }
    }
}
