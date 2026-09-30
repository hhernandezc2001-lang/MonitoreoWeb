using Microsoft.EntityFrameworkCore;
using MonitoreoWeb.Models;

namespace MonitoreoWeb.Data
{
    /// <summary>
    /// Contexto principal de la base de datos para la aplicación MonitoreoWeb.
    /// Administra las colecciones de entidades y la configuración del modelo mediante Entity Framework Core.
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="ApplicationDbContext"/>.
        /// </summary>
        /// <param name="options">Las opciones de configuración para el contexto de base de datos (cadena de conexión, proveedor, etc.).</param>
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        #region Tablas (DbSet)

        /// <summary>Obtiene o establece la tabla de Usuarios del sistema.</summary>
        public DbSet<Usuario> Usuario { get; set; }

        /// <summary>Obtiene o establece la tabla de Clientes registrados.</summary>
        public DbSet<Cliente> Cliente { get; set; }

        /// <summary>Obtiene o establece la tabla de Dispositivos asociados a los clientes.</summary>
        public DbSet<Dispositivo> Dispositivo { get; set; }

        /// <summary>Obtiene o establece la tabla de Órdenes o registros de Reparación.</summary>
        public DbSet<Reparacion> Reparacion { get; set; }

        /// <summary>Obtiene o establece la tabla del Historial de Avances registrados en las reparaciones.</summary>
        public DbSet<HistorialAvance> HistorialAvance { get; set; }

        /// <summary>Obtiene o establece la tabla de Fotografías asociadas a los avances de reparación.</summary>
        public DbSet<FotoAvance> FotoAvance { get; set; }

        /// <summary>Obtiene o establece la tabla de Pagos realizados en las reparaciones.</summary>
        public DbSet<Pago> Pago { get; set; }

        public DbSet<ChecklistRecepcion> ChecklistRecepcion { get; set; }
        #endregion

        public DbSet<TokenConsulta> TokenConsulta { get; set; }

        /// <summary>
        /// Configura el modelo de datos, mapeo de tablas, claves primarias y relaciones entre entidades (Fluent API).
        /// </summary>
        /// <param name="modelBuilder">El constructor utilizado para mapear las entidades a la base de datos.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Mapeo de la entidad Usuario
            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.ToTable("Usuario");
                entity.HasKey(e => e.IdUsuario);
            });

            // Mapeo de la entidad Cliente
            modelBuilder.Entity<Cliente>(entity =>
            {
                entity.ToTable("Cliente");
                entity.HasKey(e => e.IdCliente);
            });

            // Mapeo de la entidad Reparacion
            modelBuilder.Entity<Reparacion>(entity =>
            {
                entity.ToTable("Reparacion");
                entity.HasKey(e => e.IdReparacion);
            });

            // Mapeo de la entidad Dispositivo y sus relaciones
            modelBuilder.Entity<Dispositivo>(entity =>
            {
                entity.ToTable("Dispositivo");
                entity.HasKey(e => e.IdDispositivo);

                // Relación: Un Dispositivo pertenece a un Cliente (1:N)
                entity.HasOne(d => d.Cliente)
                      .WithMany(c => c.Dispositivos)
                      .HasForeignKey(d => d.IdCliente);
            });

            // Mapeo de la entidad HistorialAvance y sus relaciones
            modelBuilder.Entity<HistorialAvance>(entity =>
            {
                entity.ToTable("HistorialAvance");
                entity.HasKey(e => e.IdAvance);

                // Relación: Un Avance pertenece a una Reparación (1:N)
                entity.HasOne(e => e.Reparacion)
                      .WithMany()
                      .HasForeignKey(e => e.IdReparacion);

                // Relación: Un Avance es registrado por un Usuario (1:N)
                entity.HasOne(e => e.Usuario)
                      .WithMany()
                      .HasForeignKey(e => e.IdUsuario);
            });

            // Mapeo de la entidad FotoAvance y sus relaciones
            modelBuilder.Entity<FotoAvance>(entity =>
            {
                entity.ToTable("FotoAvance");
                entity.HasKey(e => e.IdFoto);

                // Relación: Una Foto pertenece a una Reparación (1:N)
                entity.HasOne(e => e.Reparacion)
                      .WithMany()
                      .HasForeignKey(e => e.IdReparacion);

                // Relación: Una Foto pertenece a un Avance específico (1:N)
                entity.HasOne(e => e.Avance)
                      .WithMany(a => a.Fotos)
                      .HasForeignKey(e => e.IdAvance);

                // Relación: Una Foto es subida por un Usuario (1:N)
                entity.HasOne(e => e.Usuario)
                      .WithMany()
                      .HasForeignKey(e => e.IdUsuario);
            });

            // Mapeo de la entidad Pago y sus relaciones
            modelBuilder.Entity<Pago>(entity =>
            {
                entity.ToTable("Pago");
                entity.HasKey(e => e.IdPago);

                // Relación: Un Pago pertenece a una Reparación (1:N)
                entity.HasOne(e => e.Reparacion)
                      .WithMany(r => r.Pagos)
                      .HasForeignKey(e => e.IdReparacion);
            });

            modelBuilder.Entity<ChecklistRecepcion>(entity =>
            {
                entity.ToTable("ChecklistRecepcion");
                entity.HasKey(e => e.IdChecklist);

                // Configuración de la relación 1 a 1 entre Reparacion y ChecklistRecepcion
                entity.HasOne(c => c.Reparacion)
                      .WithOne(r => r.Checklist)
                      .HasForeignKey<ChecklistRecepcion>(c => c.IdReparacion)
                      .OnDelete(DeleteBehavior.Cascade);
            });

        }
    }
}