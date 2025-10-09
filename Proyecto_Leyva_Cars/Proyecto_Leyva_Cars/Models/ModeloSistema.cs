using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;

namespace Proyecto_Leyva_Cars.Models
{
    public partial class ModeloSistema : DbContext
    {
        public ModeloSistema()
            : base("name=ModeloSistema")
        {
        }

        public virtual DbSet<Usuarios> Usuarios { get; set; }

        public virtual DbSet<CodigoVerificacion> CodigosVerificacion { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
        }
    }

}
