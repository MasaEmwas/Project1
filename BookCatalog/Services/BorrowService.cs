using BookCatalog.Models;

namespace BookCatalog.Services;

public class BorrowService(BookService bookService)
{
    private readonly BookService _bookService = bookService ?? throw new ArgumentNullException(nameof(bookService));

    // Full history of borrow/return actions (append-only) 
    private readonly List<BorrowEvent> _events = new();

    // Current state: which user holds which book (single copy rule) 
    private readonly Dictionary<int, string> _borrowedByBook = new();

    // Convenience index: which books a user currently holds
    private readonly Dictionary<string, HashSet<int>> _borrowedBooksByUser =
        new(StringComparer.OrdinalIgnoreCase);

    // ---------- Commands ----------

    public bool TryBorrow(string userId, int bookId, out string? error)
    {
        error = null;

        if (_bookService.GetById(bookId) is null)
        {
            error = "Book not found.";
            return false;
        }
        // Does this dictionary contain this BookId?
        if (_borrowedByBook.ContainsKey(bookId))
        {
            error = "Book is already borrowed.";
            return false;
        }

        _borrowedByBook[bookId] = userId;
        GetSet(_borrowedBooksByUser, userId).Add(bookId);

        _events.Add(new BorrowEvent
        {
            BookId = bookId,
            UserId = userId,
            Action = BorrowAction.Borrow,
            TimestampUtc = DateTime.UtcNow
        });

        return true;
    }

    public bool TryReturn(string userId, int bookId, out string? error)
    {
        error = null;

        if (_bookService.GetById(bookId) is null)
        {
            error = "Book not found.";
            return false;
        }

        if (!_borrowedByBook.TryGetValue(bookId, out var borrower))
        {
            error = "Book is not currently borrowed.";
            return false;
        }

        // Require the borrower to be the one returning (simple rule)
        if (!string.Equals(borrower, userId, StringComparison.OrdinalIgnoreCase))
        {
            error = "Only the borrower can return this book.";
            return false;
        }

        _borrowedByBook.Remove(bookId);
        GetSet(_borrowedBooksByUser, userId).Remove(bookId);

        _events.Add(new BorrowEvent
        {
            BookId = bookId,
            UserId = userId,
            Action = BorrowAction.Return,
            TimestampUtc = DateTime.UtcNow
        });

        return true;
    }

    // ---------- Queries ----------

    public List<BorrowEvent> GetBookHistory(int bookId)
    {
        return _events
            .Where(e => e.BookId == bookId)
            .OrderBy(e => e.TimestampUtc)
            .ToList();
    }

    public List<BorrowEvent> GetUserHistory(string userId)
    {
        return _events
            .Where(e => string.Equals(e.UserId, userId, StringComparison.OrdinalIgnoreCase))
            .OrderBy(e => e.TimestampUtc)
            .ToList();
    }

    public List<Book> GetUserCurrent(string userId)
    {
        var ids = GetSet(_borrowedBooksByUser, userId);

        return ids
            .Select(id => _bookService.GetById(id)) 
            .OfType<Book>()
            .OrderBy(b => b.Title)
            .ToList();
    }

    // ---------- Helpers ----------

    private static HashSet<int> GetSet(Dictionary<string, HashSet<int>> store, string userId)
    {
        HashSet<int>? set;
        if (!store.TryGetValue(userId, out set))
        {
            set = new HashSet<int>(); 
            store[userId] = set;
        }
        return set;
    }
}
