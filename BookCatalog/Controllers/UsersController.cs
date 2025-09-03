using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BookCatalog.Services;
using Microsoft.Extensions.Logging;

namespace BookCatalog.Controllers;

[ApiController]
[Route("api/users/{userId}")]
[Authorize] // all endpoints require auth
public class UsersController(UserListsService userLists, BookService bookService, BorrowService borrowService, ILogger<UsersController> logger) : ControllerBase
{
    private readonly UserListsService _userLists = userLists;
    private readonly BookService _bookService = bookService;
    private readonly BorrowService _borrowService = borrowService;
    private readonly ILogger<UsersController> _logger = logger;

    // ---- helpers ----
    private bool IsSelfOrAdmin(string userId)
    {
        var me = User?.Identity?.Name;
        return User?.Identity?.IsAuthenticated == true &&
               (string.Equals(me, userId, StringComparison.OrdinalIgnoreCase) || User.IsInRole("Admin"));
    }

    private IActionResult ForbidProblem()
    {
        return Problem(
            statusCode: 403,
            title: "Forbidden",
            detail: "You can only access your own lists unless you are an Admin.",
            instance: HttpContext.Request.Path);
    }

    // ================= FAVOURITES =================

    // POST /api/users/{userId}/favorites/{bookId}
    [HttpPost("favorites/{bookId:int}")]
    public IActionResult AddFavorite(string userId, int bookId)
    {
        if (!IsSelfOrAdmin(userId)) return ForbidProblem();

        if (_bookService.GetById(bookId) is null)
        {
            return Problem(
                statusCode: 404,
                title: "Not Found",
                detail: "Book not found.",
                instance: HttpContext.Request.Path);
        }

        var added = _userLists.AddFavorite(userId, bookId);
        if (!added)
        {
            return Problem(
                statusCode: 409,
                title: "Conflict",
                detail: "Book is already in favorites.",
                instance: HttpContext.Request.Path);
        }

        _logger.LogInformation("Favorite added {UserId} {BookId}", userId, bookId);
        return NoContent();
    }

    // GET /api/users/{userId}/favorites
    [HttpGet("favorites")]
    public IActionResult GetFavorites(string userId)
    {
        if (!IsSelfOrAdmin(userId)) return ForbidProblem();

        var books = _userLists.GetFavorites(userId);
        return Ok(books);
    }

    // DELETE /api/users/{userId}/favorites/{bookId}
    [HttpDelete("favorites/{bookId:int}")]
    public IActionResult RemoveFavorite(string userId, int bookId)
    {
        if (!IsSelfOrAdmin(userId)) return ForbidProblem();

        var removed = _userLists.RemoveFavorite(userId, bookId);
        if (!removed)
        {
            return Problem(
                statusCode: 404,
                title: "Not Found",
                detail: "Book not in favorites.",
                instance: HttpContext.Request.Path);
        }

        _logger.LogInformation("Favorite removed {UserId} {BookId}", userId, bookId);
        return NoContent();
    }

    // ================= WISHLIST =================

    // POST /api/users/{userId}/wishlist/{bookId}
    [HttpPost("wishlist/{bookId:int}")]
    public IActionResult AddToWishlist(string userId, int bookId)
    {
        if (!IsSelfOrAdmin(userId)) return ForbidProblem();

        if (_bookService.GetById(bookId) is null)
        {
            return Problem(
                statusCode: 404,
                title: "Not Found",
                detail: "Book not found.",
                instance: HttpContext.Request.Path);
        }

        var added = _userLists.AddToWishlist(userId, bookId);
        if (!added)
        {
            return Problem(
                statusCode: 409,
                title: "Conflict",
                detail: "Book is already in wishlist.",
                instance: HttpContext.Request.Path);
        }

        _logger.LogInformation("Wishlist added {UserId} {BookId}", userId, bookId);
        return NoContent();
    }

    // GET /api/users/{userId}/wishlist
    [HttpGet("wishlist")]
    public IActionResult GetWishlist(string userId)
    {
        if (!IsSelfOrAdmin(userId)) return ForbidProblem();

        var books = _userLists.GetWishlist(userId);
        return Ok(books);
    }

    // DELETE /api/users/{userId}/wishlist/{bookId}
    [HttpDelete("wishlist/{bookId:int}")]
    public IActionResult RemoveFromWishlist(string userId, int bookId)
    {
        if (!IsSelfOrAdmin(userId)) return ForbidProblem();

        var removed = _userLists.RemoveFromWishlist(userId, bookId);
        if (!removed)
        {
            return Problem(
                statusCode: 404,
                title: "Not Found",
                detail: "Book not in wishlist.",
                instance: HttpContext.Request.Path);
        }

        _logger.LogInformation("Wishlist removed {UserId} {BookId}", userId, bookId);
        return NoContent();
    }

    // POST /api/users/{userId}/wishlist/{bookId}/move-to-favorites
    [HttpPost("wishlist/{bookId:int}/move-to-favorites")]
    public IActionResult MoveWishlistToFavorites(string userId, int bookId)
    {
        if (!IsSelfOrAdmin(userId)) return ForbidProblem();

        if (_bookService.GetById(bookId) is null)
        {
            return Problem(
                statusCode: 404,
                title: "Not Found",
                detail: "Book not found.",
                instance: HttpContext.Request.Path);
        }

        var moved = _userLists.MoveWishlistToFavorites(userId, bookId);
        if (!moved)
        {
            return Problem(
                statusCode: 404,
                title: "Not Found",
                detail: "Book not in wishlist.",
                instance: HttpContext.Request.Path);
        }

        _logger.LogInformation("Wishlist -> Favorites moved {UserId} {BookId}", userId, bookId);
        return NoContent();
    }

    //Added these for the borrowing and returning of books:
    // GET /api/users/{userId}/borrowed  -> books currently borrowed by the user
    [HttpGet("borrowed")]
    public IActionResult GetUserBorrowed(string userId)
    {
        if (!IsSelfOrAdmin(userId)) return ForbidProblem();
        var books = _borrowService.GetUserCurrent(userId);
        return Ok(books);
    }

    // GET /api/users/{userId}/history  -> full borrow/return history for the user
    [HttpGet("history")]
    public IActionResult GetUserHistory(string userId)
    {
        if (!IsSelfOrAdmin(userId)) return ForbidProblem();
        var history = _borrowService.GetUserHistory(userId);
        return Ok(history);
    }

}
