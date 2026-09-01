using IdentityApi.Domain.Validation;

namespace IdentityApi.Domain.Tests;

public class CpfValidatorTests
{
    [Theory]
    [InlineData("529.982.247-25")] // CPF válido (algoritmo padrão), com pontuação
    [InlineData("52998224725")] // mesmo CPF, só dígitos
    public void IsValid_CpfComDigitosVerificadoresCorretos_RetornaTrue(string cpf)
    {
        Assert.True(CpfValidator.IsValid(cpf));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("123")] // curto demais
    [InlineData("123.456.789-99")] // dígitos verificadores errados
    [InlineData("111.111.111-11")] // todos os dígitos iguais — matematicamente "válido" no algoritmo, mas CPF real nunca é assim
    [InlineData("abc.def.ghi-jk")] // não numérico
    public void IsValid_CpfInvalido_RetornaFalse(string? cpf)
    {
        Assert.False(CpfValidator.IsValid(cpf));
    }

    [Fact]
    public void OnlyDigits_RemovePontuacao()
    {
        Assert.Equal("52998224725", CpfValidator.OnlyDigits("529.982.247-25"));
    }
}
