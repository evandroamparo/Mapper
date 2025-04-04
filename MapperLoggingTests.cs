using System;
using System.IO;
using Xunit;

public class MapperLoggingTests
{
    private readonly Mapper _mapper;
    private readonly string _logFilePath = "test_mapper_logs.txt";

    public MapperLoggingTests()
    {
        _mapper = new Mapper
        {
            LogFilePath = _logFilePath
        };

        // Limpa os logs antes de cada teste
        if (File.Exists(_logFilePath))
        {
            File.Delete(_logFilePath);
        }
    }

    [Fact]
    public void Should_Log_Successful_Mapping()
    {
        _mapper.CreateMap<Fonte, Destino>();

        var fonte = new Fonte { Id = 1, Nome = "Teste" };
        var destino = _mapper.Map<Fonte, Destino>(fonte);

        Assert.True(File.Exists(_logFilePath));

        var logs = File.ReadAllLines(_logFilePath);
        Assert.Contains("Mapping created for Fonte -> Destino", logs);
        Assert.Contains("Starting mapping for Fonte -> Destino", logs);
        Assert.Contains("Mapping completed for Fonte -> Destino", logs);
    }

    [Fact]
    public void Should_Log_Ignored_Properties()
    {
        _mapper.IgnoreProperty<Fonte>(f => f.Descricao);
        _mapper.CreateMap<Fonte, Destino>();

        var fonte = new Fonte { Id = 1, Nome = "Teste", Descricao = "Ignorar" };
        var destino = _mapper.Map<Fonte, Destino>(fonte);

        Assert.True(File.Exists(_logFilePath));

        var logs = File.ReadAllLines(_logFilePath);
        Assert.Contains("Property Descricao does not exist or is not writable in the target.", logs);
    }

    [Fact]
    public void Should_Log_Custom_Converter_Usage()
    {
        _mapper.CreateMap<Fonte, Destino>();
        _mapper.ForMember<Fonte, Destino, string>(dest => dest.Nome, value => value.ToString().ToUpper());

        var fonte = new Fonte { Id = 1, Nome = "Teste" };
        var destino = _mapper.Map<Fonte, Destino>(fonte);

        Assert.True(File.Exists(_logFilePath));

        var logs = File.ReadAllLines(_logFilePath);
        Assert.Contains("Custom converter added for Nome", logs);
        Assert.Contains("Mapped Nome with custom converter.", logs);
    }

    [Fact]
    public void Should_Log_Mapping_Errors()
    {
        // Criar mapeamento inválido para simular erro
        var fonte = new Fonte { Id = 1, Nome = "Teste" };

        var exception = Assert.Throws<InvalidOperationException>(() => _mapper.Map<Fonte, Destino>(fonte));

        Assert.Equal("Mapping not found for Fonte to Destino", exception.Message);

        Assert.True(File.Exists(_logFilePath));

        var logs = File.ReadAllLines(_logFilePath);
        Assert.Contains("Mapping not found for Fonte to Destino", logs);
    }
}
