namespace StockLens.Application.Dtos;

public record SalespersonDto(
    Guid Id,
    string FullName,
    string? Email,
    string? Team,
    DateOnly HireDate,
    bool IsActive,
    int SalesCount,
    decimal Revenue);
