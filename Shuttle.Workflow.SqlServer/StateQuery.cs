using System.Data;
using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Shuttle.Workflow.SqlServer;

public class StateQuery(WorkflowDbContext dbContext) : IStateQuery
{
    // Each state item covers the half-open interval [EffectiveDate, EffectiveDateEnd), so an "as at" read and
    // an overlap read are both plain range predicates.  This replaces the 'MAX(EffectiveDate) GROUP BY
    // StateId, Name' CTE that used to resolve the latest row per name by scanning the whole table.
    private const string ItemEffectivePredicate = @"
    (
        @ItemEffectiveDate IS NULL
        OR
        (
            si.EffectiveDate <= @ItemEffectiveDate
            AND
            si.EffectiveDateEnd > @ItemEffectiveDate
        )
    )
    AND
    (
        @ItemStartEffectiveDate IS NULL
        OR
        si.EffectiveDateEnd > @ItemStartEffectiveDate
    )
    AND
    (
        @ItemEndEffectiveDateExclusive IS NULL
        OR
        si.EffectiveDate < @ItemEndEffectiveDateExclusive
    )
";

    public async Task<IEnumerable<Models.State>> SearchAsync(Models.State.Specification specification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);

        var sqlParameters = new List<object>
        {
            new SqlParameter("@Key", !string.IsNullOrWhiteSpace(specification.Key) ? specification.Key : DBNull.Value),
            new SqlParameter("@KeyMatch", !string.IsNullOrWhiteSpace(specification.KeyMatch) ? specification.KeyMatch.Replace("[", "\\[") : DBNull.Value),
            GetDateTimeOffsetParameter("@ItemEffectiveDate", specification.ItemEffectiveDate),
            GetDateTimeOffsetParameter("@ItemStartEffectiveDate", specification.ItemStartEffectiveDate),
            GetDateTimeOffsetParameter("@ItemEndEffectiveDateExclusive", specification.ItemEndEffectiveDateExclusive)
        };

        var keys = specification.Keys.ToList();

        for (var i = 0; i < keys.Count; i++)
        {
            sqlParameters.Add(new SqlParameter($"@Key{i}", keys[i]));
        }

        var keysWhereClause = keys.Count > 0
            ? $"s.[Key] IN ({string.Join(',', Enumerable.Range(0, keys.Count).Select(i => $"@Key{i}"))}) AND"
            : string.Empty;

        var itemMatchWhereClause = new StringBuilder();
        var index = 1;

        foreach (var itemMatch in specification.ItemMatches)
        {
            var sqlItemNameParameter = new SqlParameter($"@ItemName{index}", itemMatch.Name);
            var itemValueComparison = "1=1";

            switch (itemMatch.Operator.ToUpperInvariant())
            {
                case "EXISTS":
                case "!EXISTS":
                {
                    itemMatchWhereClause.AppendLine($@"
AND {(itemMatch.Operator.Equals("EXISTS", StringComparison.InvariantCultureIgnoreCase) ? "EXISTS" : "NOT EXISTS")}
(
    SELECT
        1
    FROM
        [workflow].[StateItem] si
    WHERE
        si.StateId = s.Id
    AND
        si.[Name] = {sqlItemNameParameter.ParameterName}
    AND
{ItemEffectivePredicate}
)
");
                    break;
                }
                default:
                {
                    var sqlItemValueParameter = new SqlParameter($"@ItemValue{index}", itemMatch.Value);

                    switch (itemMatch.Type.ToUpperInvariant())
                    {
                        case "STRING":
                        {
                            itemValueComparison = itemMatch.Operator.ToUpperInvariant() switch
                            {
                                "==" => $"si.StringValue = {sqlItemValueParameter.ParameterName}",
                                "!=" => $"si.StringValue <> {sqlItemValueParameter.ParameterName}",
                                "*" => $"si.StringValue LIKE '%' + {sqlItemValueParameter.ParameterName} + '%'",
                                "*=" => $"si.StringValue LIKE '%' + {sqlItemValueParameter.ParameterName}",
                                "=*" => $"si.StringValue LIKE {sqlItemValueParameter.ParameterName} + '%'",
                                _ => throw new InvalidOperationException()
                            };

                            break;
                        }
                        case "DATETIME":
                        {
                            itemValueComparison = itemMatch.Operator.ToUpperInvariant() switch
                            {
                                "==" => $"si.DateTimeValue = {sqlItemValueParameter.ParameterName}",
                                "!=" => $"si.DateTimeValue <> {sqlItemValueParameter.ParameterName}",
                                ">" => $"si.DateTimeValue > {sqlItemValueParameter.ParameterName}",
                                "<" => $"si.DateTimeValue < {sqlItemValueParameter.ParameterName}",
                                ">=" => $"si.DateTimeValue >= {sqlItemValueParameter.ParameterName}",
                                "<=" => $"si.DateTimeValue <= {sqlItemValueParameter.ParameterName}",
                                _ => throw new InvalidOperationException()
                            };

                            break;
                        }
                        case "DECIMAL":
                        {
                            itemValueComparison = itemMatch.Operator.ToUpperInvariant() switch
                            {
                                "==" => $"si.DecimalValue = {sqlItemValueParameter.ParameterName}",
                                "!=" => $"si.DecimalValue <> {sqlItemValueParameter.ParameterName}",
                                ">" => $"si.DecimalValue > {sqlItemValueParameter.ParameterName}",
                                "<" => $"si.DecimalValue < {sqlItemValueParameter.ParameterName}",
                                ">=" => $"si.DecimalValue >= {sqlItemValueParameter.ParameterName}",
                                "<=" => $"si.DecimalValue <= {sqlItemValueParameter.ParameterName}",
                                _ => throw new InvalidOperationException()
                            };

                            break;
                        }
                        case "BOOLEAN":
                        {
                            itemValueComparison = itemMatch.Operator.ToUpperInvariant() switch
                            {
                                "==" => $"si.BooleanValue = {sqlItemValueParameter.ParameterName}",
                                _ => throw new InvalidOperationException()
                            };

                            break;
                        }
                        case "GUID":
                        {
                            itemValueComparison = itemMatch.Operator.ToUpperInvariant() switch
                            {
                                "==" => $"si.GuidValue = {sqlItemValueParameter.ParameterName}",
                                "!=" => $"si.GuidValue <> {sqlItemValueParameter.ParameterName}",
                                _ => throw new InvalidOperationException()
                            };

                            break;
                        }
                    }

                    sqlParameters.Add(sqlItemValueParameter);

                    itemMatchWhereClause.AppendLine($@"
AND EXISTS
(
    SELECT
        1
    FROM
        [workflow].[StateItem] si
    WHERE
        si.StateId = s.Id
    AND
        si.[Name] = {sqlItemNameParameter.ParameterName}
    AND
        {itemValueComparison}
    AND
{ItemEffectivePredicate}
)
");
                    break;
                }
            }

            sqlParameters.Add(sqlItemNameParameter);

            index++;
        }

        var states = await dbContext.States
            .FromSqlRaw(GetStateSql(specification, keysWhereClause, itemMatchWhereClause), sqlParameters.ToArray())
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        if (specification.ShouldIncludeItems)
        {
            var stateItems = await dbContext.StateItems
                .FromSqlRaw(GetStateItemSql(specification, keysWhereClause, itemMatchWhereClause), sqlParameters.ToArray())
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var stateItemLookup = stateItems.ToLookup(si => si.StateId);

            foreach (var state in states)
            {
                state.Items = stateItemLookup[state.Id].ToList();
            }
        }

        return states;
    }

    private static SqlParameter GetDateTimeOffsetParameter(string name, DateTimeOffset? value)
    {
        return new(name, SqlDbType.DateTimeOffset)
        {
            Value = value.HasValue ? value.Value : DBNull.Value
        };
    }

    private static string GetStateItemSql(Models.State.Specification specification, string keysWhereClause, StringBuilder itemMatchWhereClause)
    {
        return $@"
SELECT
    si.*
FROM
    [workflow].[StateItem] si
INNER JOIN
    [workflow].[State] s ON si.StateId = s.Id
WHERE
{(specification.HasIds ? $"s.[Id] IN ({string.Join(',', specification.Ids.Select(item => $"'{item}'"))}) AND" : string.Empty)}
{keysWhereClause}
(
    @Key IS NULL
    OR
    s.[Key] = @Key
)
AND
(
    @KeyMatch IS NULL
    OR
    s.[Key] LIKE '%' + @KeyMatch + '%' ESCAPE '\'
)
AND
{ItemEffectivePredicate}
{itemMatchWhereClause}
ORDER BY si.[Name], si.[EffectiveDate] DESC
";
    }

    private static string GetStateSql(Models.State.Specification specification, string keysWhereClause, StringBuilder itemMatchWhereClause)
    {
        return $@"
SELECT {(specification.MaximumRows > 0 ? $"TOP {specification.MaximumRows}" : string.Empty)}
    s.*
FROM
    [workflow].[State] s
WHERE
{(specification.HasIds ? $"s.[Id] IN ({string.Join(',', specification.Ids.Select(item => $"'{item}'"))}) AND" : string.Empty)}
{(specification.HasExcludedIds ? $"s.[Id] NOT IN ({string.Join(',', specification.ExcludedIds.Select(item => $"'{item}'"))}) AND" : string.Empty)}
{keysWhereClause}
(
    @Key IS NULL
    OR 
    s.[Key] = @Key
)
AND
(
    @KeyMatch IS NULL
    OR 
    s.[Key] LIKE '%' + @KeyMatch + '%' ESCAPE '\'
)
{itemMatchWhereClause}
ORDER BY s.[Key]
";
    }
}