namespace Configuration.Core.Entity.Dto
{
    public class ConfigurationRecordRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = "string";
        public string Value { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
