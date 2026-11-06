using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaContable.App.Migrations
{
    /// <inheritdoc />
    public partial class ConfiguracionPorTipoDte : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ConfiguracionesContablesAutomaticas_EmpresaId",
                table: "ConfiguracionesContablesAutomaticas");

            migrationBuilder.AddColumn<string>(
                name: "Operacion",
                table: "ConfiguracionesContablesAutomaticas",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TipoDte",
                table: "ConfiguracionesContablesAutomaticas",
                type: "TEXT",
                maxLength: 5,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                """
                UPDATE ConfiguracionesContablesAutomaticas
                SET TipoDte = '01', Operacion = 'PorRolEmpresa';

                INSERT INTO ConfiguracionesContablesAutomaticas
                    (EmpresaId, CuentaClientesId, CuentaVentasId,
                     CuentaIvaDebitoId, CuentaProveedoresId, CuentaComprasId,
                     CuentaIvaAcreditableId, TipoDte, Operacion)
                SELECT EmpresaId, CuentaClientesId, CuentaVentasId,
                       CuentaIvaDebitoId, CuentaProveedoresId, CuentaComprasId,
                       CuentaIvaAcreditableId, '03', 'PorRolEmpresa'
                FROM ConfiguracionesContablesAutomaticas
                WHERE TipoDte = '01';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracionesContablesAutomaticas_EmpresaId_TipoDte",
                table: "ConfiguracionesContablesAutomaticas",
                columns: new[] { "EmpresaId", "TipoDte" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM ConfiguracionesContablesAutomaticas
                WHERE ConfiguracionContableAutomaticaId NOT IN (
                    SELECT MIN(ConfiguracionContableAutomaticaId)
                    FROM ConfiguracionesContablesAutomaticas
                    GROUP BY EmpresaId);
                """);

            migrationBuilder.DropIndex(
                name: "IX_ConfiguracionesContablesAutomaticas_EmpresaId_TipoDte",
                table: "ConfiguracionesContablesAutomaticas");

            migrationBuilder.DropColumn(
                name: "Operacion",
                table: "ConfiguracionesContablesAutomaticas");

            migrationBuilder.DropColumn(
                name: "TipoDte",
                table: "ConfiguracionesContablesAutomaticas");

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracionesContablesAutomaticas_EmpresaId",
                table: "ConfiguracionesContablesAutomaticas",
                column: "EmpresaId",
                unique: true);
        }
    }
}
