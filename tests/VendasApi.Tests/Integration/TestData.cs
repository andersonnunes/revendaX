namespace VendasApi.Tests.Integration;

/// <summary>Corpo de POST /veiculos usado pelos testes de integração.</summary>
public record VeiculoRequestDto(string Marca, string Modelo, int Ano, string Cor, decimal Preco, string Placa);

/// <summary>
/// Geração de massa de dados válida para os testes de veículo (placa única por chamada, no
/// padrão Mercosul) — os testes desta suíte compartilham um único Postgres (ver
/// <see cref="VendasApiTestEnvironment"/>), então placas precisam ser únicas entre testes,
/// não só dentro de um mesmo teste.
/// </summary>
public static class TestData
{
    public static VeiculoRequestDto NovoVeiculoValido() => new(
        Marca: "Fiat",
        Modelo: "Argo",
        Ano: 2024,
        Cor: "Branco",
        Preco: 89900.00m,
        Placa: GerarPlacaValida());

    /// <summary>Placa no padrão Mercosul (AAA9A99) — aleatória, praticamente sem risco de colisão numa suíte de teste.</summary>
    public static string GerarPlacaValida()
    {
        var random = Random.Shared;
        char Letra() => (char)('A' + random.Next(26));
        int Digito() => random.Next(10);

        return $"{Letra()}{Letra()}{Letra()}{Digito()}{Letra()}{Digito()}{Digito()}";
    }
}
