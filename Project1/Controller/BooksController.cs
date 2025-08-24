using Microsoft.AspNetCore.Mvc;
using Project1.Service;
using Project1.DTO;
using Project1.Model;
using Microsoft.AspNetCore.Authorization;

namespace Project1.Controller;

[ApiController]
[Route("books")]
public class BooksController : ControllerBase
{
    private readonly BookService _service;
    public BooksController(BookService service) => _service = service;

    // Step 1 test: GET /books should return []
    // [HttpGet]
    // public IActionResult GetAll() => Ok(_service.GetAll());

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public IActionResult GetById(int id)
    {
        var book = _service.GetById(id);
        if (book == null)
        {
            return NotFound(new { Message = "Book not found." });
        }
        else
        {
            return Ok(book);
        }
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult GetAll([FromQuery] string? author, [FromQuery] string? genre, [FromQuery] int? year, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        return Ok(_service.GetAll(author, genre, year, page, pageSize));
    }

    [HttpGet("search")]
    [AllowAnonymous]

    public IActionResult GetByTitle([FromQuery] string? title)
    {
        if (title == null)
        {
            return BadRequest("You should enter keyword to search");
        }
        var result = _service.GetByTitle(title);
        if (result == null)
        {
            return NotFound("No book contains the keyword");
        }

        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public IActionResult Create([FromBody] BookDto dto)
    {
        var book = new Book
        {
            Title = dto.Title,
            Author = dto.Author,
            Genre = dto.Genre,
            PublishedYear = dto.PublishedYear,
            Price = dto.Price
        };
        var created = _service.Add(book);
        return CreatedAtAction(nameof(GetById), new { id = created.BookId }, created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public IActionResult Update(int id, [FromBody] BookDto dto)
    {
        var updatedBook = new Book
        {
            Title = dto.Title,
            Author = dto.Author,
            Genre = dto.Genre,
            PublishedYear = dto.PublishedYear,
            Price = dto.Price
        };
        var updated = _service.Update(id, updatedBook);
        if (updated == null)
        {
            return NotFound(new { Message = "Book not found." });
        }
        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public IActionResult Delete(int id)
    {
        var deleted = _service.Delete(id);
        if (!deleted)
        {
            return NotFound(new { Message = "Book not found." });
        }
        return NoContent();
    }

}
