using Proyecto_Leyva_Cars.Models;
using Proyecto_Leyva_Cars.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Proyecto_Leyva_Cars.Controllers
{
    public class ProductosController : Controller
    {
        private readonly ServicioBusquedaProductos _servicioProductos;

        public ProductosController()
        {
            _servicioProductos = new ServicioBusquedaProductos();
        }

        // GET: Productos - Catálogo principal
        public ActionResult Index(string busqueda = "", string categoria = "", 
            decimal? precioMin = null, decimal? precioMax = null, string orden = "", int pagina = 1)
        {
            try
            {
                const int productosPorPagina = 6;
                
                // Obtener productos filtrados
                var productos = _servicioProductos.ObtenerProductosFiltrados(
                    busqueda, categoria, precioMin, precioMax, orden, pagina, productosPorPagina);
                
                // Obtener total para paginación
                var totalProductos = _servicioProductos.ContarProductosFiltrados(
                    busqueda, categoria, precioMin, precioMax);
                
                // Obtener categorías para el filtro
                var categorias = _servicioProductos.ObtenerTodasLasCategorias();
                
                // Obtener marcas para filtros adicionales
                var marcas = _servicioProductos.ObtenerTodasLasMarcas();
                
                // Calcular paginación
                var totalPaginas = (int)Math.Ceiling((double)totalProductos / productosPorPagina);
                
                // Pasar datos a la vista
                ViewBag.Productos = productos;
                ViewBag.Categorias = categorias;
                ViewBag.Marcas = marcas;
                ViewBag.TotalProductos = totalProductos;
                ViewBag.PaginaActual = pagina;
                ViewBag.TotalPaginas = totalPaginas;
                ViewBag.ProductosPorPagina = productosPorPagina;
                
                // Mantener filtros actuales
                ViewBag.BusquedaActual = busqueda;
                ViewBag.CategoriaActual = categoria;
                ViewBag.PrecioMinActual = precioMin;
                ViewBag.PrecioMaxActual = precioMax;
                ViewBag.OrdenActual = orden;
                
                return View();
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error al cargar los productos: " + ex.Message;
                ViewBag.Productos = new List<Productos>();
                ViewBag.Categorias = new List<string>();
                ViewBag.Marcas = new List<string>();
                return View();
            }
        }

        // GET: Productos/Detalle/5
        public ActionResult Detalle(int id)
        {
            try
            {
                var producto = _servicioProductos.ObtenerProductoPorId(id);
                
                if (producto == null)
                {
                    return HttpNotFound("Producto no encontrado");
                }
                
                // Obtener productos relacionados de la misma categoría
                var productosRelacionados = _servicioProductos.ObtenerProductosRelacionados(id, producto.Categoria, 6);
                
                ViewBag.ProductosRelacionados = productosRelacionados;
                
                return View(producto);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error al cargar el producto: " + ex.Message;
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