using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VendasApi.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Sem DDL de fato: `xmin` já é uma coluna de sistema em toda tabela do Postgres — o
    /// gerador de migração propôs `ADD COLUMN xmin`, mas isso falharia em tempo de execução
    /// (Postgres rejeita coluna de usuário com nome de coluna de sistema, ex.: "column name
    /// xmin conflicts with a system column name"). Esta migração existe só para o EF Core
    /// atualizar o próprio snapshot do modelo (`VendasDbContextModelSnapshot`), refletindo que
    /// `Veiculo` agora mapeia `xmin` como token de concorrência (ver `VendasDbContext`) — sem
    /// isso, toda migração seguinte tentaria adicionar a mesma coluna de novo.
    /// </summary>
    public partial class AddVeiculoConcurrencyToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
