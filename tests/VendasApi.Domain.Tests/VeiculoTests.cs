using VendasApi.Domain.Exceptions;
using VendasApi.Domain.Veiculos;

namespace VendasApi.Domain.Tests;

public class VeiculoTests
{
    [Fact]
    public void Cadastrar_DadosValidos_CriaVeiculoDisponivelEAtivo()
    {
        var veiculo = Veiculo.Cadastrar("Fiat", "Argo", 2024, "Branco", 89900.00m, "ABC1D23");

        Assert.NotEqual(Guid.Empty, veiculo.Id);
        Assert.Equal(StatusVeiculo.Disponivel, veiculo.Status);
        Assert.True(veiculo.Ativo);
        Assert.Equal("ABC1D23", veiculo.Placa);
    }

    [Fact]
    public void Cadastrar_NormalizaPlaca()
    {
        var veiculo = Veiculo.Cadastrar("Fiat", "Argo", 2024, "Branco", 89900.00m, "abc-1d23");

        Assert.Equal("ABC1D23", veiculo.Placa);
    }

    [Theory]
    [InlineData(1949)]
    [InlineData(1800)]
    public void Cadastrar_AnoAnteriorAoMinimo_LancaAnoInvalidoException(int ano)
    {
        Assert.Throws<AnoInvalidoException>(() =>
            Veiculo.Cadastrar("Fiat", "Argo", ano, "Branco", 89900.00m, "ABC1D23"));
    }

    [Fact]
    public void Cadastrar_AnoMuitoNoFuturo_LancaAnoInvalidoException()
    {
        var anoMuitoFuturo = DateTimeOffset.UtcNow.Year + 2;

        Assert.Throws<AnoInvalidoException>(() =>
            Veiculo.Cadastrar("Fiat", "Argo", anoMuitoFuturo, "Branco", 89900.00m, "ABC1D23"));
    }

    [Fact]
    public void Cadastrar_AnoAtualMaisUm_NaoLancaExcecao()
    {
        var proximoAno = DateTimeOffset.UtcNow.Year + 1;

        var veiculo = Veiculo.Cadastrar("Fiat", "Argo", proximoAno, "Branco", 89900.00m, "ABC1D23");

        Assert.Equal(proximoAno, veiculo.Ano);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Cadastrar_PrecoZeroOuNegativo_LancaPrecoInvalidoException(decimal preco)
    {
        Assert.Throws<PrecoInvalidoException>(() =>
            Veiculo.Cadastrar("Fiat", "Argo", 2024, "Branco", preco, "ABC1D23"));
    }

    [Theory]
    [InlineData("AB1234")]
    [InlineData("1234ABC")]
    [InlineData("")]
    public void Cadastrar_PlacaFormatoInvalido_LancaPlacaInvalidaException(string placa)
    {
        Assert.Throws<PlacaInvalidaException>(() =>
            Veiculo.Cadastrar("Fiat", "Argo", 2024, "Branco", 89900.00m, placa));
    }

    [Fact]
    public void AtualizarDados_VeiculoDisponivel_AtualizaCampos()
    {
        var veiculo = Veiculo.Cadastrar("Fiat", "Argo", 2024, "Branco", 89900.00m, "ABC1D23");

        veiculo.AtualizarDados("Fiat", "Argo", 2025, "Prata", 85000.00m);

        Assert.Equal(2025, veiculo.Ano);
        Assert.Equal("Prata", veiculo.Cor);
        Assert.Equal(85000.00m, veiculo.Preco);
    }

    [Fact]
    public void AtualizarDados_NaoAlteraPlacaNemStatus()
    {
        var veiculo = Veiculo.Cadastrar("Fiat", "Argo", 2024, "Branco", 89900.00m, "ABC1D23");

        veiculo.AtualizarDados("Fiat", "Argo", 2025, "Prata", 85000.00m);

        Assert.Equal("ABC1D23", veiculo.Placa);
        Assert.Equal(StatusVeiculo.Disponivel, veiculo.Status);
    }

    [Fact]
    public void AtualizarDados_VeiculoVendido_LancaVeiculoVendidoExceptionENaoAlteraNada()
    {
        var veiculo = Veiculo.Cadastrar("Fiat", "Argo", 2024, "Branco", 89900.00m, "ABC1D23");
        MarcarComoVendido(veiculo);

        Assert.Throws<VeiculoVendidoException>(() =>
            veiculo.AtualizarDados("Fiat", "Mobi", 2025, "Prata", 85000.00m));

        Assert.Equal("Argo", veiculo.Modelo);
        Assert.Equal(89900.00m, veiculo.Preco);
    }

    [Theory]
    [InlineData(1800)]
    public void AtualizarDados_AnoInvalido_LancaAnoInvalidoException(int ano)
    {
        var veiculo = Veiculo.Cadastrar("Fiat", "Argo", 2024, "Branco", 89900.00m, "ABC1D23");

        Assert.Throws<AnoInvalidoException>(() =>
            veiculo.AtualizarDados("Fiat", "Argo", ano, "Branco", 89900.00m));
    }

    [Fact]
    public void AtualizarDados_PrecoInvalido_LancaPrecoInvalidoException()
    {
        var veiculo = Veiculo.Cadastrar("Fiat", "Argo", 2024, "Branco", 89900.00m, "ABC1D23");

        Assert.Throws<PrecoInvalidoException>(() =>
            veiculo.AtualizarDados("Fiat", "Argo", 2024, "Branco", 0m));
    }

    /// <summary>
    /// Não existe (ainda) nenhuma operação pública que leve um veículo a `Vendido` — essa
    /// transição só chega no Épico 3. Reflexão é o jeito honesto de testar o guard clause de
    /// `AtualizarDados` sem esperar o Épico 3 nem vazar um setter de teste na entidade.
    /// </summary>
    private static void MarcarComoVendido(Veiculo veiculo)
    {
        var propriedade = typeof(Veiculo).GetProperty(nameof(Veiculo.Status))!;
        propriedade.SetValue(veiculo, StatusVeiculo.Vendido);
    }
}
