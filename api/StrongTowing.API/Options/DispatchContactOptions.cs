namespace StrongTowing.API.Options;

public class DispatchContactOptions
{
    public const string SectionName = "DispatchContact";

    public string DisplayName { get; set; } = "Dispatch";
    public string? Phone { get; set; }
}
