namespace Proyecto_Leyva_Cars.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("CodigoVerificacion")]
    public partial class CodigoVerificacion
    {
        [Key]
        public int IdCodigo { get; set; }

        public int IdUsuario { get; set; }

        [Required]
        [StringLength(6)]
        public string Codigo { get; set; }

        public DateTime FechaCreacion { get; set; }

        public DateTime FechaExpiracion { get; set; }

        public bool Verificado { get; set; }
    }
}
