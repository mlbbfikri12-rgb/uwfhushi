namespace Hotel.Api.Entities.Master;

public class City
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<Hotel> Hotels { get; set; } = new List<Hotel>();
}
