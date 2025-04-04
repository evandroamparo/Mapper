using System;
using System.Collections.Generic;
using Xunit;

public class MapperTests
{
    private readonly Mapper _mapper;

    public MapperTests()
    {
        _mapper = new Mapper();
    }

    [Fact]
    public void Should_Map_Simple_Properties()
    {
        _mapper.CreateMap<Fonte, Destino>();

        var fonte = new Fonte { Id = 1, Nome = "Teste" };
        var destino = _mapper.Map<Fonte, Destino>(fonte);

        Assert.Equal(fonte.Id, destino.Id);
        Assert.Equal(fonte.Nome, destino.Nome);
    }

    [Fact]
    public void Should_Map_Complex_Nested_Objects()
    {
        _mapper.CreateMap<FonteAninhada, DestinoAninhado>();
        _mapper.CreateMap<EnderecoFonte, EnderecoDestino>();

        var fonte = new FonteAninhada
        {
            Id = 1,
            Nome = "Teste",
            Endereco = new EnderecoFonte { Rua = "Rua 1", Cidade = "Cidade Teste" }
        };

        var destino = _mapper.Map<FonteAninhada, DestinoAninhado>(fonte);

        Assert.Equal(fonte.Id, destino.Id);
        Assert.Equal(fonte.Nome, destino.Nome);
        Assert.Equal(fonte.Endereco.Rua, destino.Endereco.Rua);
        Assert.Equal(fonte.Endereco.Cidade, destino.Endereco.Cidade);
    }

    [Fact]
    public void Should_Map_Collections()
    {
        _mapper.CreateMap<FonteColecao, DestinoColecao>();
        _mapper.CreateMap<ItemFonte, ItemDestino>();

        var fonte = new FonteColecao
        {
            Id = 1,
            Itens = new List<ItemFonte>
            {
                new ItemFonte { Codigo = 101, Descricao = "Item 1" },
                new ItemFonte { Codigo = 102, Descricao = "Item 2" }
            }
        };

        var destino = _mapper.Map<FonteColecao, DestinoColecao>(fonte);

        Assert.Equal(fonte.Id, destino.Id);
        Assert.Equal(fonte.Itens.Count, destino.Itens.Count);
        Assert.Equal(fonte.Itens[0].Codigo, destino.Itens[0].Codigo);
        Assert.Equal(fonte.Itens[0].Descricao, destino.Itens[0].Descricao);
    }

    [Fact]
    public void Should_Apply_Custom_Converters()
    {
        _mapper.CreateMap<Fonte, Destino>();
        _mapper.ForMember<Fonte, Destino, string>(dest => dest.Nome, value => value.ToString().ToUpper());

        var fonte = new Fonte { Id = 1, Nome = "Teste" };
        var destino = _mapper.Map<Fonte, Destino>(fonte);

        Assert.Equal(fonte.Id, destino.Id);
        Assert.Equal("TESTE", destino.Nome);
    }

    [Fact]
    public void Should_Skip_Ignored_Properties()
    {
        _mapper.IgnoreProperty<Fonte>(f => f.Descricao);
        _mapper.CreateMap<Fonte, Destino>();

        var fonte = new Fonte { Id = 1, Nome = "Teste", Descricao = "Ignorar" };
        var destino = _mapper.Map<Fonte, Destino>(fonte);

        Assert.Null(destino.Descricao);
    }

    [Fact]
    public void Should_Validate_Before_Mapping()
    {
        _mapper.AddValidator(obj =>
        {
            var fonte = obj as Fonte;
            return fonte != null && fonte.Id > 0 && !string.IsNullOrEmpty(fonte.Nome);
        });

        _mapper.CreateMap<Fonte, Destino>();

        var fonteValida = new Fonte { Id = 1, Nome = "Teste" };
        var fonteInvalida = new Fonte { Id = 0, Nome = "" };

        var destino = _mapper.Map<Fonte, Destino>(fonteValida);
        Assert.Equal(fonteValida.Nome, destino.Nome);

        Assert.Throws<InvalidOperationException>(() => _mapper.Map<Fonte, Destino>(fonteInvalida));
    }

    [Fact]
    public void Should_Create_Bidirectional_Mappings()
    {
        _mapper.CreateMap<Fonte, Destino>();
        _mapper.CreateReverseMap<Fonte, Destino>();

        var fonte = new Fonte { Id = 1, Nome = "Teste" };
        var destino = _mapper.Map<Fonte, Destino>(fonte);

        Assert.Equal(fonte.Id, destino.Id);
        Assert.Equal(fonte.Nome, destino.Nome);

        var fonteReverso = _mapper.Map<Destino, Fonte>(destino);
        Assert.Equal(destino.Id, fonteReverso.Id);
        Assert.Equal(destino.Nome, fonteReverso.Nome);
    }

    [Fact]
    public void Should_Throw_Error_For_Invalid_Mappings()
    {
        _mapper.CreateMap<Fonte, Destino>();

        Assert.Throws<InvalidOperationException>(() => _mapper.Map<Destino, Fonte>(new Destino()));
    }
}

public class Fonte
{
    public int Id { get; set; }
    public string Nome { get; set; }
    public string Descricao { get; set; }
}

public class Destino
{
    public int Id { get; set; }
    public string Nome { get; set; }
    public string Descricao { get; set; }
}

public class FonteAninhada
{
    public int Id { get; set; }
    public string Nome { get; set; }
    public EnderecoFonte Endereco { get; set; }
}

public class DestinoAninhado
{
    public int Id { get; set; }
    public string Nome { get; set; }
    public EnderecoDestino Endereco { get; set; }
}

public class EnderecoFonte
{
    public string Rua { get; set; }
    public string Cidade { get; set; }
}

public class EnderecoDestino
{
    public string Rua { get; set; }
    public string Cidade { get; set; }
}

public class FonteColecao
{
    public int Id { get; set; }
    public List<ItemFonte> Itens { get; set; }
}

public class DestinoColecao
{
    public int Id { get; set; }
    public List<ItemDestino> Itens { get; set; }
}

public class ItemFonte
{
    public int Codigo { get; set; }
    public string Descricao { get; set; }
}

public class ItemDestino
{
    public int Codigo { get; set; }
    public string Descricao { get; set; }
}
