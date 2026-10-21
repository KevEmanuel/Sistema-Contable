using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaContable.App.Migrations
{
    /// <inheritdoc />
    public partial class AgregarRelacionesReglasCuentas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReglasContables_Empresas_EmpresaId",
                table: "ReglasContables");

            migrationBuilder.DropForeignKey(
                name: "FK_ReglasCuentas_CuentasContables_CuentaContableId",
                table: "ReglasCuentas");

            migrationBuilder.DropForeignKey(
                name: "FK_ReglasCuentas_ReglasContables_ReglaContableId",
                table: "ReglasCuentas");

            migrationBuilder.AlterColumn<string>(
                name: "RolEmpresa",
                table: "ReglasContables",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ReglasContables_Empresas_EmpresaId",
                table: "ReglasContables",
                column: "EmpresaId",
                principalTable: "Empresas",
                principalColumn: "EmpresaId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ReglasCuentas_CuentasContables_CuentaContableId",
                table: "ReglasCuentas",
                column: "CuentaContableId",
                principalTable: "CuentasContables",
                principalColumn: "CuentaContableId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ReglasCuentas_ReglasContables_ReglaContableId",
                table: "ReglasCuentas",
                column: "ReglaContableId",
                principalTable: "ReglasContables",
                principalColumn: "ReglaContableId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReglasContables_Empresas_EmpresaId",
                table: "ReglasContables");

            migrationBuilder.DropForeignKey(
                name: "FK_ReglasCuentas_CuentasContables_CuentaContableId",
                table: "ReglasCuentas");

            migrationBuilder.DropForeignKey(
                name: "FK_ReglasCuentas_ReglasContables_ReglaContableId",
                table: "ReglasCuentas");

            migrationBuilder.AlterColumn<int>(
                name: "RolEmpresa",
                table: "ReglasContables",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ReglasContables_Empresas_EmpresaId",
                table: "ReglasContables",
                column: "EmpresaId",
                principalTable: "Empresas",
                principalColumn: "EmpresaId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ReglasCuentas_CuentasContables_CuentaContableId",
                table: "ReglasCuentas",
                column: "CuentaContableId",
                principalTable: "CuentasContables",
                principalColumn: "CuentaContableId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ReglasCuentas_ReglasContables_ReglaContableId",
                table: "ReglasCuentas",
                column: "ReglaContableId",
                principalTable: "ReglasContables",
                principalColumn: "ReglaContableId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
