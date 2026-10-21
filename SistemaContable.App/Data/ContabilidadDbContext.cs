using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using SistemaContableDTE.Models;

namespace SistemaContableDTE.Data;

public class ContabilidadDbContext : DbContext
{
    public DbSet<Empresa> Empresas { get; set; } = null!;
    public DbSet<Usuario> Usuarios { get; set; } = null!;
    public DbSet<Rol> Roles { get; set; } = null!;
    public DbSet<UsuarioRol> UsuarioRoles { get; set; } = null!;
    //correcion de error en minucula
    //public DbSet<PeriodoContable> PeriodosContables { get; set; } = null!;
    //public DbSet<PeriodoContable> PeriodosContables { get; set; }
    public DbSet<PeriodoContable> PeriodosContables { get; set; } = null!;
    public DbSet<CuentaContable> CuentasContables { get; set; } = null!;

    public DbSet<Tercero> Terceros { get; set; } = null!;
    public DbSet<DocumentoDte> DocumentosDte { get; set; } = null!;
    public DbSet<DteParticipante> DteParticipantes { get; set; } = null!;
    public DbSet<ArchivoDte> ArchivosDte { get; set; } = null!;
    public DbSet<EvidenciaFiscal> EvidenciasFiscales { get; set; } = null!;
    public DbSet<DetalleDTe> DetallesDte { get; set; } = null!;
    public DbSet<EventoDte> EventosDte { get; set; } = null!;
    public DbSet<DteReferencia> DteReferencias { get; set; } = null!;

    public DbSet<ReglaContable> ReglasContables { get; set; } = null!;
    public DbSet<ReglaCuenta> ReglasCuentas { get; set; } = null!;
    public DbSet<PartidaContable> PartidasContables { get; set; } = null!;
    public DbSet<DetallePartida> DetallesPartida { get; set; } = null!;

    public DbSet<RegistroLibroIva> RegistrosLibroIva { get; set; } = null!;
    public DbSet<ExportacionFiscal> ExportacionesFiscales { get; set; } = null!;
    public DbSet<ExportacionFiscalDetalle> ExportacionesFiscalesDetalle { get; set; } = null!;
    public DbSet<BitacoraAuditoria> BitacorasAuditoria { get; set; } = null!;

    public ContabilidadDbContext()
    {
    }

    public ContabilidadDbContext(
        DbContextOptions<ContabilidadDbContext> options)
        : base(options)
    {
    }

    protected override void OnConfiguring(
        DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured)
            return;

        string carpetaBase = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            "SistemaContableDTE");

        Directory.CreateDirectory(carpetaBase);

        string rutaBaseDatos = Path.Combine(
            carpetaBase,
            "contabilidad.db");

        optionsBuilder.UseSqlite($"Data Source={rutaBaseDatos}");
    }
    //CLASE PARA MIGRACIONES DEL LA BD
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Guardar los enum como texto en SQLite.
        modelBuilder.Entity<DocumentoDte>()
            .Property(x => x.RolEmpresa)
            .HasConversion<string>();

        modelBuilder.Entity<DocumentoDte>()
            .Property(x => x.Estado)
            .HasConversion<string>();

        modelBuilder.Entity<DteParticipante>()
            .Property(x => x.Rol)
            .HasConversion<string>();

        modelBuilder.Entity<ArchivoDte>()
            .Property(x => x.Fuente)
            .HasConversion<string>();

        modelBuilder.Entity<EvidenciaFiscal>()
            .Property(x => x.Fuente)
            .HasConversion<string>();

        //cambios recientes
        modelBuilder.Entity<ReglaContable>()
    .Property(x => x.RolEmpresa)
    .HasConversion<string>();


        modelBuilder.Entity<ReglaCuenta>()
            .Property(x => x.Lado)
            .HasConversion<string>();

        modelBuilder.Entity<PartidaContable>()
            .Property(x => x.TipoOrigen)
            .HasConversion<string>();

        modelBuilder.Entity<PartidaContable>()
            .Property(x => x.Estado)
            .HasConversion<string>();

        modelBuilder.Entity<RegistroLibroIva>()
            .Property(x => x.TipoLibro)
            .HasConversion<string>();

        modelBuilder.Entity<RegistroLibroIva>()
            .Property(x => x.Estado)
            .HasConversion<string>();

        // Importes monetarios.
        modelBuilder.Entity<DetalleDTe>()
            .Property(x => x.PrecioUnitario)
            .HasPrecision(18, 2);

        modelBuilder.Entity<DetallePartida>()
            .Property(x => x.Debe)
            .HasPrecision(18, 2);

        modelBuilder.Entity<DetallePartida>()
            .Property(x => x.Haber)
            .HasPrecision(18, 2);

        modelBuilder.Entity<RegistroLibroIva>()
            .Property(x => x.TotalOperacion)
            .HasPrecision(18, 2);

        // Un DTE solo conserva un archivo JSON original.
        modelBuilder.Entity<ArchivoDte>()
            .HasOne(x => x.DocumentoDte)
            .WithOne(x => x.Archivo)
            .HasForeignKey<ArchivoDte>(x => x.DocumentoDteId);

        // Una regla pertenece a una empresa.
        modelBuilder.Entity<ReglaContable>()
            .HasOne(x => x.Empresa)
            .WithMany()
            .HasForeignKey(x => x.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);

        // Una regla posee varias cuentas configuradas.
        modelBuilder.Entity<ReglaCuenta>()
            .HasOne(x => x.ReglaContable)
            .WithMany(x => x.Cuentas)
            .HasForeignKey(x => x.ReglaContableId)
            .OnDelete(DeleteBehavior.Restrict);

        // Cada línea de regla usa una cuenta del catálogo.
        modelBuilder.Entity<ReglaCuenta>()
            .HasOne(x => x.CuentaContable)
            .WithMany()
            .HasForeignKey(x => x.CuentaContableId)
            .OnDelete(DeleteBehavior.Restrict);


        // Documento relacionado: nota de crédito, invalidación u otra referencia.
        modelBuilder.Entity<DocumentoDte>()
            .HasOne(x => x.DocumentoRelacionado)
            .WithMany()
            .HasForeignKey(x => x.DocumentoRelacionadoId)
            .OnDelete(DeleteBehavior.Restrict);

        // Un DTE puede tener emisor y receptor.
        modelBuilder.Entity<DteParticipante>()
            .HasOne(x => x.DocumentoDte)
            .WithMany(x => x.Participantes)
            .HasForeignKey(x => x.DocumentoDteId);

        // Dos relaciones distintas entre referencias y DTE.
        modelBuilder.Entity<DteReferencia>()
            .HasOne(x => x.DocumentoDteOrigen)
            .WithMany()
            .HasForeignKey(x => x.DocumentoDteOrigenId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DteReferencia>()
            .HasOne(x => x.DocumentoDteReferenciado)
            .WithMany()
            .HasForeignKey(x => x.DocumentoDteReferenciadoId)
            .OnDelete(DeleteBehavior.Restrict);

        // Usuario que crea y usuario que aprueba la partida.
        modelBuilder.Entity<PartidaContable>()
            .HasOne(x => x.CreadoPor)
            .WithMany()
            .HasForeignKey(x => x.CreadoPorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PartidaContable>()
            .HasOne(x => x.AprobadoPor)
            .WithMany()
            .HasForeignKey(x => x.AprobadoPorId)
            .OnDelete(DeleteBehavior.SetNull);

        // Índices únicos principales.
        modelBuilder.Entity<Empresa>()
            .HasIndex(x => x.Nit)
            .IsUnique();

        modelBuilder.Entity<Usuario>()
            .HasIndex(x => x.Correo)
            .IsUnique();

        modelBuilder.Entity<CuentaContable>()
            .HasIndex(x => new { x.EmpresaId, x.Codigo })
            .IsUnique();

        modelBuilder.Entity<DocumentoDte>()
            .HasIndex(x => new { x.EmpresaId, x.CodigoGeneracion })
            .IsUnique();

        modelBuilder.Entity<DetalleDTe>()
            .HasIndex(x => new { x.DocumentoDteId, x.NumeroItem })
            .IsUnique();

        modelBuilder.Entity<PartidaContable>()
            .HasIndex(x => new { x.EmpresaId, x.PeriodoContableId, x.NumeroPartida })
            .IsUnique();
    }
}