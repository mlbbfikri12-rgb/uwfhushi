public class EmailVerificationToken
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiredAt { get; set; }
}