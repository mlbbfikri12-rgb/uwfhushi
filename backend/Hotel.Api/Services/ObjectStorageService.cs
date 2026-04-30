using Hotel.Api.Configurations;
using Hotel.Api.DTOs;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Hotel.Api.Services;

public interface IObjectStorageService
{
    Task<UploadImageResponseDto> UploadImageAsync(IFormFile file, string folder, CancellationToken cancellationToken = default);
}

public class ObjectStorageService : IObjectStorageService
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/avif"
    };

    private readonly HttpClient _httpClient;
    private readonly StorageSettings _settings;

    public ObjectStorageService(HttpClient httpClient, IOptions<StorageSettings> settings)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
    }

    public async Task<UploadImageResponseDto> UploadImageAsync(IFormFile file, string folder, CancellationToken cancellationToken = default)
    {
        if (file.Length == 0)
            throw new Exception("File is required");

        if (file.Length > _settings.MaxUploadBytes)
            throw new Exception("File exceeds maximum upload size");

        if (!AllowedContentTypes.Contains(file.ContentType))
            throw new Exception("Only jpeg, png, webp, and avif images are allowed");

        if (string.IsNullOrWhiteSpace(_settings.UploadEndpoint))
            throw new Exception("Storage upload endpoint is not configured");

        var safeFolder = string.IsNullOrWhiteSpace(folder) ? "uploads" : folder.Trim().ToLowerInvariant();
        var objectKey = $"{safeFolder}/{Guid.NewGuid():N}.webp";
        var processedBytes = await ProcessImageAsync(file, cancellationToken);

        if (_settings.Provider.Equals("Local", StringComparison.OrdinalIgnoreCase))
            return await SaveLocalAsync(processedBytes, objectKey, cancellationToken);

        using var form = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(processedBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/webp");

        form.Add(fileContent, "file", objectKey);
        form.Add(new StringContent(_settings.Bucket), "bucket");
        form.Add(new StringContent(objectKey), "key");

        using var request = new HttpRequestMessage(HttpMethod.Post, _settings.UploadEndpoint)
        {
            Content = form
        };

        if (!string.IsNullOrWhiteSpace(_settings.AccessToken))
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _settings.AccessToken);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var publicBaseUrl = _settings.PublicBaseUrl.TrimEnd('/');
        return new UploadImageResponseDto
        {
            ObjectKey = objectKey,
            Url = string.IsNullOrWhiteSpace(publicBaseUrl) ? objectKey : $"{publicBaseUrl}/{objectKey}"
        };
    }

    private async Task<byte[]> ProcessImageAsync(IFormFile file, CancellationToken cancellationToken)
    {
        await using var input = file.OpenReadStream();
        using var image = await Image.LoadAsync(input, cancellationToken);

        if (image.Width > _settings.MaxImageWidth)
        {
            var height = (int)Math.Round(image.Height * (_settings.MaxImageWidth / (double)image.Width));
            image.Mutate(x => x.Resize(_settings.MaxImageWidth, height));
        }

        await using var output = new MemoryStream();
        await image.SaveAsWebpAsync(output, new WebpEncoder
        {
            Quality = _settings.WebpQuality
        }, cancellationToken);

        return output.ToArray();
    }

    private async Task<UploadImageResponseDto> SaveLocalAsync(byte[] bytes, string objectKey, CancellationToken cancellationToken)
    {
        var root = Path.GetFullPath(_settings.LocalRootPath);
        var fullPath = Path.GetFullPath(Path.Combine(root, objectKey.Replace('/', Path.DirectorySeparatorChar)));
        if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new Exception("Invalid upload path");

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllBytesAsync(fullPath, bytes, cancellationToken);

        var publicBaseUrl = string.IsNullOrWhiteSpace(_settings.PublicBaseUrl)
            ? "/uploads"
            : _settings.PublicBaseUrl.TrimEnd('/');

        return new UploadImageResponseDto
        {
            ObjectKey = objectKey,
            Url = $"{publicBaseUrl}/{objectKey}"
        };
    }
}
