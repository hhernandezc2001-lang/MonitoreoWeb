using Microsoft.EntityFrameworkCore;
using MonitoreoWeb.Models;

namespace MonitoreoWeb.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Usuario> Usuario { get; set; }
        public DbSet<Cliente> Cliente { get; set; }
        public DbSet<Dispositivo> Dispositivo { get; set; }
        public DbSet<Reparacion> Reparacion { get; set; }
        public DbSet<HistorialAvance> HistorialAvance { get; set; }
        public DbSet<FotoAvance> FotoAvance { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.ToTable("Usuario");

                entity.HasKey(e => e.IdUsuario);
            });

            modelBuilder.Entity<Cliente>(entity =>
            {
                entity.ToTable("Cliente");

                entity.HasKey(e => e.IdCliente);
            });

            modelBuilder.Entity<Reparacion>(entity =>
            {
                entity.ToTable("Reparacion");

                entity.HasKey(e => e.IdReparacion);
            });

            modelBuilder.Entity<Dispositivo>(entity =>
            {
                entity.ToTable("Dispositivo");

                entity.HasKey(e => e.IdDispositivo);

                entity.HasOne(d => d.Cliente)
                      .WithMany()
                      .HasForeignKey(d => d.IdCliente);
            });

            modelBuilder.Entity<HistorialAvance>(entity =>
            {
                entity.ToTable("HistorialAvance");
                entity.HasKey(e => e.IdAvance);

                entity.HasOne(e => e.Reparacion)
                      .WithMany()
                      .HasForeignKey(e => e.IdReparacion);

                entity.HasOne(e => e.Usuario)
                      .WithMany()
                      .HasForeignKey(e => e.IdUsuario);
            });

            modelBuilder.Entity<FotoAvance>(entity =>
            {
                entity.ToTable("FotoAvance");
                entity.HasKey(e => e.IdFoto);

                entity.HasOne(e => e.Reparacion)
                      .WithMany()
                      .HasForeignKey(e => e.IdReparacion);

                entity.HasOne(e => e.Avance)
                      .WithMany(a => a.Fotos)
                      .HasForeignKey(e => e.IdAvance);

                entity.HasOne(e => e.Usuario)
                      .WithMany()
                      .HasForeignKey(e => e.IdUsuario);
            });
        }
    }
}