using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaContable.App.Migrations
{
    /// <inheritdoc />
    public partial class AgregarResumenYValidacionesDte : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Codigo",
                table: "DetallesDte",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ResultadosValidacionDte",
                columns: table => new
                {
                    ResultadoValidacionDteId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DocumentoDteId = table.Column<int>(type: "INTEGER", nullable: false),
                    TipoValidacion = table.Column<string>(type: "TEXT", nullable: false),
                    VersionSchema = table.Column<int>(type: "INTEGER", nullable: false),
                    VersionValidador = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Resultado = table.Column<string>(type: "TEXT", nullable: false),
                    ErroresJson = table.Column<string>(type: "TEXT", nullable: false),
                    ValidadoEn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResultadosValidacionDte", x => x.ResultadoValidacionDteId);
                    table.ForeignKey(
                        name: "FK_ResultadosValidacionDte_DocumentosDte_DocumentoDteId",
                        column: x => x.DocumentoDteId,
                        principalTable: "DocumentosDte",
                        principalColumn: "DocumentoDteId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResultadosValidacionDte_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "UsuarioId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ResumenesTributarios",
                columns: table => new
                {
                    ResumenTributarioId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DocumentoDteId = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalGravada = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    TotalExenta = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    TotalNoSujeta = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    TotalIva = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    MontoTotalOperacion = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    TotalPagar = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    JsonOriginal = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResumenesTributarios", x => x.ResumenTributarioId);
                    table.ForeignKey(
                        name: "FK_ResumenesTributarios_DocumentosDte_DocumentoDteId",
                        column: x => x.DocumentoDteId,
                        principalTable: "DocumentosDte",
                        principalColumn: "DocumentoDteId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ResultadosValidacionDte_DocumentoDteId",
                table: "ResultadosValidacionDte",
                column: "DocumentoDteId");

            migrationBuilder.CreateIndex(
                name: "IX_ResultadosValidacionDte_UsuarioId",
                table: "ResultadosValidacionDte",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_ResumenesTributarios_DocumentoDteId",
                table: "ResumenesTributarios",
                column: "DocumentoDteId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ResultadosValidacionDte");

            migrationBuilder.DropTable(
                name: "ResumenesTributarios");

            migrationBuilder.DropColumn(
                name: "Codigo",
                table: "DetallesDte");
        }
    }
}
