namespace Hotel.Api.Entities.Master;

public class Brand
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Hotel> Hotels { get; set; } = new List<Hotel>();
}
