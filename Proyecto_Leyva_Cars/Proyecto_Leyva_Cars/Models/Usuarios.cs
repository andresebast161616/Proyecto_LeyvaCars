namespace Proyecto_Leyva_Cars.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class Usuarios
    {
        [Key]
        public int IdUsuario { get; set; }

        [Required]
        [StringLength(50)]
        public string NombreUsuario { get; set; }

        [Required]
        [StringLength(100)]
        public string Correo { get; set; }

        [Required]
        [StringLength(200)]
        public string Contrasena { get; set; }

        public DateTime? FechaRegistro { get; set; }

        // NUEVO: Campo para verificación
        public bool EmailVerificado { get; set; }

        // NUEVO: Relación con códigos de verificación
        public virtual ICollection<CodigoVerificacion> CodigosVerificacion { get; set; }
    }
}
