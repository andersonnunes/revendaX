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
    /// este método de propósito: são identidade e ciclo de vida do veículo, não "dados" no
    /// sentido desta operação. Bloqueia veículo `Vendido` — conflito
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

    /// <summary>
    /// Soft delete (US2.5) — só permitido em <see cref="StatusVeiculo.Disponivel"/> (mais
    /// restritivo que <see cref="AtualizarDados"/>, que só bloqueia `Vendido`: excluir tira o
    /// veículo inteiramente de vista, o que poderia esconder uma reserva em andamento).
    /// Idempotente por natureza — excluir não altera `Status`, então
    /// chamar de novo num veículo já excluído passa pelo mesmo guard clause e apenas reatribui
    /// `Ativo = false` a um campo que já é `false`, sem efeito colateral novo.
    /// </summary>
    public void Excluir()
    {
        if (Status != StatusVeiculo.Disponivel)
        {
            throw new VeiculoNaoPodeSerExcluidoException();
        }

        Ativo = false;
    }

    /// <summary>
    /// Reserva o veículo para uma compra em andamento (US3.1) — só permitido a partir de
    /// <see cref="StatusVeiculo.Disponivel"/>. Conflito de estado (409), mesmo racional de
    /// <see cref="Excluir"/>: checar o estado é a defesa de primeira linha contra comprar um
    /// veículo já reservado/vendido; a corrida entre duas leituras concorrentes (US3.2) é
    /// fechada em `Infrastructure`, não aqui.
    /// </summary>
    public void Reservar()
    {
        if (Status != StatusVeiculo.Disponivel)
        {
            throw new VeiculoIndisponivelParaCompraException();
        }

        Status = StatusVeiculo.Reservado;
    }

    /// <summary>
    /// Efetiva a venda (US3.3) — só permitido a partir de <see cref="StatusVeiculo.Reservado"/>.
    /// Defesa em profundidade, não um caminho alcançável pelo fluxo normal: uma compra
    /// `Pendente` sempre corresponde a um veículo `Reservado` (invariante garantida pela
    /// gravação atômica das US3.1/US3.2), então este guard só dispararia com dado corrompido
    /// em outro lugar.
    /// </summary>
    public void MarcarComoVendido()
    {
        if (Status != StatusVeiculo.Reservado)
        {
            throw new VeiculoNaoReservadoException();
        }

        Status = StatusVeiculo.Vendido;
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
