using Xunit;
using QuickMapper.Core;
using System;
using Moq;
using Microsoft.Extensions.Logging;

namespace QuickMapper.Tests
{
    public class SourceWithValidation
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class DestinationWithConversion
    {
        public int Id { get; set; }
        public string UpperName { get; set; }
    }

    public class MapperLoggingTests
    {
        private readonly IMapper _mapper;
        private readonly Mock<ILogger<Mapper>> _loggerMock;

        public MapperLoggingTests()
        {
            _loggerMock = new Mock<ILogger<Mapper>>();
            _mapper = new Mapper(_loggerMock.Object);
        }

        [Fact]
        public void Map_WithValidator_ShouldValidateAndLogFailure()
        {
            // Arrange
            var source = new SourceWithValidation { Id = -1, Name = "Test" };
            _mapper.CreateMap<SourceWithValidation, DestinationWithConversion>();
            _mapper.AddValidator<SourceWithValidation>(source => source.Id > 0);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                _mapper.Map<SourceWithValidation, DestinationWithConversion>(source));

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Validation failed")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()
                ),
                Times.Once);
        }

        [Fact]
        public void Map_WithCustomConverter_ShouldLogConversionAndSucceed()
        {
            // Arrange
            var source = new SourceWithValidation { Id = 1, Name = "test" };
            _mapper.CreateMap<SourceWithValidation, DestinationWithConversion>();
            _mapper.ForMember<SourceWithValidation, DestinationWithConversion, string>(
                d => d.UpperName,
                src => ((SourceWithValidation)src).Name.ToUpper()
            );

            // Act
            var result = _mapper.Map<SourceWithValidation, DestinationWithConversion>(source);

            // Assert
            Assert.Equal("TEST", result.UpperName);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Successfully mapped")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()
                ),
                Times.Once);
        }

        [Fact]
        public void Map_WithMultipleValidators_ShouldLogAllValidationFailures()
        {
            // Arrange
            var source = new SourceWithValidation { Id = 1, Name = null };
            _mapper.CreateMap<SourceWithValidation, DestinationWithConversion>();
            _mapper.AddValidator<SourceWithValidation>(source => source.Id > 0);
            _mapper.AddValidator<SourceWithValidation>(source => !string.IsNullOrEmpty(source.Name));

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                _mapper.Map<SourceWithValidation, DestinationWithConversion>(source));

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Validation failed")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()
                ),
                Times.Once);
        }

        [Fact]
        public void Map_BasicMapping_ShouldLogSuccess()
        {
            // Arrange
            var source = new SourceWithValidation { Id = 1, Name = "Test" };
            _mapper.CreateMap<SourceWithValidation, DestinationWithConversion>();

            // Act
            var result = _mapper.Map<SourceWithValidation, DestinationWithConversion>(source);

            // Assert
            Assert.NotNull(result);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Successfully mapped")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()
                ),
                Times.Once);
        }

        [Fact]
        public void Map_WithIgnoredProperty_ShouldLogIgnoredProperty()
        {
            // Arrange
            var source = new SourceWithValidation { Id = 1, Name = "Test", Description = "Ignore me" };
            _mapper.CreateMap<SourceWithValidation, DestinationWithConversion>();
            _mapper.IgnoreProperty<SourceWithValidation>(s => s.Description);

            // Act
            var result = _mapper.Map<SourceWithValidation, DestinationWithConversion>(source);

            // Assert
            Assert.NotNull(result);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Property Description marked as ignored")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()
                ),
                Times.Once);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Successfully mapped")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()
                ),
                Times.Once);
        }
    }
}
