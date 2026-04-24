namespace MemPalace.Core.Backends;

/// <summary>
/// Base class for filter clauses. Backends that cannot handle specific clauses throw UnsupportedFilterException.
/// </summary>
public abstract record WhereClause;

public sealed record Eq(string Field, object? Value) : WhereClause;
public sealed record NotEq(string Field, object? Value) : WhereClause;
public sealed record Gt(string Field, object? Value) : WhereClause;
public sealed record Gte(string Field, object? Value) : WhereClause;
public sealed record Lt(string Field, object? Value) : WhereClause;
public sealed record Lte(string Field, object? Value) : WhereClause;
public sealed record In(string Field, IReadOnlyList<object?> Values) : WhereClause;
public sealed record NotIn(string Field, IReadOnlyList<object?> Values) : WhereClause;
public sealed record And(IReadOnlyList<WhereClause> Clauses) : WhereClause;
public sealed record Or(IReadOnlyList<WhereClause> Clauses) : WhereClause;
