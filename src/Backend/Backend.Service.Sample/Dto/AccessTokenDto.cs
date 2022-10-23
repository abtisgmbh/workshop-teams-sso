namespace Backend.Service.Sample.Dto;

public class AccessTokenDto
{
    public DateTimeOffset ExpiresOn { get; set; }
    public string? IdToken { get; set; }
    public string? TenantId { get; set; }
    public string? AccessToken { get; set; }
}