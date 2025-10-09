using Proyecto_Leyva_Cars.Models;
using System;
using System.Linq;
using System.Web.Mvc;

namespace Proyecto_Leyva_Cars.Controllers
{
    public class UsuariosController : Controller
    {
        private ModeloSistema db = new ModeloSistema();

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
                // Verificar si el correo ya existe
                if (db.Usuarios.Any(u => u.Correo == usuario.Correo))
                {
                    ModelState.AddModelError("Correo", "Este correo ya está registrado");
                    return View(usuario);
                }

                // Crear usuario (sin verificar aún)
                usuario.FechaRegistro = DateTime.Now;
                usuario.EmailVerificado = false;
                db.Usuarios.Add(usuario);
                db.SaveChanges();

                // Generar código de verificación
                string codigo = GenerarCodigoAleatorio();
                var codigoVerificacion = new CodigoVerificacion
                {
                    IdUsuario = usuario.IdUsuario,
                    Codigo = codigo,
                    FechaCreacion = DateTime.Now,
                    FechaExpiracion = DateTime.Now.AddMinutes(15), // Válido por 15 minutos
                    Verificado = false
                };
                db.CodigosVerificacion.Add(codigoVerificacion);
                db.SaveChanges();

                // Guardar datos en TempData para la vista de verificación
                TempData["IdUsuario"] = usuario.IdUsuario;
                TempData["Correo"] = usuario.Correo;
                TempData["Codigo"] = codigo; // Para enviarlo por EmailJS

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
            ViewBag.NombreUsuario = correo.Split('@')[0]; // 👈 AGREGADO: Extraer nombre del correo
            TempData.Keep("IdUsuario"); // Mantener para el POST

            return View();
        }

        // POST: Usuarios/VerificarEmail
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult VerificarEmail(string codigo)
        {
            int idUsuario = (int)TempData["IdUsuario"];

            var codigoVerificacion = db.CodigosVerificacion
                .Where(c => c.IdUsuario == idUsuario && c.Codigo == codigo && !c.Verificado)
                .OrderByDescending(c => c.FechaCreacion)
                .FirstOrDefault();

            if (codigoVerificacion == null)
            {
                ViewBag.Error = "Código inválido";
                // 👇 AGREGADO: Mantener datos en ViewBag para mostrar en la vista
                var usuario = db.Usuarios.Find(idUsuario);
                ViewBag.Correo = usuario.Correo;
                ViewBag.NombreUsuario = usuario.Correo.Split('@')[0];
                TempData.Keep("IdUsuario");
                return View();
            }

            if (DateTime.Now > codigoVerificacion.FechaExpiracion)
            {
                ViewBag.Error = "El código ha expirado";
                // 👇 AGREGADO: Mantener datos en ViewBag para mostrar en la vista
                var usuario = db.Usuarios.Find(idUsuario);
                ViewBag.Correo = usuario.Correo;
                ViewBag.NombreUsuario = usuario.Correo.Split('@')[0];
                TempData.Keep("IdUsuario");
                return View();
            }

            // Marcar como verificado
            codigoVerificacion.Verificado = true;
            var usuarioVerificado = db.Usuarios.Find(idUsuario);
            usuarioVerificado.EmailVerificado = true;
            db.SaveChanges();

            TempData["Mensaje"] = "¡Email verificado correctamente! ✅";
            return RedirectToAction("Index");
        }

        // Método auxiliar para generar código aleatorio
        private string GenerarCodigoAleatorio()
        {
            Random random = new Random();
            return random.Next(100000, 999999).ToString(); // Código de 6 dígitos
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