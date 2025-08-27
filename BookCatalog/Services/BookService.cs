using BookCatalog.Models;
using Microsoft.AspNetCore.Hosting;

namespace BookCatalog.Services;

public class BookService
{
    private readonly List<Book> _books = new();
    private readonly string _csvPath;
    private int _nextId = 1;

    public BookService(IWebHostEnvironment env)
    {
        if (env is null) throw new ArgumentNullException(nameof(env));
        _csvPath = Path.Combine(env.ContentRootPath, "Data", "book.csv");
        LoadFromCsv(_csvPath);
    }

    private void LoadFromCsv(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                Console.WriteLine($"CSV not found at '{path}'. Starting with an empty catalog.");
                return;
            }

            foreach (var line in File.ReadLines(path).Skip(1)) 
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split(',', StringSplitOptions.TrimEntries);
                if (parts.Length < 6)
                {
                    Console.WriteLine($"Skipping line with insufficient columns: {line}");
                    continue;
                }

                if (!int.TryParse(parts[0], out var id))   { Console.WriteLine($"Bad Id in line: {line}"); continue; }
                if (!int.TryParse(parts[4], out var year)) { Console.WriteLine($"Bad Year in line: {line}"); continue; }
                if (!decimal.TryParse(parts[5], out var price)) { Console.WriteLine($"Bad Price in line: {line}"); continue; }

                var book = new Book
                {
                    BookId = id,
                    Title = parts[1],
                    Author = parts[2],
                    Genre = parts[3],
                    PublishedYear = year,
                    Price = price
                };

                _books.Add(book);
                _nextId = Math.Max(_nextId, book.BookId + 1);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading CSV '{path}': {ex.Message}");
        }
    }

    public List<Book> GetAll() => _books;

    public Book? GetById(int id) => _books.FirstOrDefault(b => b.BookId == id);

    public List<Book> GetAll(
        string? author,
        string? genre,
        int? year,
        int page = 1,
        int pageSize = 10)
    {
        var query = _books.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(author))
        {
            query = query.Where(b => string.Equals(b.Author, author, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(genre))
        {
            query = query.Where(b => string.Equals(b.Genre, genre, StringComparison.OrdinalIgnoreCase));
        }

        if (year.HasValue)
        {
            query = query.Where(b => b.PublishedYear == year.Value);
        }

        query = query.OrderBy(b => b.Price);

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;

        return query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
    }

    public List<Book> GetByTitle(string? keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return new List<Book>();
        }

        return _books
            .Where(b => b.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase)) // (2)
            .OrderBy(b => b.Price)                                                    // (1)
            .ToList();
    }

    public Book Add(Book book)
    {
        if (book is null) throw new ArgumentNullException(nameof(book));
        if (!IsValid(book)) throw new ArgumentException("Invalid book data.", nameof(book));

        book.BookId = _nextId++;
        _books.Add(book);
        SaveToCsv();
        return book;
    }

    public Book? Update(int id, Book updated)
    {
        if (updated is null) throw new ArgumentNullException(nameof(updated));
        if (!IsValid(updated)) throw new ArgumentException("Invalid book data.", nameof(updated));

        var existing = _books.FirstOrDefault(b => b.BookId == id);
        if (existing is null) return null; 

        existing.Title = updated.Title;
        existing.Author = updated.Author;
        existing.Genre = updated.Genre;
        existing.PublishedYear = updated.PublishedYear;
        existing.Price = updated.Price;

        SaveToCsv();
        return existing;
    }

    public bool Delete(int id)
    {
        var toRemove = _books.FirstOrDefault(b => b.BookId == id);
        if (toRemove is null) return false; 

        _books.Remove(toRemove);
        SaveToCsv();
        return true;
    }

    private static bool IsValid(Book book)
    {
        return !string.IsNullOrWhiteSpace(book.Title)
            && !string.IsNullOrWhiteSpace(book.Author)
            && !string.IsNullOrWhiteSpace(book.Genre)
            && book.PublishedYear > 0
            && book.Price >= 0;
    }

    private void SaveToCsv()
    {
        try
        {
            var lines = new List<string>
            {
                "BookID,Title,Author,Genre,PublishedYear,Price"
            };

            foreach (var b in _books)
            {
                lines.Add($"{b.BookId},{b.Title},{b.Author},{b.Genre},{b.PublishedYear},{b.Price}");
            }

            File.WriteAllLines(_csvPath, lines);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to save CSV: {ex.Message}");
        }
    }
}
