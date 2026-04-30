namespace Hotel.Api.Configurations;

public class StorageSettings
{
    public string Provider { get; set; } = "R2";
    public string UploadEndpoint { get; set; } = string.Empty;
    public string PublicBaseUrl { get; set; } = string.Empty;
    public string Bucket { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public long MaxUploadBytes { get; set; } = 5 * 1024 * 1024;
    public string LocalRootPath { get; set; } = "wwwroot/uploads";
    public int MaxImageWidth { get; set; } = 1920;
    public int WebpQuality { get; set; } = 82;
}
