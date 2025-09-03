namespace BookCatalog.Models;

public enum BorrowAction
{
    Borrow,
    Return
}

public class BorrowEvent
{
    public int BookId { get; set; }
    public required string UserId { get; set; }
    public BorrowAction Action { get; set; }
    public DateTime TimestampUtc { get; set; }
}
