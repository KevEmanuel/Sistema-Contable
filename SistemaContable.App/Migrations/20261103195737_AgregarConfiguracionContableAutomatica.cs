using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaContable.App.Migrations
{
    /// <inheritdoc />
    public partial class AgregarConfiguracionContableAutomatica : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConfiguracionesContablesAutomaticas",
                columns: table => new
                {
                    ConfiguracionContableAutomaticaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    CuentaClientesId = table.Column<int>(type: "INTEGER", nullable: true),
                    CuentaVentasId = table.Column<int>(type: "INTEGER", nullable: true),
                    CuentaIvaDebitoId = table.Column<int>(type: "INTEGER", nullable: true),
                    CuentaProveedoresId = table.Column<int>(type: "INTEGER", nullable: true),
                    CuentaComprasId = table.Column<int>(type: "INTEGER", nullable: true),
                    CuentaIvaAcreditableId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionesContablesAutomaticas", x => x.ConfiguracionContableAutomaticaId);
                    table.ForeignKey(
                        name: "FK_ConfiguracionesContablesAutomaticas_CuentasContables_CuentaClientesId",
                        column: x => x.CuentaClientesId,
                        principalTable: "CuentasContables",
                        principalColumn: "CuentaContableId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConfiguracionesContablesAutomaticas_CuentasContables_CuentaComprasId",
                        column: x => x.CuentaComprasId,
                        principalTable: "CuentasContables",
                        principalColumn: "CuentaContableId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConfiguracionesContablesAutomaticas_CuentasContables_CuentaIvaAcreditableId",
                        column: x => x.CuentaIvaAcreditableId,
                        principalTable: "CuentasContables",
                        principalColumn: "CuentaContableId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConfiguracionesContablesAutomaticas_CuentasContables_CuentaIvaDebitoId",
                        column: x => x.CuentaIvaDebitoId,
                        principalTable: "CuentasContables",
                        principalColumn: "CuentaContableId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConfiguracionesContablesAutomaticas_CuentasContables_CuentaProveedoresId",
                        column: x => x.CuentaProveedoresId,
                        principalTable: "CuentasContables",
                        principalColumn: "CuentaContableId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConfiguracionesContablesAutomaticas_CuentasContables_CuentaVentasId",
                        column: x => x.CuentaVentasId,
                        principalTable: "CuentasContables",
                        principalColumn: "CuentaContableId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConfiguracionesContablesAutomaticas_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "EmpresaId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracionesContablesAutomaticas_CuentaClientesId",
                table: "ConfiguracionesContablesAutomaticas",
                column: "CuentaClientesId");

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracionesContablesAutomaticas_CuentaComprasId",
                table: "ConfiguracionesContablesAutomaticas",
                column: "CuentaComprasId");

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracionesContablesAutomaticas_CuentaIvaAcreditableId",
                table: "ConfiguracionesContablesAutomaticas",
                column: "CuentaIvaAcreditableId");

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracionesContablesAutomaticas_CuentaIvaDebitoId",
                table: "ConfiguracionesContablesAutomaticas",
                column: "CuentaIvaDebitoId");

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracionesContablesAutomaticas_CuentaProveedoresId",
                table: "ConfiguracionesContablesAutomaticas",
                column: "CuentaProveedoresId");

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracionesContablesAutomaticas_CuentaVentasId",
                table: "ConfiguracionesContablesAutomaticas",
                column: "CuentaVentasId");

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracionesContablesAutomaticas_EmpresaId",
                table: "ConfiguracionesContablesAutomaticas",
                column: "EmpresaId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfiguracionesContablesAutomaticas");
        }
    }
}
