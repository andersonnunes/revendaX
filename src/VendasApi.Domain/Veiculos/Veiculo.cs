using VendasApi.Domain.Exceptions;
using VendasApi.Domain.Validation;

namespace VendasApi.Domain.Veiculos;

/// <summary>
/// Agregado do catálogo de veículos. Gera o próprio `Id`/`CriadoEm` — diferente do
/// `identity-api` (onde o Keycloak, um sistema externo, gera o id do cliente), aqui o
/// `vendas-api` é dono do próprio banco, então cabe ao agregado se auto-identificar.
/// </summary>
public class Veiculo
{
    private const int AnoMinimo = 1950;

    public Guid Id { get; private set; }
    public string Marca { get; private set; } = string.Empty;
    public string Modelo { get; private set; } = string.Empty;
    public int Ano { get; private set; }
    public string Cor { get; private set; } = string.Empty;
    public decimal Preco { get; private set; }
    public string Placa { get; private set; } = string.Empty;
    public StatusVeiculo Status { get; private set; }

    /// <summary>Visibilidade do registro (US2.5) — independente de <see cref="Status"/>, ver doc-comment de <see cref="StatusVeiculo"/>.</summary>
    public bool Ativo { get; private set; }

    public DateTimeOffset CriadoEm { get; private set; }

    private Veiculo()
    {
        // Construtor exigido pelo EF Core para materializar entidades via reflexão — nunca
        // chamado pelo código da aplicação (ver Cadastrar, o único ponto de criação válido).
    }

    /// <summary>
    /// Cadastra um veículo novo — sempre <see cref="StatusVeiculo.Disponivel"/> e
    /// <see cref="Ativo"/> (US2.1). Valida ano/preço/placa (regra de negócio, mapeada para
    /// 422 pelo <c>DomainExceptionHandler</c>) — unicidade de placa não é responsabilidade
    /// deste método porque exige consultar o banco (ver `CadastrarVeiculoUseCase`).
    /// </summary>
    public static Veiculo Cadastrar(string marca, string modelo, int ano, string cor, decimal preco, string placa)
    {
        ValidarAnoEPreco(ano, preco);

        var placaNormalizada = PlacaValidator.Normalizar(placa);
        if (!PlacaValidator.IsValid(placaNormalizada))
        {
            throw new PlacaInvalidaException();
        }

        return new Veiculo
        {
            Id = Guid.NewGuid(),
            Marca = marca,
            Modelo = modelo,
            Ano = ano,
            Cor = cor,
            Preco = preco,
            Placa = placaNormalizada,
            Status = StatusVeiculo.Disponivel,
            Ativo = true,
            CriadoEm = DateTimeOffset.UtcNow,
        };
    }

    /// <summary>
    /// Atualiza marca/modelo/ano/cor/preço (US2.2) — `Placa` e `Status` são imutáveis por
    /// este método de propósito (ver refinamento da US2.2: são identidade e ciclo de vida do
    /// veículo, não "dados" no sentido desta operação). Bloqueia veículo `Vendido` — conflito
    /// de estado (409), não erro de validação de entrada (422): por isso é checado antes da
    /// validação de ano/preço, não junto dela.
    /// </summary>
    public void AtualizarDados(string marca, string modelo, int ano, string cor, decimal preco)
    {
        if (Status == StatusVeiculo.Vendido)
        {
            throw new VeiculoVendidoException();
        }

        ValidarAnoEPreco(ano, preco);

        Marca = marca;
        Modelo = modelo;
        Ano = ano;
        Cor = cor;
        Preco = preco;
    }

    private static void ValidarAnoEPreco(int ano, decimal preco)
    {
        var anoMaximo = DateTimeOffset.UtcNow.Year + 1;
        if (ano < AnoMinimo || ano > anoMaximo)
        {
            throw new AnoInvalidoException();
        }

        if (preco <= 0)
        {
            throw new PrecoInvalidoException();
        }
    }
}
