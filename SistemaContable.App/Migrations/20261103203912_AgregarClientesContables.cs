using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaContable.App.Migrations
{
    /// <inheritdoc />
    public partial class AgregarClientesContables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClienteContableId",
                table: "DocumentosDte",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ClientesContables",
                columns: table => new
                {
                    ClienteContableId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    Nit = table.Column<string>(type: "TEXT", maxLength: 17, nullable: false),
                    Nrc = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientesContables", x => x.ClienteContableId);
                    table.ForeignKey(
                        name: "FK_ClientesContables_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "EmpresaId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosDte_ClienteContableId",
                table: "DocumentosDte",
                column: "ClienteContableId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientesContables_EmpresaId_Nit",
                table: "ClientesContables",
                columns: new[] { "EmpresaId", "Nit" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentosDte_ClientesContables_ClienteContableId",
                table: "DocumentosDte",
                column: "ClienteContableId",
                principalTable: "ClientesContables",
                principalColumn: "ClienteContableId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DocumentosDte_ClientesContables_ClienteContableId",
                table: "DocumentosDte");

            migrationBuilder.DropTable(
                name: "ClientesContables");

            migrationBuilder.DropIndex(
                name: "IX_DocumentosDte_ClienteContableId",
                table: "DocumentosDte");

            migrationBuilder.DropColumn(
                name: "ClienteContableId",
                table: "DocumentosDte");
        }
    }
}
