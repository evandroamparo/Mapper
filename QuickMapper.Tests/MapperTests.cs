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
    }
}
