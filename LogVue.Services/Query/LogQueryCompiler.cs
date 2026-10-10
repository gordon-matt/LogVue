namespace LogVue.Services.Query;

/// <summary>
/// Turns a parsed query into an EF-translatable predicate. Values travel as captured constants,
/// so EF sends them as parameters. Sejil spliced them into SQL text, which made every distinct
/// query a new plan and left quoting as the only defence against injection.
/// </summary>
internal sealed class LogQueryCompiler(ILogVueQueryDialect dialect)
{
    public Expression<Func<LogEntry, bool>> Compile(string query) =>
        Compile(LogQueryParser.Parse(query)).Expand();

    private Expression<Func<LogEntry, bool>> Compile(QueryNode node) => node switch
    {
        LogicalNode { Operator: LogicalOperator.And } logical => Compile(logical.Left).And(Compile(logical.Right)),
        LogicalNode logical => Compile(logical.Left).Or(Compile(logical.Right)),
        ComparisonNode comparison => CompileComparison(comparison),
        TextSearchNode search => CompileTextSearch(search),
        _ => throw new NotSupportedException($"Unknown query node {node.GetType().Name}."),
    };

    private Expression<Func<LogEntry, bool>> CompileTextSearch(TextSearchNode search)
    {
        var predicate = PredicateBuilder.New<LogEntry>(true);

        // Every term has to appear somewhere in the entry, in any order.
        foreach (string term in search.Terms)
        {
            var match = dialect.Contains(term);

            predicate = predicate.And(e =>
                match.Invoke(e.Message)
                || match.Invoke(e.Exception)
                || e.Properties.Any(p => match.Invoke(p.Value)));
        }

        return predicate;
    }

    private Expression<Func<LogEntry, bool>> CompileComparison(ComparisonNode node) =>
        LogField.Resolve(node.Field) switch
        {
            LogField.Message => CompileText(node, e => e.Message, nullable: false),
            LogField.MessageTemplate => CompileText(node, e => e.MessageTemplate, nullable: false),
            LogField.Exception => CompileText(node, e => e.Exception, nullable: true),
            LogField.Level => CompileLevel(node),
            LogField.Timestamp => throw new LogQueryException(
                $"At position {node.Position + 1}: filter on time with the date range picker rather than the query."),
            _ => CompileProperty(node),
        };

    private Expression<Func<LogEntry, bool>> CompileText(
        ComparisonNode node,
        Expression<Func<LogEntry, string?>> column,
        bool nullable)
    {
        var match = Match(node);

        Expression<Func<LogEntry, bool>> predicate = IsNegated(node)
            ? nullable
                ? e => column.Invoke(e) == null || !match.Invoke(column.Invoke(e))
                : e => !match.Invoke(column.Invoke(e))
            : e => match.Invoke(column.Invoke(e));

        return predicate;
    }

    private static Expression<Func<LogEntry, bool>> CompileLevel(ComparisonNode node)
    {
        if (node.Operator is ComparisonOperator.Like or ComparisonOperator.NotLike)
        {
            throw new LogQueryException(
                $"At position {node.Position + 1}: level can only be compared with = or !=.");
        }

        if (!LogLevels.TryParse(node.Value, out var level))
        {
            throw new LogQueryException(
                $"At position {node.Position + 1}: '{node.Value}' is not a level. " +
                "Use Verbose, Debug, Information, Warning, Error or Fatal.");
        }

        return node.Operator == ComparisonOperator.Equal
            ? e => e.Level == level
            : e => e.Level != level;
    }

    /// <summary>
    /// <c>Name = 'x'</c> means "has a property Name equal to x", and <c>Name != 'x'</c> means
    /// "has no property Name equal to x" — which includes entries without that property at all.
    /// That matches Sejil, so <c>StatusCode like '%' and StatusCode != 200</c> still reads as
    /// "has a status code, and it isn't 200".
    /// </summary>
    private Expression<Func<LogEntry, bool>> CompileProperty(ComparisonNode node)
    {
        var nameMatch = dialect.EqualTo(node.Field);
        var valueMatch = Match(node);

        Expression<Func<LogEntry, bool>> predicate = IsNegated(node)
            ? e => !e.Properties.Any(p => nameMatch.Invoke(p.Name) && valueMatch.Invoke(p.Value))
            : e => e.Properties.Any(p => nameMatch.Invoke(p.Name) && valueMatch.Invoke(p.Value));

        return predicate;
    }

    /// <summary>The positive form of the comparison; negation is applied by the caller.</summary>
    private Expression<Func<string?, bool>> Match(ComparisonNode node) =>
        node.Operator is ComparisonOperator.Like or ComparisonOperator.NotLike
            ? dialect.Like(node.Value)
            : dialect.EqualTo(node.Value);

    private static bool IsNegated(ComparisonNode node) =>
        node.Operator is ComparisonOperator.NotEqual or ComparisonOperator.NotLike;
}
