namespace StockLens.Domain.Enums;

/// <summary>Proposed action a manager can log against an (aging) vehicle.</summary>
public enum ActionType
{
    PriceReductionPlanned = 0,
    MoveToAuction = 1,
    Promote = 2,
    TransferToBranch = 3,
    Recondition = 4,
    Other = 5
}
