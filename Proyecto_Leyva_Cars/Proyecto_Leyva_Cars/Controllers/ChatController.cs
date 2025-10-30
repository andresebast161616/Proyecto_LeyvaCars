using Newtonsoft.Json;
using Proyecto_Leyva_Cars.Models;
using Proyecto_Leyva_Cars.Services;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Proyecto_Leyva_Cars.Controllers
{
    public class ChatController : Controller
    {
        private readonly ServicioGemini _servicioGemini;
        private readonly ServicioBusquedaProductos _servicioProductos;
        private LeyvaCarEntities db = new LeyvaCarEntities();
        
        private string NumeroWhatsApp => ConfigurationManager.AppSettings["WhatsAppBusinessPhone"]?.Replace("+", "").Replace(" ", "") ?? "51901986806";

        public ChatController()
        {
            _servicioGemini = new ServicioGemini();
            _servicioProductos = new ServicioBusquedaProductos();
        }

        public ActionResult Index()
        {
            ViewBag.Title = "Chat IA - Identificar Pieza";
            return View();
        }

        [HttpPost]
        public async Task<JsonResult> ProcesarImagenYBuscar()
        {
            try
            {
                var mensaje = Request.Form["mensaje"];
                var marcaVehiculo = Request.Form["marcaVehiculo"];
                var modeloVehiculo = Request.Form["modeloVehiculo"];
                var anioVehiculo = Request.Form["anioVehiculo"];

                string rutaImagen = null;
                byte[] imageBytes = null;
                string mimeType = null;

                // Procesar imagen si existe
                if (Request.Files.Count > 0 && Request.Files[0].ContentLength > 0)
                {
                    var archivo = Request.Files[0];

                    // Validar tipo de archivo
                    var tiposPermitidos = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
                    if (!tiposPermitidos.Contains(archivo.ContentType))
                    {
                        return Json(new { success = false, error = "Tipo de imagen no válido" });
                    }

                    // Validar tamaño (máx 4MB)
                    if (archivo.ContentLength > 4 * 1024 * 1024)
                    {
                        return Json(new { success = false, error = "La imagen es muy grande. Máximo 4MB" });
                    }

                    // Guardar imagen
                    var carpetaUploads = Server.MapPath("~/uploads/consultas/");
                    if (!Directory.Exists(carpetaUploads))
                        Directory.CreateDirectory(carpetaUploads);

                    var nombreArchivo = $"{Guid.NewGuid()}{Path.GetExtension(archivo.FileName)}";
                    rutaImagen = Path.Combine(carpetaUploads, nombreArchivo);
                    archivo.SaveAs(rutaImagen);

                    // Leer bytes para Gemini
                    imageBytes = System.IO.File.ReadAllBytes(rutaImagen);
                    mimeType = archivo.ContentType;

                    // Guardar ruta relativa
                    rutaImagen = "/uploads/consultas/" + nombreArchivo;
                }

                // NUEVO: Llamar a Gemini con análisis completo
                var respuestaIA = await _servicioGemini.AnalizarPiezaAsync(imageBytes, mimeType, $"{marcaVehiculo} {modeloVehiculo} {anioVehiculo}");
                
                // DEBUG: Para ver qué recibe el controlador
                System.Diagnostics.Debug.WriteLine("=== DEBUG CHATCONTROLLER ===");
                System.Diagnostics.Debug.WriteLine($"Descripción: {respuestaIA.DescripcionNatural}");
                System.Diagnostics.Debug.WriteLine($"Nombres detectados: [{string.Join(", ", respuestaIA.NombresDetectados)}]");
                System.Diagnostics.Debug.WriteLine("============================");                // Buscar productos en BD usando los nombres detectados
                var productosEncontrados = new List<Productos>();
                if (respuestaIA.NombresDetectados?.Any() == true)
                {
                    productosEncontrados = _servicioProductos.BuscarPorNombres(
                        respuestaIA.NombresDetectados,
                        $"{marcaVehiculo} {modeloVehiculo} {anioVehiculo}"
                    );
                }

                // Registrar consulta en BD
                var consulta = new Consultas
                {
                    MarcaVehiculo = marcaVehiculo,
                    ModeloVehiculo = modeloVehiculo,
                    AnioVehiculo = int.Parse(anioVehiculo ?? "0"),
                    RutaImagen = rutaImagen,
                    NombresDetectadosIA = string.Join(",", respuestaIA.NombresDetectados ?? new List<string>()),
                    TipoConsulta = productosEncontrados.Any() ? "EncontradoEnBD" : "BusquedaManual",
                    Estado = "Pendiente",
                    FechaConsulta = DateTime.Now
                };

                db.Consultas.Add(consulta);
                db.SaveChanges();

                // TEMPORAL: Obtener respuesta RAW de la misma llamada (esto es redundante pero para debug)
                var promptSimple = "Identifica esta pieza automotriz";
                var respuestaRaw = await _servicioGemini.ProcesarImagenAsync(promptSimple, imageBytes, mimeType);

                return Json(new
                {
                    success = true,
                    consultaId = consulta.Id,
                    descripcionIA = respuestaIA.DescripcionNatural,    // NUEVO: Descripción natural
                    nombresDetectados = respuestaIA.NombresDetectados, // NUEVO: Para debug
                    respuestaRaw = respuestaRaw,                       // TEMPORAL: Para debug
                    productos = productosEncontrados.Select(p => new
                    {
                        id = p.Id,
                        codigo = p.Codigo,
                        nombre = p.Nombre,
                        precio = p.Precio,
                        stock = p.Stock,
                        marca = p.Marca,
                        imagen = p.UrlImagen,
                        compatibilidad = p.ModelosCompatibles
                    }).ToList(),
                    tieneResultados = productosEncontrados.Any()
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult GenerarConsultaWhatsApp(int productoId, int consultaId)
        {
            try
            {
                var consulta = db.Consultas.Find(consultaId);
                var producto = db.Productos.Find(productoId);

                if (consulta == null || producto == null)
                    return Json(new { success = false, error = "Datos no encontrados" });

                var mensaje = $@"Hola! Encontré esta pieza en su sistema:

🚗 VEHÍCULO:
- {consulta.MarcaVehiculo} {consulta.ModeloVehiculo} {consulta.AnioVehiculo}

📦 PRODUCTO:
- Código: {producto.Codigo}
- {producto.Nombre}
- Precio: S/ {producto.Precio:F2}

❓ CONSULTA:
¿Esta pieza es compatible con mi vehículo?
¿Está disponible para entrega inmediata?";

                var urlWhatsApp = $"https://wa.me/{NumeroWhatsApp}?text={Uri.EscapeDataString(mensaje)}";

                // Actualizar consulta
                consulta.ProductoId = productoId;
                consulta.MensajeWhatsApp = mensaje;
                consulta.TipoConsulta = "EncontradoEnBD";
                db.SaveChanges();

                return Json(new { success = true, urlWhatsApp = urlWhatsApp });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult ConsultarPedidoEspecial(int consultaId)
        {
            try
            {
                var consulta = db.Consultas.Find(consultaId);
                if (consulta == null)
                    return Json(new { success = false, error = "Consulta no encontrada" });

                var nombres = (consulta.NombresDetectadosIA ?? "").Split(',').Where(n => !string.IsNullOrEmpty(n)).ToList();
                var nombrePrincipal = nombres.FirstOrDefault() ?? "Pieza automotriz";

                var mensaje = $@"Hola! No encontré esta pieza en su catálogo:

🚗 VEHÍCULO: {consulta.MarcaVehiculo} {consulta.ModeloVehiculo} {consulta.AnioVehiculo}
🔍 PIEZA IDENTIFICADA: {nombrePrincipal}

❓ ¿Pueden conseguir esta pieza por pedido especial?
¿Cuánto costaría y tiempo de entrega?";

                var urlWhatsApp = $"https://wa.me/{NumeroWhatsApp}?text={Uri.EscapeDataString(mensaje)}";

                // Actualizar consulta
                consulta.MensajeWhatsApp = mensaje;
                consulta.TipoConsulta = "PedidoEspecial";
                db.SaveChanges();

                return Json(new { success = true, urlWhatsApp = urlWhatsApp });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
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