namespace StockLens.Domain.Enums;

/// <summary>Progress state of a logged action.</summary>
public enum ActionStatus
{
    Open = 0,
    InProgress = 1,
    Done = 2,
    Cancelled = 3
}
