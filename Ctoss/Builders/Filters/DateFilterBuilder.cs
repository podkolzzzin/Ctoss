﻿using System.Linq.Expressions;
using Ctoss.Models.Enums;
using Ctoss.Models.V2;

namespace Ctoss.Builders.Filters;

public class DateFilterBuilder : IPropertyFilterBuilder<DateCondition>
{
    public Expression<Func<T, bool>> GetExpression<T>(string property, DateCondition condition, bool conditionalAccess)
    {
        return condition.Type switch
        {
            DateFilterOptions.Blank or DateFilterOptions.NotBlank or DateFilterOptions.Empty
                => GetBlankExpression<T>(property, condition, conditionalAccess),
            DateFilterOptions.InRange
                => GetRangeExpression<T>(property, condition, conditionalAccess),
            _ => GetComparisonExpression<T>(property, condition, conditionalAccess)
        };
    }

    private Expression<Func<T, bool>> GetBlankExpression<T>(string property, DateCondition condition, bool conditionalAccess)
    {
        var propertyType = IPropertyBuilder.GetPropertyType<T>(property);

        var nullablePropertyType = propertyType.IsValueType && Nullable.GetUnderlyingType(propertyType) == null
            ? typeof(Nullable<>).MakeGenericType(propertyType)
            : propertyType;

        var parameter = Expression.Parameter(typeof(T), "x");
        var propertyExpression = IPropertyBuilder.GetPropertyExpression<T>(property, parameter, propertyType, conditionalAccess);

        return condition.Type switch
        {
            DateFilterOptions.Empty or DateFilterOptions.Blank
                => Expression.Lambda<Func<T, bool>>(
                    Expression.Equal(Expression.Convert(propertyExpression, nullablePropertyType),
                        Expression.Constant(null, nullablePropertyType)), parameter),

            DateFilterOptions.NotBlank
                => Expression.Lambda<Func<T, bool>>(
                    Expression.NotEqual(Expression.Convert(propertyExpression, nullablePropertyType),
                        Expression.Constant(null, nullablePropertyType)), parameter),

            _ => throw new NotSupportedException($"Date filter type '{condition.Type}' is not supported.")
        };
    }

    private Expression<Func<T, bool>> GetRangeExpression<T>(string property, DateCondition condition, bool conditionalAccess)
    {
        var propertyType = IPropertyBuilder.GetPropertyType<T>(property);

        var parameter = Expression.Parameter(typeof(T), "x");

        var propertyExpression = IPropertyBuilder.GetPropertyExpression<T>(property, parameter, propertyType, conditionalAccess);

        if (string.IsNullOrEmpty(condition.DateFrom))
            throw new ArgumentException("DateFrom value is required.");

        if (string.IsNullOrEmpty(condition.DateTo))
            throw new ArgumentException("DateTo value is required.");

        var from = ParseDateValue(condition.DateFrom, propertyType);
        var fromExpression = Expression.Constant(from, propertyType);

        var to = ParseDateValue(condition.DateTo, propertyType);
        var toExpression = Expression.Constant(to, propertyType);

        var greaterThan = Expression.GreaterThan(propertyExpression, fromExpression);
        var lessThan = Expression.LessThan(propertyExpression, toExpression);

        return Expression.Lambda<Func<T, bool>>(
            Expression.AndAlso(greaterThan, lessThan), parameter);
    }

    private Expression<Func<T, bool>> GetComparisonExpression<T>(string property, DateCondition condition, bool conditionalAccess)
    {
        var parameter = Expression.Parameter(typeof(T), "x");

        var propertyType = IPropertyBuilder.GetPropertyType<T>(property);
        var propertyExpression = IPropertyBuilder.GetPropertyExpression<T>(property, parameter, propertyType, conditionalAccess);

        if (string.IsNullOrEmpty(condition.DateFrom))
            throw new ArgumentException("DateFrom value is required.");

        var filterValue = ParseDateValue(condition.DateFrom, propertyType);
        var dateFromExpression = Expression.Constant(filterValue, propertyType);

        return condition.Type switch
        {
            DateFilterOptions.Equals
                => Expression.Lambda<Func<T, bool>>(
                    Expression.Equal(propertyExpression, dateFromExpression), parameter),
            DateFilterOptions.NotEquals
                => Expression.Lambda<Func<T, bool>>(
                    Expression.NotEqual(propertyExpression, dateFromExpression), parameter),
            DateFilterOptions.LessThan
                => Expression.Lambda<Func<T, bool>>(
                    Expression.LessThan(propertyExpression, dateFromExpression), parameter),
            DateFilterOptions.GreaterThan
                => Expression.Lambda<Func<T, bool>>(
                    Expression.GreaterThan(propertyExpression, dateFromExpression), parameter),
            _ => throw new NotSupportedException($"Date filter type '{condition.Type}' is not supported.")
        };
    }

    private static object ParseDateValue(string filterValue, Type propertyType)
    {
        if (propertyType == typeof(DateTimeOffset) || propertyType == typeof(DateTimeOffset?))
            return DateTimeOffset.Parse(filterValue);
        if (propertyType == typeof(DateTime) || propertyType == typeof(DateTime?))
            return DateTime.Parse(filterValue);
        if (propertyType == typeof(DateOnly) || propertyType == typeof(DateOnly?))
            return DateOnly.FromDateTime(DateTime.Parse(filterValue));
        if (propertyType == typeof(TimeOnly) || propertyType == typeof(TimeOnly?))
            return TimeOnly.Parse(filterValue);
        if (propertyType == typeof(TimeSpan) || propertyType == typeof(TimeSpan?))
            return TimeSpan.Parse(filterValue);

        return Convert.ChangeType(filterValue, propertyType);
    }
}
