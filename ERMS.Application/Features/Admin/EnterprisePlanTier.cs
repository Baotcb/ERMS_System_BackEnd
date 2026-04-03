using System.Linq.Expressions;
using ERMS.Domain.Entities.Enterprise;

namespace ERMS.Application.Features.Admin;

public static class EnterprisePlanTier
{
    public const string Free = "Free";
    public const string Pro = "Pro";

    public static readonly string[] ProPlanTokens = ["pro"];
    private static readonly HashSet<string> FreeTierAliases = [Free.ToLowerInvariant(), "free"];
    private static readonly HashSet<string> ProTierAliases = [Pro.ToLowerInvariant(), "pro"];
    private static readonly Expression<Func<Enterprise, bool>> ProEnterpriseExpression = BuildEnterpriseTierExpression(matchesProTier: true);
    private static readonly Expression<Func<Enterprise, bool>> FreeEnterpriseExpression = BuildEnterpriseTierExpression(matchesProTier: false);

    public static string? NormalizeTierName(string? value)
    {
        var normalizedValue = value?.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(normalizedValue))
        {
            return null;
        }

        if (FreeTierAliases.Contains(normalizedValue))
        {
            return Free;
        }

        if (ProTierAliases.Contains(normalizedValue))
        {
            return Pro;
        }

        return null;
    }

    public static bool IsProPlan(string? planName, string? planCode)
    {
        var normalizedName = planName?.Trim().ToLowerInvariant() ?? string.Empty;
        var normalizedCode = planCode?.Trim().ToLowerInvariant() ?? string.Empty;

        return ProPlanTokens.Any(token =>
            normalizedName.Contains(token, StringComparison.Ordinal) ||
            normalizedCode.Contains(token, StringComparison.Ordinal));
    }

    public static string GetTierName(string? planName, string? planCode)
    {
        return IsProPlan(planName, planCode) ? Pro : Free;
    }

    public static IQueryable<Enterprise> ApplyTierFilter(IQueryable<Enterprise> enterprises, string? tierInput)
    {
        var normalizedTier = NormalizeTierName(tierInput);

        if (normalizedTier is null)
        {
            return enterprises;
        }

        return normalizedTier == Pro
            ? enterprises.Where(ProEnterpriseExpression)
            : enterprises.Where(FreeEnterpriseExpression);
    }

    private static Expression<Func<Enterprise, bool>> BuildEnterpriseTierExpression(bool matchesProTier)
    {
        var enterpriseParameter = Expression.Parameter(typeof(Enterprise), "enterprise");
        var subscriptionPlan = Expression.Property(enterpriseParameter, nameof(Enterprise.SubscriptionPlan));
        var planName = Expression.Property(subscriptionPlan, nameof(SubscriptionPlan.PlanName));
        var planCode = Expression.Property(subscriptionPlan, nameof(SubscriptionPlan.PlanCode));

        var proMatchExpression = ProPlanTokens
            .Select(token => Expression.OrElse(
                BuildContainsExpression(planName, token),
                BuildContainsExpression(planCode, token)))
            .Aggregate(Expression.OrElse);

        Expression finalExpression = matchesProTier
            ? proMatchExpression
            : Expression.Not(proMatchExpression);

        return Expression.Lambda<Func<Enterprise, bool>>(finalExpression, enterpriseParameter);
    }

    private static Expression BuildContainsExpression(MemberExpression propertyExpression, string token)
    {
        var stringValue = Expression.Coalesce(propertyExpression, Expression.Constant(string.Empty));
        var normalizedValue = Expression.Call(
            stringValue,
            typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)!);

        return Expression.Call(
            normalizedValue,
            typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!,
            Expression.Constant(token));
    }
}
