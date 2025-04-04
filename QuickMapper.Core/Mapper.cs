using System.Linq.Expressions;
using Microsoft.Extensions.Logging;

namespace QuickMapper.Core
{
    public interface IMapper
    {
        void CreateMap<TSource, TDestination>();
        void CreateReverseMap<TSource, TDestination>();
        void IgnoreProperty<TSource>(Expression<Func<TSource, object>> propertyExpression);
        void AddValidator(Func<object, bool> validator);
        TDestination Map<TSource, TDestination>(TSource source) where TDestination : new();
        void ForMember<TSource, TDestination, TMember>(
            Expression<Func<TDestination, TMember>> destinationMember,
            Func<object, TMember> mapFrom);
    }

    public class Mapper : IMapper
    {
        private readonly Dictionary<(Type, Type), Action<object, object>> _mappings = new();
        private readonly Dictionary<(Type, Type), Dictionary<string, Func<object, object>>> _memberMappings = new();
        private readonly List<Func<object, bool>> _validators = new();
        private readonly HashSet<string> _ignoredProperties = new();
        private readonly ILogger<Mapper> _logger;

        public Mapper(ILogger<Mapper> logger)
        {
            _logger = logger;
        }

        public void CreateMap<TSource, TDestination>()
        {
            var key = (typeof(TSource), typeof(TDestination));
            if (!_mappings.ContainsKey(key))
            {
                _mappings[key] = (src, dest) =>
                {
                    foreach (var prop in typeof(TDestination).GetProperties())
                    {
                        if (_ignoredProperties.Contains(prop.Name))
                            continue;

                        var sourceProp = typeof(TSource).GetProperty(prop.Name);
                        if (sourceProp != null && prop.CanWrite)
                        {
                            prop.SetValue(dest, sourceProp.GetValue(src));
                        }
                    }
                };
                _memberMappings[key] = new Dictionary<string, Func<object, object>>();
            }
        }

        public void CreateReverseMap<TSource, TDestination>()
        {
            CreateMap<TSource, TDestination>();
            CreateMap<TDestination, TSource>();
        }

        public void IgnoreProperty<TSource>(Expression<Func<TSource, object>> propertyExpression)
        {
            var memberExpr = propertyExpression.Body as MemberExpression 
                           ?? ((UnaryExpression)propertyExpression.Body).Operand as MemberExpression;
            if (memberExpr != null)
            {
                _ignoredProperties.Add(memberExpr.Member.Name);
                _logger.LogInformation("Property {PropertyName} marked as ignored", memberExpr.Member.Name);
            }
        }

        public void AddValidator(Func<object, bool> validator)
        {
            _validators.Add(validator);
        }

        public void ForMember<TSource, TDestination, TMember>(
            Expression<Func<TDestination, TMember>> destinationMember,
            Func<object, TMember> mapFrom)
        {
            var key = (typeof(TSource), typeof(TDestination));
            var memberName = ((MemberExpression)destinationMember.Body).Member.Name;
            _memberMappings[key][memberName] = src => mapFrom(src);
        }

        public TDestination Map<TSource, TDestination>(TSource source) where TDestination : new()
        {
            if (source == null)
            {
                _logger.LogError("Source object is null");
                throw new ArgumentNullException(nameof(source));
            }

            foreach (var validator in _validators)
            {
                if (!validator(source))
                {
                    _logger.LogError("Validation failed for {SourceType}", typeof(TSource).Name);
                    throw new InvalidOperationException("Validation failed");
                }
            }

            var key = (typeof(TSource), typeof(TDestination));
            if (!_mappings.ContainsKey(key))
            {
                _logger.LogError("No mapping defined for {SourceType} to {DestType}", typeof(TSource).Name, typeof(TDestination).Name);
                throw new InvalidOperationException($"No mapping defined for {typeof(TSource)} to {typeof(TDestination)}");
            }

            var destination = new TDestination();
            _mappings[key](source, destination);

            foreach (var memberMapping in _memberMappings[key])
            {
                var property = typeof(TDestination).GetProperty(memberMapping.Key);
                if (property != null)
                {
                    property.SetValue(destination, memberMapping.Value(source));
                }
            }

            _logger.LogInformation("Successfully mapped {SourceType} to {DestType}", typeof(TSource).Name, typeof(TDestination).Name);
            return destination;
        }
    }
}