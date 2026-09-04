using VendasApi.Domain.Validation;

namespace VendasApi.Domain.Tests;

public class PlacaValidatorTests
{
    [Theory]
    [InlineData("ABC1234")]
    [InlineData("abc1234")]
    [InlineData("ABC-1234")]
    public void IsValid_PadraoAntigo_RetornaTrue(string placa)
    {
        Assert.True(PlacaValidator.IsValid(PlacaValidator.Normalizar(placa)));
    }

    [Theory]
    [InlineData("ABC1D23")]
    [InlineData("abc1d23")]
    public void IsValid_PadraoMercosul_RetornaTrue(string placa)
    {
        Assert.True(PlacaValidator.IsValid(PlacaValidator.Normalizar(placa)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("AB1234")]
    [InlineData("ABCD123")]
    [InlineData("ABC12345")]
    [InlineData("1234ABC")]
    public void IsValid_FormatoInvalido_RetornaFalse(string placa)
    {
        Assert.False(PlacaValidator.IsValid(PlacaValidator.Normalizar(placa)));
    }

    [Fact]
    public void Normalizar_RemoveHifenEEspacosEMaiusculiza()
    {
        Assert.Equal("ABC1234", PlacaValidator.Normalizar(" abc-1234 "));
    }

    [Fact]
    public void Normalizar_Nulo_RetornaVazio()
    {
        Assert.Equal(string.Empty, PlacaValidator.Normalizar(null));
    }
}
