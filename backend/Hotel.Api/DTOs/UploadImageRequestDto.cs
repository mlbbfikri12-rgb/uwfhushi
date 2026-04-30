namespace Hotel.Api.DTOs;

public class UploadImageRequestDto
{
    public IFormFile File { get; set; } = default!;
    public string Folder { get; set; } = "uploads";
}
