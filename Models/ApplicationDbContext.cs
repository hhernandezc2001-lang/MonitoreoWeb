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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.ToTable("Usuario");

                entity.HasKey(e => e.IdUsuario);

                entity.Property(e => e.IdUsuario)
                    .HasColumnName("IdUsuario");

                entity.Property(e => e.Nombre)
                    .HasColumnName("Nombre");

                entity.Property(e => e.Email)
                    .HasColumnName("Email");

                entity.Property(e => e.PasswordHash)
                    .HasColumnName("PasswordHash");

                entity.Property(e => e.Rol)
                    .HasColumnName("Rol");

                entity.Property(e => e.Activo)
                    .HasColumnName("Activo");

                entity.Property(e => e.FechaCreacion)
                    .HasColumnName("FechaCreacion");
            });

            modelBuilder.Entity<Cliente>(entity =>
            {
                entity.ToTable("Cliente");

                entity.HasKey(e => e.IdCliente);
            });
        }
    }
}