namespace Hotel.Api.DTOs;

public class HeroBannerResponseDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string LinkUrl { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public class UpsertHeroBannerDto
{
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;   // ✅ tambah
    public string ImageUrl { get; set; } = string.Empty;
    public string LinkUrl { get; set; } = string.Empty;    // ✅ tambah
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
