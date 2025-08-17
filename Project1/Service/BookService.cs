using Project1.Model;
using Microsoft.AspNetCore.Hosting;

namespace Project1.Service;

public class BookService
{
    private readonly List<Book> _books = new();
    private readonly string _csvPath;
    private int _nextId = 1;

    public BookService(IWebHostEnvironment env) // Constructor
    {
        // Absolute path to Data/book.csv inside your project
        _csvPath = Path.Combine(env.ContentRootPath, "Data", "book.csv");
        LoadFromCsv(_csvPath);
    }

    private void LoadFromCsv(string path)
    {
        if (!File.Exists(path)) return;

        // Read all lines, skip header
        var lines = File.ReadAllLines(path).Skip(1);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            // simple CSV: fields are comma-separated, no quoted commas
            var parts = line.Split(',', StringSplitOptions.TrimEntries);

            // Defensive: ensure we have exactly 6 columns
            if (parts.Length < 6) continue;

            try
            {
                var book = new Book
                {
                    BookId = int.Parse(parts[0]),
                    Title = parts[1],
                    Author = parts[2],
                    Genre = parts[3],
                    PublishedYear = int.Parse(parts[4]),
                    Price = decimal.Parse(parts[5])
                };

                _books.Add(book);
                _nextId = Math.Max(_nextId, book.BookId + 1);
            }
            catch
            {
                // If a line is malformed, skip it quietly for now
                Console.WriteLine($"Skipping malformed line: {line}");
            }
        }
    }

    // ===== currently only READ =====

    public List<Book> GetAll() => _books;

    public Book? GetById(int id) => _books.FirstOrDefault(b => b.BookId == id);

    public List<Book> GetAll(string? author, string? genre, int? year, int page = 1, int pageSize = 10)
    {
        var query = _books.AsEnumerable();

        if (!string.IsNullOrEmpty(author))
            query = query.Where(b => b.Author.ToLower() == author.ToLower()).OrderBy(b => b.Price);


        if (!string.IsNullOrEmpty(genre))
            query = query.Where(b => b.Genre.ToLower() == genre.ToLower()).OrderBy(b => b.Price);

        if (year.HasValue)
            query = query.Where(b => b.PublishedYear == year.Value).OrderBy(b => b.Price);

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;

        query = query.Skip((page - 1) * pageSize).Take(pageSize);


        return query.ToList();
    }

    // We'll use these later; leaving them here is fine
    public Book Add(Book book)
    {
        book.BookId = _nextId++;
        _books.Add(book);
        SaveToCsv();  
        return book;
    }
    public Book? Update(int id, Book updated)
    {
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
        var bookToRemove = _books.FirstOrDefault(b => b.BookId == id);
        if (bookToRemove is null)
            return false; 

        _books.Remove(bookToRemove);
        SaveToCsv();
        return true;
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
