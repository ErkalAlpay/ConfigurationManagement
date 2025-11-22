namespace Configuration.Core.Entity.Dto
{
    public class ConfigurationRecordUpdateRequest
    {
        public string Value { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}