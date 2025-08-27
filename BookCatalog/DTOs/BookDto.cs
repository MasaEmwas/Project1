using System.ComponentModel.DataAnnotations;

namespace BookCatalog.DTOs;

public class BookDto
{
    public int? BookId { get; set; }

    [Required]
    [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters.")]
    public string Title { get; set; } = "";

    [Required]
    [StringLength(100, ErrorMessage = "Author name cannot exceed 100 characters.")]
    public string Author { get; set; } = "";

    [Required]
    [StringLength(50, ErrorMessage = "Genre cannot exceed 50 characters.")]
    public string Genre { get; set; } = "";

    [Required] // should be removed? because its non-nullable in Book model
    [Range(1450, 2025, ErrorMessage = "Published year must be between 1450 and 2025.")]
    public int PublishedYear { get; set; }

    [Required]// should be removed? because its non-nullable in Book model
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be a positive value.")]
    public decimal Price { get; set; }
    
}