using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;

namespace Proyecto_Leyva_Cars.Models
{
    public partial class ModeloLeyvaCar : DbContext
    {
        public ModeloLeyvaCar()
            : base("name=ModeloLeyvaCar")
        {
        }

        public virtual DbSet<Consultas> Consultas { get; set; }
        public virtual DbSet<Pedidos> Pedidos { get; set; }
        public virtual DbSet<Productos> Productos { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Consultas>()
                .HasMany(e => e.Pedidos)
                .WithOptional(e => e.Consultas)
                .HasForeignKey(e => e.ConsultaId);

            modelBuilder.Entity<Pedidos>()
                .Property(e => e.PrecioAcordado)
                .HasPrecision(10, 2);

            modelBuilder.Entity<Productos>()
                .Property(e => e.Precio)
                .HasPrecision(10, 2);

            modelBuilder.Entity<Productos>()
                .HasMany(e => e.Consultas)
                .WithOptional(e => e.Productos)
                .HasForeignKey(e => e.ProductoId);

            modelBuilder.Entity<Productos>()
                .HasMany(e => e.Pedidos)
                .WithOptional(e => e.Productos)
                .HasForeignKey(e => e.ProductoId);
        }
    }
}
