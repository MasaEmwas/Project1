using BookCatalog.Models;

namespace BookCatalog.Services;

public class UserListsService(BookService bookService)
{
    private readonly Dictionary<string, HashSet<int>> _favoritesByUser =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, HashSet<int>> _wishlistByUser =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly BookService _bookService = bookService ?? throw new ArgumentNullException(nameof(bookService));

    // ---------- Favorites ----------

    public bool AddFavorite(string userId, int bookId)
    {
        if (!BookExists(bookId)) return false;
        return GetSet(_favoritesByUser, userId).Add(bookId);
    }

    public bool RemoveFavorite(string userId, int bookId)
    {
        return GetSet(_favoritesByUser, userId).Remove(bookId);
    }

    public List<Book> GetFavorites(string userId)
    {
        return ProjectToBooks(GetSet(_favoritesByUser, userId));
    }

    // ---------- Wishlist ----------

    public bool AddToWishlist(string userId, int bookId)
    {
        if (!BookExists(bookId)) return false;
        return GetSet(_wishlistByUser, userId).Add(bookId);
    }

    public bool RemoveFromWishlist(string userId, int bookId)
    {
        return GetSet(_wishlistByUser, userId).Remove(bookId);
    }

    public List<Book> GetWishlist(string userId)
    {
        return ProjectToBooks(GetSet(_wishlistByUser, userId));
    }

    // ---------- Move wishlist -> favorites ----------

    public bool MoveWishlistToFavorites(string userId, int bookId)
    {
        var wish = GetSet(_wishlistByUser, userId);
        if (!wish.Remove(bookId)) return false; // not in wishlist

        var fav = GetSet(_favoritesByUser, userId);
        fav.Add(bookId); // HashSet prevents duplicates
        return true;
    }

    // ---------- Helpers ----------

    private static HashSet<int> GetSet(Dictionary<string, HashSet<int>> store, string userId)
    {
        HashSet<int>? set;
        bool found = store.TryGetValue(userId, out set);
        if (!found)
        {
            set = new HashSet<int>();
            store[userId] = set;
        }
        return set;
    }

    private bool BookExists(int bookId)
    {
        return _bookService.GetById(bookId) is not null;
    }

    private List<Book> ProjectToBooks(HashSet<int> ids)
    {
        return ids
            .Select(id => _bookService.GetById(id))
            .OfType<Book>()
            .OrderBy(b => b.Title)
            .ToList();
    }
}
