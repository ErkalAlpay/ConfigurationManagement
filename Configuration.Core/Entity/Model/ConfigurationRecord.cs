namespace Configuration.Core.Entity
{
    public class ConfigurationRecord
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = "string";
        public string Value { get; set; } = string.Empty;
        public int IsActive { get; set; }
        public string ApplicationName { get; set; } = string.Empty;
    }
}
