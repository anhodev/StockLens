namespace StockLens.Domain.Entities;

/// <summary>
/// A member of the dealership's sales team. Sales are attributed to a salesperson so
/// managers can track individual performance alongside inventory health.
/// </summary>
public class Salesperson
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }

    /// <summary>Team or desk the salesperson works on, e.g. "New", "Used", "Fleet".</summary>
    public string? Team { get; set; }

    public DateOnly HireDate { get; set; }

    /// <summary>Inactive salespeople keep their historical sales but take no new ones.</summary>
    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<SalesRecord> Sales { get; set; } = new List<SalesRecord>();
}
