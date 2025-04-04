using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

public class Mapper : IMapper
{
    private readonly Dictionary<(Type, Type), Delegate> _compiledMappings = new();
    private readonly Dictionary<(Type, Type), Action<object, object>> _mappings = new();
    private readonly Dictionary<(Type, Type, string), Func<object, object>> _customConverters = new();
    private readonly HashSet<string> _ignoredProperties = new();
    private readonly List<Func<object, bool>> _validators = new();

    public void CreateMap<TSource, TTarget>()
    {
        var sourceType = typeof(TSource);
        var targetType = typeof(TTarget);

        // Compile mapping
        var parameterSource = Expression.Parameter(sourceType, "source");
        var parameterTarget = Expression.Parameter(targetType, "target");

        var bindings = new List<Expression>();
        foreach (var sourceProperty in sourceType.GetProperties())
        {
            if (_ignoredProperties.Contains(sourceProperty.Name)) continue;

            var targetProperty = targetType.GetProperty(sourceProperty.Name);
            if (targetProperty != null && targetProperty.CanWrite)
            {
                var sourceValue = Expression.Property(parameterSource, sourceProperty);
                var targetValue = Expression.Property(parameterTarget, targetProperty);
                var assignment = Expression.Assign(targetValue, sourceValue);
                bindings.Add(assignment);
            }
        }

        var body = Expression.Block(bindings);
        var lambda = Expression.Lambda<Action<TSource, TTarget>>(body, parameterSource, parameterTarget);
        var compiledLambda = lambda.Compile();

        _compiledMappings[(sourceType, targetType)] = compiledLambda;

        // Add reverse mapping for bidirectional support
        CreateReverseMap<TSource, TTarget>();
    }

    public void CreateReverseMap<TSource, TTarget>()
    {
        var sourceType = typeof(TSource);
        var targetType = typeof(TTarget);

        if (!_compiledMappings.ContainsKey((sourceType, targetType)))
            throw new InvalidOperationException("Reverse mapping cannot be created because no forward mapping exists.");

        CreateMap<TTarget, TSource>();
    }

    public void IgnoreProperty<TSource>(Expression<Func<TSource, object>> propertyExpression)
    {
        var propertyName = GetPropertyName(propertyExpression);
        _ignoredProperties.Add(propertyName);
    }

    public void AddValidator(Func<object, bool> validator)
    {
        _validators.Add(validator);
    }

    public TTarget Map<TSource, TTarget>(TSource source) where TTarget : new()
    {
        if (!Validate(source))
        {
            throw new InvalidOperationException("Validation failed for the source object.");
        }

        var sourceType = typeof(TSource);
        var targetType = typeof(TTarget);

        if (!_compiledMappings.ContainsKey((sourceType, targetType)))
        {
            throw new InvalidOperationException($"Mapping not found for {sourceType.Name} to {targetType.Name}");
        }

        var target = new TTarget();
        var mappingFunc = (Action<TSource, TTarget>)_compiledMappings[(sourceType, targetType)];
        mappingFunc(source, target);
        return target;
    }

    private bool Validate(object source)
    {
        foreach (var validator in _validators)
        {
            if (!validator(source))
            {
                return false;
            }
        }
        return true;
    }

    public void ForMember<TSource, TTarget, TProperty>(
        Expression<Func<TTarget, TProperty>> targetExpression,
        Func<object, object> converter)
    {
        var targetPropertyName = GetPropertyName(targetExpression);
        var sourceType = typeof(TSource);
        var targetType = typeof(TTarget);
        var key = (sourceType, targetType, targetPropertyName);
        _customConverters[key] = converter;
    }

    private string GetPropertyName<T, TProperty>(Expression<Func<T, TProperty>> expression)
    {
        if (expression.Body is MemberExpression memberExpression)
        {
            return memberExpression.Member.Name;
        }

        if (expression.Body is UnaryExpression unaryExpression &&
            unaryExpression.Operand is MemberExpression operand)
        {
            return operand.Member.Name;
        }

        throw new ArgumentException("Invalid expression for property.");
    }
}
