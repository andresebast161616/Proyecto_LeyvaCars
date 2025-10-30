using Proyecto_Leyva_Cars.Models;
using System;
using System.Linq;
using System.Web.Mvc;

namespace Proyecto_Leyva_Cars.Controllers
{
    public class UsuariosController : Controller
    {
        // CAMBIO: Usar el contexto generado desde el .edmx
        private LeyvaCarEntities db = new LeyvaCarEntities();

        // GET: Usuarios
        public ActionResult Index()
        {
            var usuarios = db.Usuarios.ToList();
            return View(usuarios);
        }

        // GET: Usuarios/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Usuarios/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Usuarios usuario)
        {
            if (ModelState.IsValid)
            {
                // CAMBIO: Usar Email en lugar de Correo (según tu modelo de BD)
                if (db.Usuarios.Any(u => u.Email == usuario.Email))
                {
                    ModelState.AddModelError("Email", "Este correo ya está registrado");
                    return View(usuario);
                }

                // Crear usuario (sin verificar aún)
                usuario.FechaRegistro = DateTime.Now;
                usuario.Activo = false; // CAMBIO: Usar Activo en lugar de EmailVerificado
                db.Usuarios.Add(usuario);
                db.SaveChanges();

                // Generar código de verificación
                string codigo = GenerarCodigoAleatorio();
                var codigoVerificacion = new CodigoVerificacion
                {
                    Id_Usuario = usuario.Id_Usuario, // CAMBIO: Usar Id_Usuario
                    Codigo = codigo,
                    TipoVerificacion = "registro", // CAMBIO: Agregar tipo
                    FechaCreacion = DateTime.Now,
                    FechaExpiracion = DateTime.Now.AddMinutes(15),
                    Usado = false // CAMBIO: Usar Usado en lugar de Verificado
                };
                db.CodigoVerificacion.Add(codigoVerificacion);
                db.SaveChanges();

                // Guardar datos en TempData para la vista de verificación
                TempData["IdUsuario"] = usuario.Id_Usuario;
                TempData["Correo"] = usuario.Email;
                TempData["Codigo"] = codigo;

                return RedirectToAction("VerificarEmail");
            }
            return View(usuario);
        }

        // GET: Usuarios/VerificarEmail
        public ActionResult VerificarEmail()
        {
            if (TempData["IdUsuario"] == null)
            {
                return RedirectToAction("Create");
            }

            string correo = TempData["Correo"].ToString();

            ViewBag.Correo = correo;
            ViewBag.Codigo = TempData["Codigo"];
            ViewBag.NombreUsuario = correo.Split('@')[0];
            TempData.Keep("IdUsuario");

            return View();
        }

        // POST: Usuarios/VerificarEmail
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult VerificarEmail(string codigo)
        {
            int idUsuario = (int)TempData["IdUsuario"];

            // CAMBIO: Usar nombres de campos correctos
            var codigoVerificacion = db.CodigoVerificacion
                .Where(c => c.Id_Usuario == idUsuario && c.Codigo == codigo && c.Usado == false)
                .OrderByDescending(c => c.FechaCreacion)
                .FirstOrDefault();

            if (codigoVerificacion == null)
            {
                ViewBag.Error = "Código inválido";
                var usuario = db.Usuarios.Find(idUsuario);
                ViewBag.Correo = usuario.Email;
                ViewBag.NombreUsuario = usuario.Email.Split('@')[0];
                TempData.Keep("IdUsuario");
                return View();
            }

            if (DateTime.Now > codigoVerificacion.FechaExpiracion)
            {
                ViewBag.Error = "El código ha expirado";
                var usuario = db.Usuarios.Find(idUsuario);
                ViewBag.Correo = usuario.Email;
                ViewBag.NombreUsuario = usuario.Email.Split('@')[0];
                TempData.Keep("IdUsuario");
                return View();
            }

            // Marcar como verificado
            codigoVerificacion.Usado = true;
            var usuarioVerificado = db.Usuarios.Find(idUsuario);
            usuarioVerificado.Activo = true; // CAMBIO: Usar Activo
            db.SaveChanges();

            TempData["Mensaje"] = "¡Email verificado correctamente! ✅";
            return RedirectToAction("Index");
        }

        // Método auxiliar para generar código aleatorio
        private string GenerarCodigoAleatorio()
        {
            Random random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}