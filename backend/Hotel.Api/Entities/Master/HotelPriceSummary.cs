using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hotel.Api.Entities.Master;

public class HotelPriceSummary
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)] // 🔥 prevent index bloat
    public string Slug { get; set; } = default!;

    [Column(TypeName = "numeric(18,2)")] // 🔥 konsisten dengan money/price
    public decimal LowestPrice { get; set; }

    public DateTime UpdatedAt { get; set; }

    // 🔥 OPTIONAL (recommended for future)
    public Guid? HotelId { get; set; }
}