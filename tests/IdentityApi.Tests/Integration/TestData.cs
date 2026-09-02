namespace IdentityApi.Tests.Integration;

/// <summary>Corpo de POST /clientes usado pelos testes de integração.</summary>
public record ClienteRequestDto(string Nome, string Email, string Cpf, string Senha, string? Telefone);

/// <summary>
/// Geração de massa de dados válida para os testes (CPF com dígitos verificadores corretos,
/// e-mail único por chamada) — compartilhado entre <see cref="ClientesControllerTests"/> e
/// <see cref="LoginTests"/> para não duplicar a lógica de geração em cada classe.
/// </summary>
public static class TestData
{
    public static ClienteRequestDto NovoClienteValido() => new(
        Nome: "Maria Silva",
        Email: $"maria.{Guid.NewGuid():N}@example.com",
        Cpf: GerarCpfValido(),
        Senha: "SenhaForte123",
        Telefone: "11999990000");

    /// <summary>
    /// Gera um CPF com dígitos verificadores válidos — não reaproveita
    /// IdentityApi.Domain.Validation.CpfValidator (que só valida, não gera); é geração de
    /// massa de teste, não a lógica sendo testada.
    /// </summary>
    public static string GerarCpfValido()
    {
        var random = Random.Shared;
        int[] digits;
        do
        {
            digits = Enumerable.Range(0, 9).Select(_ => random.Next(0, 10)).ToArray();
        } while (digits.Distinct().Count() == 1);

        var d1 = CalcularDigito(digits, 9);
        var d2 = CalcularDigito([.. digits, d1], 10);
        return string.Concat(digits) + d1 + d2;
    }

    private static int CalcularDigito(int[] numbers, int length)
    {
        var sum = 0;
        for (var i = 0; i < length; i++)
        {
            sum += numbers[i] * (length + 1 - i);
        }

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }
}
