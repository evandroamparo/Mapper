using Xunit;
using QuickMapper.Core;
using System;
using System.Linq.Expressions;
using Moq;
using Microsoft.Extensions.Logging;

namespace QuickMapper.Tests
{
    public class Source
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class Destination
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class NestedSource
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public SourceAddress Address { get; set; }
    }

    public class SourceAddress
    {
        public string Street { get; set; }
        public string City { get; set; }
    }

    public class NestedDestination
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DestinationAddress Address { get; set; }
    }

    public class DestinationAddress
    {
        public string Street { get; set; }
        public string City { get; set; }
    }

    public class SourceCollection
    {
        public int Id { get; set; }
        public List<SourceItem> Items { get; set; }
    }

    public class DestinationCollection
    {
        public int Id { get; set; }
        public List<DestinationItem> Items { get; set; }
    }

    public class SourceItem
    {
        public int Id { get; set; }
        public string Description { get; set; }
    }

    public class DestinationItem
    {
        public int Id { get; set; }
        public string Description { get; set; }
    }


    public class MapperTests
    {
        private readonly IMapper _mapper;
        private readonly Mock<ILogger<Mapper>> _loggerMock;

        public MapperTests()
        {
            _loggerMock = new Mock<ILogger<Mapper>>();
            _mapper = new Mapper(_loggerMock.Object);
        }

        [Fact]
        public void Should_Map_Simple_Properties()
        {
            _mapper.CreateMap<Source, Destination>();

            var source = new Source { Id = 1, Name = "Test" };
            var destination = _mapper.Map<Source, Destination>(source);

            Assert.Equal(source.Id, destination.Id);
            Assert.Equal(source.Name, destination.Name);
        }

        [Fact]
        public void Should_Map_Complex_Nested_Objects()
        {
            _mapper.CreateMap<NestedSource, NestedDestination>();
            _mapper.CreateMap<SourceAddress, DestinationAddress>();

            var source = new NestedSource
            {
                Id = 1,
                Name = "Test",
                Address = new SourceAddress { Street = "Street 1", City = "Test City" }
            };

            var destination = _mapper.Map<NestedSource, NestedDestination>(source);

            Assert.Equal(source.Id, destination.Id);
            Assert.Equal(source.Name, destination.Name);
            Assert.Equal(source.Address.Street, destination.Address.Street);
            Assert.Equal(source.Address.City, destination.Address.City);
        }

        [Fact]
        public void Should_Map_Collections()
        {
            _mapper.CreateMap<SourceCollection, DestinationCollection>();
            _mapper.CreateMap<SourceItem, DestinationItem>();

            var source = new SourceCollection
            {
                Id = 1,
                Items =
            [
                new SourceItem { Id = 101, Description = "Item 1" },
                new SourceItem { Id = 102, Description = "Item 2" }
            ]
            };

            var destination = _mapper.Map<SourceCollection, DestinationCollection>(source);

            Assert.Equal(source.Id, destination.Id);
            Assert.Equal(source.Items.Count, destination.Items.Count);
            Assert.Equal(source.Items[0].Id, destination.Items[0].Id);
            Assert.Equal(source.Items[0].Description, destination.Items[0].Description);
        }

        [Fact]
        public void Should_Apply_Custom_Converters()
        {
            _mapper.CreateMap<Source, Destination>();
            _mapper.ForMember<Source, Destination, string>(dest => dest.Name, value => value.Name.ToUpper());

            var source = new Source { Id = 1, Name = "Test" };
            var destination = _mapper.Map<Source, Destination>(source);

            Assert.Equal(source.Id, destination.Id);
            Assert.Equal("TEST", destination.Name);
        }

        [Fact]
        public void Should_Skip_Ignored_Properties()
        {
            _mapper.IgnoreProperty<Source>(f => f.Description);
            _mapper.CreateMap<Source, Destination>();

            var source = new Source { Id = 1, Name = "Test", Description = "Ignore" };
            var destination = _mapper.Map<Source, Destination>(source);

            Assert.Equal(source.Id, destination.Id);
            Assert.Equal(source.Name, destination.Name);
            Assert.Null(destination.Description);
        }

        [Fact]
        public void Should_Validate_Before_Mapping()
        {
            _mapper.AddValidator<Source>(source =>
            {
                return source != null && source.Id > 0 && !string.IsNullOrEmpty(source.Name);
            });

            _mapper.CreateMap<Source, Destination>();

            var validSource = new Source { Id = 1, Name = "Test" };
            var invalidSource = new Source { Id = 0, Name = "" };

            var destination = _mapper.Map<Source, Destination>(validSource);
            Assert.Equal(validSource.Name, destination.Name);

            Assert.Throws<InvalidOperationException>(() => _mapper.Map<Source, Destination>(invalidSource));
        }

        [Fact]
        public void Should_Create_Bidirectional_Mappings()
        {
            _mapper.CreateMap<Source, Destination>();
            _mapper.CreateReverseMap<Source, Destination>();

            var source = new Source { Id = 1, Name = "Test" };
            var destination = _mapper.Map<Source, Destination>(source);

            Assert.Equal(source.Id, destination.Id);
            Assert.Equal(source.Name, destination.Name);

            var reverseSource = _mapper.Map<Destination, Source>(destination);
            Assert.Equal(destination.Id, reverseSource.Id);
            Assert.Equal(destination.Name, reverseSource.Name);
        }

        [Fact]
        public void Should_Throw_Error_For_Invalid_Mappings()
        {
            _mapper.CreateMap<Source, Destination>();

            Assert.Throws<InvalidOperationException>(() => _mapper.Map<Destination, Source>(new Destination()));
        }

        [Fact]
        public void Map_WithValidConfiguration_ShouldMapCorrectly()
        {
            // Arrange
            var source = new Source { Id = 1, Name = "Test", Description = "Test Description" };
            _mapper.CreateMap<Source, Destination>();

            // Act
            var result = _mapper.Map<Source, Destination>(source);

            // Assert
            Assert.Equal(source.Id, result.Id);
            Assert.Equal(source.Name, result.Name);
            Assert.Equal(source.Description, result.Description);
        }

        [Fact]
        public void Map_WithNullSource_ShouldThrowArgumentNullException()
        {
            // Arrange
            _mapper.CreateMap<Source, Destination>();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _mapper.Map<Source, Destination>(null));
        }

        [Fact]
        public void Map_WithoutConfiguration_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var source = new Source();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _mapper.Map<Source, Destination>(source));
        }

        [Fact]
        public void Map_WithIgnoredProperty_ShouldNotMapProperty()
        {
            // Arrange
            var source = new Source { Id = 1, Name = "Test", Description = "Test Description" };
            _mapper.CreateMap<Source, Destination>();
            _mapper.IgnoreProperty<Source>(s => s.Description);

            // Act
            var result = _mapper.Map<Source, Destination>(source);

            // Assert
            Assert.Equal(source.Id, result.Id);
            Assert.Equal(source.Name, result.Name);
            Assert.Null(result.Description);
        }

        [Fact]
        public void Should_Keep_Properties_Ignored_Even_With_Custom_Mapping()
        {
            // Arrange
            _mapper.IgnoreProperty<Source>(s => s.Description);
            _mapper.CreateMap<Source, Destination>();
            _mapper.ForMember<Source, Destination, string>(
                dest => dest.Description,
                value => "Custom Value"
            );

            var source = new Source { Id = 1, Name = "Test", Description = "Should Be Ignored" };

            // Act
            var destination = _mapper.Map<Source, Destination>(source);

            // Assert
            Assert.Equal(source.Id, destination.Id);
            Assert.Equal(source.Name, destination.Name);
            Assert.Null(destination.Description);  // Should remain null despite custom mapping
        }

        [Fact]
        public void Should_Keep_Properties_Ignored_When_Added_After_Custom_Mapping()
        {
            // Arrange
            _mapper.CreateMap<Source, Destination>();
            _mapper.ForMember<Source, Destination, string>(
                dest => dest.Description,
                value => "Custom Value"
            );
            _mapper.IgnoreProperty<Source>(s => s.Description);  // Ignore after custom mapping

            var source = new Source { Id = 1, Name = "Test", Description = "Should Be Ignored" };

            // Act
            var destination = _mapper.Map<Source, Destination>(source);

            // Assert
            Assert.Equal(source.Id, destination.Id);
            Assert.Equal(source.Name, destination.Name);
            Assert.Null(destination.Description);  // Should be null as ignore takes precedence
        }
    }
}
