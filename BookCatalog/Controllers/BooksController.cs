using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BookCatalog.Services;
using BookCatalog.DTOs;
using BookCatalog.Models;
using Microsoft.Extensions.Logging; //added this for the logs 

namespace BookCatalog.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BooksController(BookService bookService, ILogger<BooksController> logger) : ControllerBase
{
    private readonly BookService _bookService = bookService;
    private readonly ILogger<BooksController> _logger = logger;

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
            return Problem(
                statusCode: 404,
                title: "Not Found",
                detail: "Book not found.",
                instance: HttpContext.Request.Path);
        return Ok(book);
    }

    [HttpGet("search")]
    [AllowAnonymous]
    public IActionResult GetByTitle([FromQuery] string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return Problem(
                statusCode: 400,
                title: "Bad Request",
                detail: "A non-empty 'title' query is required.",
                instance: HttpContext.Request.Path);
        var results = _bookService.GetByTitle(title);
        if (results is null || results.Count == 0)
            return Problem(
                statusCode: 404,
                title: "Not Found",
                detail: "No books matched the title keyword.",
                instance: HttpContext.Request.Path);
        return Ok(results);
    }

    // -------- POST(create) --------

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public IActionResult CreateBook([FromBody] BookDto bookDto)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var created = _bookService.Add(MapToBook(bookDto));
        _logger.LogInformation("Book created {BookId} {Title}", created.BookId, created.Title);
        return CreatedAtAction(nameof(GetBookById), new { id = created.BookId }, created);
    }

    // -------- PUT(update) --------

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public IActionResult UpdateBook(int id, [FromBody] BookDto bookDto)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var updated = _bookService.Update(id, MapToBook(bookDto));
        if (updated is null)
        {
            _logger.LogWarning("Update failed: book {BookId} not found", id);
            return Problem(
                statusCode: 404,
                title: "Not Found",
                detail: "Book not found.",
                instance: HttpContext.Request.Path);
        }

        _logger.LogInformation("Book updated {BookId} {Title}", updated.BookId, updated.Title);
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
            _logger.LogWarning("Delete failed: book {BookId} not found", id);
            return Problem(
                statusCode: 404,
                title: "Not Found",
                detail: "Book not found.",
                instance: HttpContext.Request.Path);
        }

        _logger.LogInformation("Book deleted {BookId}", id);
        return NoContent();
    }
}
