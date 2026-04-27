namespace Hotel.Api.Entities.Master;

public class CustomerGlobal
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;

    public string? PasswordHash { get; set; }

    public bool IsVerified { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}