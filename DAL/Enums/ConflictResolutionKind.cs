namespace DAL.Enums;

public enum ConflictResolutionKind
{
    Unspecified = 0,
    FixedWithSqlChange = 1,
    ClosedWithoutSqlChange = 2
}
