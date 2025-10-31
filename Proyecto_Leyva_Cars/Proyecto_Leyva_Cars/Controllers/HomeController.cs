using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Proyecto_Leyva_Cars.Services;
using Proyecto_Leyva_Cars.Models;

namespace Proyecto_LeyvaCar_MVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly ServicioBusquedaProductos _servicioProductos;

        public HomeController()
        {
            _servicioProductos = new ServicioBusquedaProductos();
        }

        // GET: Home
        public ActionResult Index()
        {
            try
            {
                // Obtener productos recientes para mostrar en "Nuevos Ingresos"
                var productosRecientes = _servicioProductos.ObtenerProductosRecientes(12);
                
                // Obtener categorías para posible uso futuro
                var categorias = _servicioProductos.ObtenerTodasLasCategorias();
                
                // Obtener marcas para el carrusel
                var marcas = _servicioProductos.ObtenerTodasLasMarcas();

                // Pasar datos a la vista
                ViewBag.ProductosRecientes = productosRecientes;
                ViewBag.Categorias = categorias;
                ViewBag.Marcas = marcas;

                return View();
            }
            catch (Exception ex)
            {
                // En caso de error, pasar listas vacías
                ViewBag.ProductosRecientes = new List<Productos>();
                ViewBag.Categorias = new List<string>();
                ViewBag.Marcas = new List<string>();
                
                return View();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _servicioProductos?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}