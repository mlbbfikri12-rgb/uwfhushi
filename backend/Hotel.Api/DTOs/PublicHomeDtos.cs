namespace Hotel.Api.DTOs;

public class PublicHomeResponseDto
{
    public IReadOnlyCollection<HeroBannerResponseDto> HeroBanners { get; set; } = Array.Empty<HeroBannerResponseDto>();
    public IReadOnlyCollection<PublicHotelListItemDto> PopularHotels { get; set; } = Array.Empty<PublicHotelListItemDto>();
    public IReadOnlyCollection<PublicDestinationDto> Destinations { get; set; } = Array.Empty<PublicDestinationDto>();
    public IReadOnlyCollection<PublicBlogDto> Blogs { get; set; } = Array.Empty<PublicBlogDto>();
}

public class PublicDestinationDto
{
    public string City { get; set; } = string.Empty;
    public decimal MinPrice { get; set; }
}

public class PublicBlogDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
