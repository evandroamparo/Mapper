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
                    if (src == null || dest == null) return;

                    foreach (var prop in typeof(TDestination).GetProperties())
                    {
                        if (_ignoredProperties.Contains(prop.Name))
                            continue;

                        var sourceProp = typeof(TSource).GetProperty(prop.Name);
                        if (sourceProp != null && prop.CanWrite)
                        {
                            var value = sourceProp.GetValue(src);
                            if (value == null)
                            {
                                prop.SetValue(dest, null);
                                continue;
                            }

                            // Check for custom mapping
                            if (_memberMappings.ContainsKey(key) && _memberMappings[key].ContainsKey(prop.Name))
                            {
                                var convertedValue = _memberMappings[key][prop.Name](src);
                                prop.SetValue(dest, convertedValue);
                                continue;
                            }

                            // Handle collections
                            if (IsCollectionType(sourceProp.PropertyType) && IsCollectionType(prop.PropertyType))
                            {
                                var destCollection = CreateDestinationCollection(value, prop.PropertyType);
                                prop.SetValue(dest, destCollection);
                                continue;
                            }

                            // Handle nested objects
                            if (!IsSimpleType(sourceProp.PropertyType))
                            {
                                var nestedKey = (sourceProp.PropertyType, prop.PropertyType);
                                if (_mappings.ContainsKey(nestedKey))
                                {
                                    var nestedInstance = Activator.CreateInstance(prop.PropertyType);
                                    if (nestedInstance != null)
                                    {
                                        _mappings[nestedKey](value, nestedInstance);
                                        prop.SetValue(dest, nestedInstance);
                                    }
                                    continue;
                                }
                            }

                            // Simple property mapping
                            prop.SetValue(dest, value);
                        }
                    }
                };
                _memberMappings[key] = new Dictionary<string, Func<object, object>>();
            }
        }

        private bool IsSimpleType(Type type)
        {
            return type.IsPrimitive || 
                   type.IsEnum || 
                   type == typeof(string) || 
                   type == typeof(decimal) || 
                   type == typeof(DateTime);
        }

        private bool IsCollectionType(Type type)
        {
            return type.IsGenericType && 
                   (type.GetGenericTypeDefinition() == typeof(List<>) ||
                    type.GetGenericTypeDefinition() == typeof(IList<>) ||
                    type.GetGenericTypeDefinition() == typeof(ICollection<>));
        }

        private object CreateDestinationCollection(object sourceCollection, Type destinationType)
        {
            var sourceList = (System.Collections.IEnumerable)sourceCollection;
            var destListType = destinationType;
            
            if (destinationType.IsInterface)
            {
                destListType = typeof(List<>).MakeGenericType(destinationType.GetGenericArguments()[0]);
            }

            var destList = (System.Collections.IList)Activator.CreateInstance(destListType);
            var elementType = destinationType.GetGenericArguments()[0];

            foreach (var item in sourceList)
            {
                if (item == null) continue;

                var sourceType = item.GetType();
                var itemKey = (sourceType, elementType);

                if (_mappings.ContainsKey(itemKey))
                {
                    var destItem = Activator.CreateInstance(elementType);
                    if (destItem != null)
                    {
                        _mappings[itemKey](item, destItem);
                        destList.Add(destItem);
                    }
                }
                else
                {
                    destList.Add(item);
                }
            }

            return destList;
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
            if (!_memberMappings.ContainsKey(key))
            {
                _memberMappings[key] = new Dictionary<string, Func<object, object>>();
            }
            
            var memberName = ((MemberExpression)destinationMember.Body).Member.Name;
            var sourceType = typeof(TSource);
            _memberMappings[key][memberName] = src => 
            {
                var sourceProp = sourceType.GetProperty(memberName);
                if (sourceProp != null)
                {
                    var value = sourceProp.GetValue(src);
                    return mapFrom(value);
                }
                return mapFrom(src);
            };
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

            // Only apply custom mappings for non-ignored properties
            foreach (var memberMapping in _memberMappings[key])
            {
                if (!_ignoredProperties.Contains(memberMapping.Key))
                {
                    var property = typeof(TDestination).GetProperty(memberMapping.Key);
                    if (property != null)
                    {
                        property.SetValue(destination, memberMapping.Value(source));
                    }
                }
            }

            _logger.LogInformation("Successfully mapped {SourceType} to {DestType}", typeof(TSource).Name, typeof(TDestination).Name);
            return destination;
        }
    }
}