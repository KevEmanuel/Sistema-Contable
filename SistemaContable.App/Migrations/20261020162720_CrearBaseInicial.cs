using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaContable.App.Migrations
{
    /// <inheritdoc />
    public partial class CrearBaseInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Empresas",
                columns: table => new
                {
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NombreRazonSocial = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    NombreComercial = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true),
                    Nit = table.Column<string>(type: "TEXT", maxLength: 17, nullable: false),
                    NRC = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    CodigoActividad = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    DescripcionActividad = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true),
                    Departamento = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Municipio = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Direccion = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Telefono = table.Column<string>(type: "TEXT", maxLength: 25, nullable: true),
                    Correo = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true),
                    Activa = table.Column<bool>(type: "INTEGER", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empresas", x => x.EmpresaId);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    RolId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.RolId);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Correo = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    HashContrasena = table.Column<string>(type: "TEXT", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.UsuarioId);
                });

            migrationBuilder.CreateTable(
                name: "CuentasContables",
                columns: table => new
                {
                    CuentaContableId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Codigo = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    TipoCuenta = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CuentaPadreId = table.Column<int>(type: "INTEGER", nullable: true),
                    Nivel = table.Column<int>(type: "INTEGER", nullable: false),
                    PermiteMovimientos = table.Column<bool>(type: "INTEGER", nullable: false),
                    Activa = table.Column<bool>(type: "INTEGER", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CuentasContables", x => x.CuentaContableId);
                    table.ForeignKey(
                        name: "FK_CuentasContables_CuentasContables_CuentaPadreId",
                        column: x => x.CuentaPadreId,
                        principalTable: "CuentasContables",
                        principalColumn: "CuentaContableId");
                    table.ForeignKey(
                        name: "FK_CuentasContables_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "EmpresaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocumentosDte",
                columns: table => new
                {
                    DocumentoDteId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    CodigoGeneracion = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    NumeroControl = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    TipoDte = table.Column<string>(type: "TEXT", maxLength: 5, nullable: false),
                    VersionSchema = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaEmision = table.Column<DateTime>(type: "TEXT", nullable: false),
                    HoraEmision = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    RolEmpresa = table.Column<string>(type: "TEXT", nullable: false),
                    Estado = table.Column<string>(type: "TEXT", nullable: false),
                    DocumentoRelacionadoId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreadoEn = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentosDte", x => x.DocumentoDteId);
                    table.ForeignKey(
                        name: "FK_DocumentosDte_DocumentosDte_DocumentoRelacionadoId",
                        column: x => x.DocumentoRelacionadoId,
                        principalTable: "DocumentosDte",
                        principalColumn: "DocumentoDteId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentosDte_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "EmpresaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PeriodosContables",
                columns: table => new
                {
                    PeriodoContableId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EstaCerrado = table.Column<bool>(type: "INTEGER", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PeriodosContables", x => x.PeriodoContableId);
                    table.ForeignKey(
                        name: "FK_PeriodosContables_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "EmpresaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReglasContables",
                columns: table => new
                {
                    ReglaContableId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    TipoDte = table.Column<string>(type: "TEXT", maxLength: 5, nullable: true),
                    RolEmpresa = table.Column<int>(type: "INTEGER", nullable: true),
                    CondicionJson = table.Column<string>(type: "TEXT", nullable: false),
                    VigenciDesde = table.Column<DateTime>(type: "TEXT", nullable: false),
                    VigenciaHasta = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    Activa = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReglasContables", x => x.ReglaContableId);
                    table.ForeignKey(
                        name: "FK_ReglasContables_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "EmpresaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Terceros",
                columns: table => new
                {
                    TerceroId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nit = table.Column<string>(type: "TEXT", maxLength: 17, nullable: true),
                    Nrc = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    CodigoActividad = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    Correo = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true),
                    Telefono = table.Column<string>(type: "TEXT", maxLength: 25, nullable: true),
                    Direccion = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Terceros", x => x.TerceroId);
                    table.ForeignKey(
                        name: "FK_Terceros_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "EmpresaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BitacorasAuditoria",
                columns: table => new
                {
                    BitacoraAuditoriaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: true),
                    Entidad = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IdEntidad = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Accion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ValoresAnteriores = table.Column<string>(type: "TEXT", nullable: true),
                    ValoresNuevos = table.Column<string>(type: "TEXT", nullable: true),
                    IpOrigen = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    FechaHora = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BitacorasAuditoria", x => x.BitacoraAuditoriaId);
                    table.ForeignKey(
                        name: "FK_BitacorasAuditoria_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "EmpresaId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BitacorasAuditoria_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "UsuarioId");
                });

            migrationBuilder.CreateTable(
                name: "UsuarioRoles",
                columns: table => new
                {
                    UsuarioRolId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    RolId = table.Column<int>(type: "INTEGER", nullable: false),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuarioRoles", x => x.UsuarioRolId);
                    table.ForeignKey(
                        name: "FK_UsuarioRoles_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "EmpresaId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UsuarioRoles_Roles_RolId",
                        column: x => x.RolId,
                        principalTable: "Roles",
                        principalColumn: "RolId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UsuarioRoles_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "UsuarioId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ArchivosDte",
                columns: table => new
                {
                    ArchivoDteId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DocumentoDteId = table.Column<int>(type: "INTEGER", nullable: false),
                    JsonOriginal = table.Column<string>(type: "TEXT", nullable: false),
                    HashSha256 = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Fuente = table.Column<string>(type: "TEXT", nullable: false),
                    FechaCarga = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArchivosDte", x => x.ArchivoDteId);
                    table.ForeignKey(
                        name: "FK_ArchivosDte_DocumentosDte_DocumentoDteId",
                        column: x => x.DocumentoDteId,
                        principalTable: "DocumentosDte",
                        principalColumn: "DocumentoDteId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DetallesDte",
                columns: table => new
                {
                    DetalleDTeId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DocumentoDteId = table.Column<int>(type: "INTEGER", nullable: false),
                    NumeroItem = table.Column<int>(type: "INTEGER", nullable: false),
                    TipoItem = table.Column<int>(type: "INTEGER", nullable: true),
                    Cantidad = table.Column<decimal>(type: "TEXT", nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Descuento = table.Column<decimal>(type: "TEXT", nullable: false),
                    VentaGravada = table.Column<decimal>(type: "TEXT", nullable: false),
                    VentaExenta = table.Column<decimal>(type: "TEXT", nullable: false),
                    VentaNoSujeta = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetallesDte", x => x.DetalleDTeId);
                    table.ForeignKey(
                        name: "FK_DetallesDte_DocumentosDte_DocumentoDteId",
                        column: x => x.DocumentoDteId,
                        principalTable: "DocumentosDte",
                        principalColumn: "DocumentoDteId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DteReferencias",
                columns: table => new
                {
                    DteReferenciaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DocumentoDteOrigenId = table.Column<int>(type: "INTEGER", nullable: false),
                    DocumentoDteReferenciadoId = table.Column<int>(type: "INTEGER", nullable: false),
                    TipoReferencia = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DteReferencias", x => x.DteReferenciaId);
                    table.ForeignKey(
                        name: "FK_DteReferencias_DocumentosDte_DocumentoDteOrigenId",
                        column: x => x.DocumentoDteOrigenId,
                        principalTable: "DocumentosDte",
                        principalColumn: "DocumentoDteId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DteReferencias_DocumentosDte_DocumentoDteReferenciadoId",
                        column: x => x.DocumentoDteReferenciadoId,
                        principalTable: "DocumentosDte",
                        principalColumn: "DocumentoDteId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EventosDte",
                columns: table => new
                {
                    EventoDteId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DocumentoDteId = table.Column<int>(type: "INTEGER", nullable: false),
                    TipoEvento = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    EstadoEvento = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    FechaEvento = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SelloRecibido = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    DetalleJson = table.Column<string>(type: "TEXT", nullable: true),
                    CreadoEn = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventosDte", x => x.EventoDteId);
                    table.ForeignKey(
                        name: "FK_EventosDte_DocumentosDte_DocumentoDteId",
                        column: x => x.DocumentoDteId,
                        principalTable: "DocumentosDte",
                        principalColumn: "DocumentoDteId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EvidenciasFiscales",
                columns: table => new
                {
                    EvidenciaFiscalId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DocumentoDteId = table.Column<int>(type: "INTEGER", nullable: false),
                    Fuente = table.Column<string>(type: "TEXT", nullable: false),
                    SelloRecibido = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    EstadoHacienda = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    FechaProcesamiento = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CodigoMensaje = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    DescripcionMensaje = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Observaciones = table.Column<string>(type: "TEXT", nullable: true),
                    RespuestaJson = table.Column<string>(type: "TEXT", nullable: true),
                    CreadoEn = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvidenciasFiscales", x => x.EvidenciaFiscalId);
                    table.ForeignKey(
                        name: "FK_EvidenciasFiscales_DocumentosDte_DocumentoDteId",
                        column: x => x.DocumentoDteId,
                        principalTable: "DocumentosDte",
                        principalColumn: "DocumentoDteId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExportacionesFiscales",
                columns: table => new
                {
                    ExportacionFiscalId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    PeriodoContableId = table.Column<int>(type: "INTEGER", nullable: false),
                    TipoAnexo = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    VersionFormato = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    NombreArchivo = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    Contenido = table.Column<string>(type: "TEXT", nullable: false),
                    HashSha256 = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    GeneradoPorId = table.Column<int>(type: "INTEGER", nullable: false),
                    GeneradoEn = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExportacionesFiscales", x => x.ExportacionFiscalId);
                    table.ForeignKey(
                        name: "FK_ExportacionesFiscales_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "EmpresaId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExportacionesFiscales_PeriodosContables_PeriodoContableId",
                        column: x => x.PeriodoContableId,
                        principalTable: "PeriodosContables",
                        principalColumn: "PeriodoContableId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExportacionesFiscales_Usuarios_GeneradoPorId",
                        column: x => x.GeneradoPorId,
                        principalTable: "Usuarios",
                        principalColumn: "UsuarioId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RegistrosLibroIva",
                columns: table => new
                {
                    RegistroLibroIvaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DocumentoDteId = table.Column<int>(type: "INTEGER", nullable: false),
                    PeriodoContableId = table.Column<int>(type: "INTEGER", nullable: false),
                    TipoLibro = table.Column<string>(type: "TEXT", nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "TEXT", nullable: false),
                    BaseGravada = table.Column<decimal>(type: "TEXT", nullable: false),
                    MontoExento = table.Column<decimal>(type: "TEXT", nullable: false),
                    MontoNoSujeto = table.Column<decimal>(type: "TEXT", nullable: false),
                    CreditoFiscal = table.Column<decimal>(type: "TEXT", nullable: false),
                    DebitoFiscal = table.Column<decimal>(type: "TEXT", nullable: false),
                    TotalOperacion = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Estado = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosLibroIva", x => x.RegistroLibroIvaId);
                    table.ForeignKey(
                        name: "FK_RegistrosLibroIva_DocumentosDte_DocumentoDteId",
                        column: x => x.DocumentoDteId,
                        principalTable: "DocumentosDte",
                        principalColumn: "DocumentoDteId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RegistrosLibroIva_PeriodosContables_PeriodoContableId",
                        column: x => x.PeriodoContableId,
                        principalTable: "PeriodosContables",
                        principalColumn: "PeriodoContableId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PartidasContables",
                columns: table => new
                {
                    PartidaContableId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    PeriodoContableId = table.Column<int>(type: "INTEGER", nullable: false),
                    DocumentoDteId = table.Column<int>(type: "INTEGER", nullable: true),
                    ReglaContableId = table.Column<int>(type: "INTEGER", nullable: true),
                    NumeroPartida = table.Column<int>(type: "INTEGER", nullable: false),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Glosa = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    TipoOrigen = table.Column<string>(type: "TEXT", nullable: false),
                    Estado = table.Column<string>(type: "TEXT", nullable: false),
                    PartidaReversaId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreadoPorId = table.Column<int>(type: "INTEGER", nullable: false),
                    AprobadoPorId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreadoEn = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartidasContables", x => x.PartidaContableId);
                    table.ForeignKey(
                        name: "FK_PartidasContables_DocumentosDte_DocumentoDteId",
                        column: x => x.DocumentoDteId,
                        principalTable: "DocumentosDte",
                        principalColumn: "DocumentoDteId");
                    table.ForeignKey(
                        name: "FK_PartidasContables_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "EmpresaId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PartidasContables_PartidasContables_PartidaReversaId",
                        column: x => x.PartidaReversaId,
                        principalTable: "PartidasContables",
                        principalColumn: "PartidaContableId");
                    table.ForeignKey(
                        name: "FK_PartidasContables_PeriodosContables_PeriodoContableId",
                        column: x => x.PeriodoContableId,
                        principalTable: "PeriodosContables",
                        principalColumn: "PeriodoContableId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PartidasContables_ReglasContables_ReglaContableId",
                        column: x => x.ReglaContableId,
                        principalTable: "ReglasContables",
                        principalColumn: "ReglaContableId");
                    table.ForeignKey(
                        name: "FK_PartidasContables_Usuarios_AprobadoPorId",
                        column: x => x.AprobadoPorId,
                        principalTable: "Usuarios",
                        principalColumn: "UsuarioId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PartidasContables_Usuarios_CreadoPorId",
                        column: x => x.CreadoPorId,
                        principalTable: "Usuarios",
                        principalColumn: "UsuarioId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReglasCuentas",
                columns: table => new
                {
                    ReglaCuentaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ReglaContableId = table.Column<int>(type: "INTEGER", nullable: false),
                    CuentaContableId = table.Column<int>(type: "INTEGER", nullable: false),
                    Lado = table.Column<string>(type: "TEXT", nullable: false),
                    Formula = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Orden = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReglasCuentas", x => x.ReglaCuentaId);
                    table.ForeignKey(
                        name: "FK_ReglasCuentas_CuentasContables_CuentaContableId",
                        column: x => x.CuentaContableId,
                        principalTable: "CuentasContables",
                        principalColumn: "CuentaContableId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReglasCuentas_ReglasContables_ReglaContableId",
                        column: x => x.ReglaContableId,
                        principalTable: "ReglasContables",
                        principalColumn: "ReglaContableId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DteParticipantes",
                columns: table => new
                {
                    DteParticipanteId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DocumentoDteId = table.Column<int>(type: "INTEGER", nullable: false),
                    TerceroId = table.Column<int>(type: "INTEGER", nullable: true),
                    Rol = table.Column<string>(type: "TEXT", nullable: false),
                    NitSnapshot = table.Column<string>(type: "TEXT", maxLength: 17, nullable: true),
                    NrcSnapshot = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    NombreSnapshot = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DteParticipantes", x => x.DteParticipanteId);
                    table.ForeignKey(
                        name: "FK_DteParticipantes_DocumentosDte_DocumentoDteId",
                        column: x => x.DocumentoDteId,
                        principalTable: "DocumentosDte",
                        principalColumn: "DocumentoDteId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DteParticipantes_Terceros_TerceroId",
                        column: x => x.TerceroId,
                        principalTable: "Terceros",
                        principalColumn: "TerceroId");
                });

            migrationBuilder.CreateTable(
                name: "ExportacionesFiscalesDetalle",
                columns: table => new
                {
                    ExportacionFiscalDetalleId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ExportacionFiscalId = table.Column<int>(type: "INTEGER", nullable: false),
                    RegistroLibroIvaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Linea = table.Column<int>(type: "INTEGER", nullable: false),
                    ContenidoLinea = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExportacionesFiscalesDetalle", x => x.ExportacionFiscalDetalleId);
                    table.ForeignKey(
                        name: "FK_ExportacionesFiscalesDetalle_ExportacionesFiscales_ExportacionFiscalId",
                        column: x => x.ExportacionFiscalId,
                        principalTable: "ExportacionesFiscales",
                        principalColumn: "ExportacionFiscalId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExportacionesFiscalesDetalle_RegistrosLibroIva_RegistroLibroIvaId",
                        column: x => x.RegistroLibroIvaId,
                        principalTable: "RegistrosLibroIva",
                        principalColumn: "RegistroLibroIvaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DetallesPartida",
                columns: table => new
                {
                    DetallePartidaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PartidaContableId = table.Column<int>(type: "INTEGER", nullable: false),
                    CuentaContableId = table.Column<int>(type: "INTEGER", nullable: false),
                    Concepto = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Debe = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Haber = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetallesPartida", x => x.DetallePartidaId);
                    table.ForeignKey(
                        name: "FK_DetallesPartida_CuentasContables_CuentaContableId",
                        column: x => x.CuentaContableId,
                        principalTable: "CuentasContables",
                        principalColumn: "CuentaContableId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DetallesPartida_PartidasContables_PartidaContableId",
                        column: x => x.PartidaContableId,
                        principalTable: "PartidasContables",
                        principalColumn: "PartidaContableId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArchivosDte_DocumentoDteId",
                table: "ArchivosDte",
                column: "DocumentoDteId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BitacorasAuditoria_EmpresaId",
                table: "BitacorasAuditoria",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_BitacorasAuditoria_UsuarioId",
                table: "BitacorasAuditoria",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_CuentasContables_CuentaPadreId",
                table: "CuentasContables",
                column: "CuentaPadreId");

            migrationBuilder.CreateIndex(
                name: "IX_CuentasContables_EmpresaId_Codigo",
                table: "CuentasContables",
                columns: new[] { "EmpresaId", "Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DetallesDte_DocumentoDteId_NumeroItem",
                table: "DetallesDte",
                columns: new[] { "DocumentoDteId", "NumeroItem" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DetallesPartida_CuentaContableId",
                table: "DetallesPartida",
                column: "CuentaContableId");

            migrationBuilder.CreateIndex(
                name: "IX_DetallesPartida_PartidaContableId",
                table: "DetallesPartida",
                column: "PartidaContableId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosDte_DocumentoRelacionadoId",
                table: "DocumentosDte",
                column: "DocumentoRelacionadoId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosDte_EmpresaId_CodigoGeneracion",
                table: "DocumentosDte",
                columns: new[] { "EmpresaId", "CodigoGeneracion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DteParticipantes_DocumentoDteId",
                table: "DteParticipantes",
                column: "DocumentoDteId");

            migrationBuilder.CreateIndex(
                name: "IX_DteParticipantes_TerceroId",
                table: "DteParticipantes",
                column: "TerceroId");

            migrationBuilder.CreateIndex(
                name: "IX_DteReferencias_DocumentoDteOrigenId",
                table: "DteReferencias",
                column: "DocumentoDteOrigenId");

            migrationBuilder.CreateIndex(
                name: "IX_DteReferencias_DocumentoDteReferenciadoId",
                table: "DteReferencias",
                column: "DocumentoDteReferenciadoId");

            migrationBuilder.CreateIndex(
                name: "IX_Empresas_Nit",
                table: "Empresas",
                column: "Nit",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventosDte_DocumentoDteId",
                table: "EventosDte",
                column: "DocumentoDteId");

            migrationBuilder.CreateIndex(
                name: "IX_EvidenciasFiscales_DocumentoDteId",
                table: "EvidenciasFiscales",
                column: "DocumentoDteId");

            migrationBuilder.CreateIndex(
                name: "IX_ExportacionesFiscales_EmpresaId",
                table: "ExportacionesFiscales",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_ExportacionesFiscales_GeneradoPorId",
                table: "ExportacionesFiscales",
                column: "GeneradoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_ExportacionesFiscales_PeriodoContableId",
                table: "ExportacionesFiscales",
                column: "PeriodoContableId");

            migrationBuilder.CreateIndex(
                name: "IX_ExportacionesFiscalesDetalle_ExportacionFiscalId",
                table: "ExportacionesFiscalesDetalle",
                column: "ExportacionFiscalId");

            migrationBuilder.CreateIndex(
                name: "IX_ExportacionesFiscalesDetalle_RegistroLibroIvaId",
                table: "ExportacionesFiscalesDetalle",
                column: "RegistroLibroIvaId");

            migrationBuilder.CreateIndex(
                name: "IX_PartidasContables_AprobadoPorId",
                table: "PartidasContables",
                column: "AprobadoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_PartidasContables_CreadoPorId",
                table: "PartidasContables",
                column: "CreadoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_PartidasContables_DocumentoDteId",
                table: "PartidasContables",
                column: "DocumentoDteId");

            migrationBuilder.CreateIndex(
                name: "IX_PartidasContables_EmpresaId_PeriodoContableId_NumeroPartida",
                table: "PartidasContables",
                columns: new[] { "EmpresaId", "PeriodoContableId", "NumeroPartida" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PartidasContables_PartidaReversaId",
                table: "PartidasContables",
                column: "PartidaReversaId");

            migrationBuilder.CreateIndex(
                name: "IX_PartidasContables_PeriodoContableId",
                table: "PartidasContables",
                column: "PeriodoContableId");

            migrationBuilder.CreateIndex(
                name: "IX_PartidasContables_ReglaContableId",
                table: "PartidasContables",
                column: "ReglaContableId");

            migrationBuilder.CreateIndex(
                name: "IX_PeriodosContables_EmpresaId",
                table: "PeriodosContables",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosLibroIva_DocumentoDteId",
                table: "RegistrosLibroIva",
                column: "DocumentoDteId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosLibroIva_PeriodoContableId",
                table: "RegistrosLibroIva",
                column: "PeriodoContableId");

            migrationBuilder.CreateIndex(
                name: "IX_ReglasContables_EmpresaId",
                table: "ReglasContables",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_ReglasCuentas_CuentaContableId",
                table: "ReglasCuentas",
                column: "CuentaContableId");

            migrationBuilder.CreateIndex(
                name: "IX_ReglasCuentas_ReglaContableId",
                table: "ReglasCuentas",
                column: "ReglaContableId");

            migrationBuilder.CreateIndex(
                name: "IX_Terceros_EmpresaId",
                table: "Terceros",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioRoles_EmpresaId",
                table: "UsuarioRoles",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioRoles_RolId",
                table: "UsuarioRoles",
                column: "RolId");

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioRoles_UsuarioId",
                table: "UsuarioRoles",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Correo",
                table: "Usuarios",
                column: "Correo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArchivosDte");

            migrationBuilder.DropTable(
                name: "BitacorasAuditoria");

            migrationBuilder.DropTable(
                name: "DetallesDte");

            migrationBuilder.DropTable(
                name: "DetallesPartida");

            migrationBuilder.DropTable(
                name: "DteParticipantes");

            migrationBuilder.DropTable(
                name: "DteReferencias");

            migrationBuilder.DropTable(
                name: "EventosDte");

            migrationBuilder.DropTable(
                name: "EvidenciasFiscales");

            migrationBuilder.DropTable(
                name: "ExportacionesFiscalesDetalle");

            migrationBuilder.DropTable(
                name: "ReglasCuentas");

            migrationBuilder.DropTable(
                name: "UsuarioRoles");

            migrationBuilder.DropTable(
                name: "PartidasContables");

            migrationBuilder.DropTable(
                name: "Terceros");

            migrationBuilder.DropTable(
                name: "ExportacionesFiscales");

            migrationBuilder.DropTable(
                name: "RegistrosLibroIva");

            migrationBuilder.DropTable(
                name: "CuentasContables");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "ReglasContables");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "DocumentosDte");

            migrationBuilder.DropTable(
                name: "PeriodosContables");

            migrationBuilder.DropTable(
                name: "Empresas");
        }
    }
}
