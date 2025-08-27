using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BookCatalog.Services;
using BookCatalog.DTOs;
using BookCatalog.Models;

namespace BookCatalog.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BooksController : ControllerBase
{
    private readonly BookService _bookService;

    public BooksController(BookService bookService)
    {
        _bookService = bookService;
    }

    private static Book MapToBook(BookDto bookDto)
    {
        return new Book
        {
            Title = bookDto.Title,
            Author = bookDto.Author,
            Genre = bookDto.Genre,
            PublishedYear = bookDto.PublishedYear,
            Price = bookDto.Price
        };
    }

    // -------- GET --------

    [HttpGet]
    [AllowAnonymous]
    public IActionResult GetAllBooks(
        [FromQuery] string? author,
        [FromQuery] string? genre,
        [FromQuery] int? year,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var books = _bookService.GetAll(author, genre, year, page, pageSize);
        return Ok(books);
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public IActionResult GetBookById(int id)
    {
        var book = _bookService.GetById(id);
        if (book is null) 
        {
            return NotFound(new { message = "Book not found." });
        }

        return Ok(book);
    }

    [HttpGet("search")]
    [AllowAnonymous]
    public IActionResult GetByTitle([FromQuery] string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return BadRequest(new { message = "A non-empty 'title' query is required." });
        }

        var results = _bookService.GetByTitle(title);

        if (results is null || results.Count == 0)
        {
            return NotFound(new { message = "No books matched the title keyword." });
        }

        return Ok(results);
    }

    // -------- POST(create) --------

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public IActionResult CreateBook([FromBody] BookDto bookDto) 
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState); 
        }

        var book = MapToBook(bookDto);           
        var created = _bookService.Add(book);

        return CreatedAtAction(nameof(GetBookById), new { id = created.BookId }, created);
    }

    // -------- PUT(update) --------

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public IActionResult UpdateBook(int id, [FromBody] BookDto bookDto) 
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState); 
        }

        var updated = _bookService.Update(id, MapToBook(bookDto)); 

        if (updated is null)
        {
            return NotFound(new { message = "Book not found." }); 
        }

        return Ok(updated);
    }

    // -------- DELETE --------

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public IActionResult DeleteBook(int id)
    {
        var deleted = _bookService.Delete(id);

        if (!deleted)
        {
            return NotFound(new { message = "Book not found." }); 
        }

        return NoContent();
    }
}
