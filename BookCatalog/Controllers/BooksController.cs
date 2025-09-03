using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BookCatalog.Services;
using BookCatalog.DTOs;
using BookCatalog.Models;
using Microsoft.Extensions.Logging; //added this for the logs 

namespace BookCatalog.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BooksController(BookService bookService, BorrowService borrowService, ILogger<BooksController> logger) : ControllerBase
{
    private readonly BookService _bookService = bookService;
    private readonly BorrowService _borrowService = borrowService;
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




    // added these actions for the borrowing and returning of books
    // POST /api/books/{id}/borrow  -> current user borrows the book
    [HttpPost("{id:int}/borrow")]
    [Authorize]
    public IActionResult Borrow(int id)
    {
        var userId = User?.Identity?.Name ?? string.Empty;
        if (string.IsNullOrWhiteSpace(userId))
            return Problem(statusCode: 401, title: "Unauthorized", detail: "Login required.", instance: HttpContext.Request.Path);

        if (!_borrowService.TryBorrow(userId, id, out var error))
        {
            return error switch
            {
                "Book not found."          => Problem(404, "Not Found", error, HttpContext.Request.Path),
                "Book is already borrowed."=> Problem(409, "Conflict",   error, HttpContext.Request.Path),
                _                          => Problem(400, "Bad Request", error, HttpContext.Request.Path)
            };
        }

        _logger.LogInformation("Borrowed {BookId} by {UserId}", id, userId);
        return NoContent();
    }

    // POST /api/books/{id}/return  -> current user returns it
    [HttpPost("{id:int}/return")]
    [Authorize]
    public IActionResult Return(int id)
    {
        var userId = User?.Identity?.Name ?? string.Empty;
        if (string.IsNullOrWhiteSpace(userId))
            return Problem(statusCode: 401, title: "Unauthorized", detail: "Login required.", instance: HttpContext.Request.Path);

        if (!_borrowService.TryReturn(userId, id, out var error))
        {
            return error switch
            {
                "Book not found."                         => Problem(404, "Not Found", error, HttpContext.Request.Path),
                "Book is not currently borrowed."         => Problem(409, "Conflict",   error, HttpContext.Request.Path),
                "Only the borrower can return this book." => Problem(403, "Forbidden",  error, HttpContext.Request.Path),
                _                                         => Problem(400, "Bad Request", error, HttpContext.Request.Path)
            };
        }

        _logger.LogInformation("Returned {BookId} by {UserId}", id, userId);
        return NoContent();
    }

    // GET /api/books/{id}/history  -> full borrow/return log for this book
    [HttpGet("{id:int}/history")]
    [AllowAnonymous]
    public IActionResult GetBookHistory(int id)
    {
        if (_bookService.GetById(id) is null)
            return Problem(404, "Not Found", "Book not found.", HttpContext.Request.Path);

        var history = _borrowService.GetBookHistory(id);
        return Ok(history);
    }

    // tiny helper to keep Problem(...) calls concise
    private ObjectResult Problem(int statusCode, string title, string detail, string instance)
        => Problem(statusCode: statusCode, title: title, detail: detail, instance: instance);



}
