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

        // GET: Usuarios - SOLO PARA ADMINISTRADORES
        public ActionResult Index()
        {
            // TEMPORAL: Bloquear acceso hasta implementar roles de admin
            return RedirectToAction("Index", "Home");
            
            // TODO: Agregar verificación de rol de administrador
            // var usuarios = db.Usuarios.ToList();
            // return View(usuarios);
        }

        // GET: Usuarios/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Usuarios/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(string NombreUsuario, string Correo, string Contrasena)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Verificar si el correo ya existe
                    if (db.Usuarios.Any(u => u.Email == Correo))
                    {
                        ModelState.AddModelError("Correo", "Este correo ya está registrado");
                        ViewBag.NombreUsuario = NombreUsuario;
                        ViewBag.Correo = Correo;
                        return View();
                    }

                    // MAPEAR correctamente los campos del formulario al modelo
                    var usuario = new Usuarios
                    {
                        Nombre = NombreUsuario,        // NombreUsuario -> Nombre
                        Apellido = "",                 // Campo vacío por ahora
                        Email = Correo,               // Correo -> Email  
                        PasswordHash = Contrasena,    // Contrasena -> PasswordHash (deberías hashearla)
                        FechaRegistro = DateTime.Now,
                        Activo = false
                    };

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
                catch (System.Data.Entity.Validation.DbEntityValidationException ex)
                {
                    // CAPTURAR DETALLES DEL ERROR
                    string errorDetails = "";
                    foreach (var validationErrors in ex.EntityValidationErrors)
                    {
                        foreach (var validationError in validationErrors.ValidationErrors)
                        {
                            errorDetails += $"Campo: {validationError.PropertyName}, Error: {validationError.ErrorMessage}\n";
                        }
                    }
                    
                    // Mostrar el error en la vista
                    ModelState.AddModelError("", "Error de validación: " + errorDetails);
                    return View();
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error general: " + ex.Message);
                    return View();
                }
            }
            return View();
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

            TempData["MensajeExito"] = "¡Email verificado correctamente! Ya puedes iniciar sesión.";
            
            // CAMBIO: Redirigir a Login en lugar de Index
            return RedirectToAction("Login", "Usuarios");
        }

        // GET: Usuarios/Login
        public ActionResult Login()
        {
            // Si ya está logueado, redirigir al Home
            if (Session["UsuarioId"] != null)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // POST: Usuarios/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string Email, string Contrasena)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Contrasena))
                {
                    ModelState.AddModelError("", "Email y contraseña son requeridos");
                    return View();
                }

                // Buscar usuario por email
                var usuario = db.Usuarios
                    .Where(u => u.Email == Email && u.PasswordHash == Contrasena)
                    .FirstOrDefault();

                if (usuario == null)
                {
                    ModelState.AddModelError("", "Email o contraseña incorrectos");
                    return View();
                }

                if (usuario.Activo != true)
                {
                    ModelState.AddModelError("", "Debes verificar tu email antes de iniciar sesión");
                    return View();
                }

                // CREAR SESIÓN
                Session["UsuarioId"] = usuario.Id_Usuario;
                Session["UsuarioNombre"] = usuario.Nombre;
                Session["UsuarioEmail"] = usuario.Email;

                TempData["MensajeExito"] = $"¡Bienvenido {usuario.Nombre}!";
                
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al iniciar sesión: " + ex.Message);
                return View();
            }
        }

        // GET: Usuarios/Logout
        public ActionResult Logout()
        {
            // Limpiar sesión
            Session.Clear();
            Session.Abandon();
            
            TempData["Mensaje"] = "Has cerrado sesión correctamente";
            return RedirectToAction("Index", "Home");
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