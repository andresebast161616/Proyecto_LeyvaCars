using Proyecto_Leyva_Cars.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace Proyecto_Leyva_Cars.Services
{
    public class ServicioBusquedaProductos
    {
        // CAMBIO: Usar el contexto correcto del .edmx
        private readonly LeyvaCarEntities _context;

        public ServicioBusquedaProductos()
        {
            _context = new LeyvaCarEntities();
        }

        public List<Productos> BuscarPorNombres(List<string> nombres, string vehiculo)
        {
            if (nombres == null || !nombres.Any())
                return new List<Productos>();

            try
            {
                // Construir consulta dinámica
                var query = _context.Productos
                    .Where(p => p.Activo == true && p.Stock > 0);

                // Buscar por coincidencias en nombre o descripción
                IQueryable<Productos> resultados = null;

                foreach (var nombre in nombres)
                {
                    var nombreLimpio = nombre.Trim().ToLower();
                    if (string.IsNullOrEmpty(nombreLimpio)) continue;

                    var consulta = query.Where(p =>
                        p.Nombre.ToLower().Contains(nombreLimpio) ||
                        p.Descripcion.ToLower().Contains(nombreLimpio) ||
                        p.Categoria.ToLower().Contains(nombreLimpio)
                    );

                    if (resultados == null)
                        resultados = consulta;
                    else
                        resultados = resultados.Union(consulta);
                }

                if (resultados == null)
                    return new List<Productos>();

                // Obtener resultados y calcular puntuación
                var productos = resultados.ToList();
                var productosConPuntuacion = new List<ProductoConPuntuacion>();

                foreach (var producto in productos)
                {
                    var puntuacion = CalcularPuntuacion(producto, nombres, vehiculo);
                    productosConPuntuacion.Add(new ProductoConPuntuacion
                    {
                        Producto = producto,
                        Puntuacion = puntuacion
                    });
                }

                // Ordenar por puntuación y retornar los mejores
                return productosConPuntuacion
                    .OrderByDescending(p => p.Puntuacion)
                    .Take(10)
                    .Select(p => p.Producto)
                    .ToList();
            }
            catch (Exception)
            {
                return new List<Productos>();
            }
        }

        private int CalcularPuntuacion(Productos producto, List<string> nombres, string vehiculo)
        {
            var puntuacion = 0;
            var nombreProducto = producto.Nombre?.ToLower() ?? "";
            var descripcionProducto = producto.Descripcion?.ToLower() ?? "";
            var compatibilidad = producto.ModelosCompatibles?.ToLower() ?? "";

            // Puntos por coincidencia exacta en nombre (50 puntos máx)
            foreach (var nombre in nombres)
            {
                var nombreLimpio = nombre.ToLower().Trim();
                if (nombreProducto.Contains(nombreLimpio))
                {
                    if (nombreProducto.Equals(nombreLimpio))
                        puntuacion += 50; // Coincidencia exacta
                    else if (nombreProducto.StartsWith(nombreLimpio) || nombreProducto.EndsWith(nombreLimpio))
                        puntuacion += 35; // Coincidencia al inicio o final
                    else
                        puntuacion += 20; // Coincidencia parcial
                }
            }

            // Puntos por coincidencia en descripción (30 puntos máx)
            foreach (var nombre in nombres)
            {
                var nombreLimpio = nombre.ToLower().Trim();
                if (descripcionProducto.Contains(nombreLimpio))
                {
                    puntuacion += 15;
                }
            }

            // Puntos por compatibilidad con vehículo (20 puntos máx)
            if (!string.IsNullOrEmpty(vehiculo))
            {
                var vehiculoLimpio = vehiculo.ToLower();
                var palabrasVehiculo = vehiculoLimpio.Split(' ');

                foreach (var palabra in palabrasVehiculo)
                {
                    if (palabra.Length > 2 && compatibilidad.Contains(palabra))
                    {
                        puntuacion += 5;
                    }
                }
            }

            // Puntos por disponibilidad (10 puntos máx)
            if (producto.Stock > 10)
                puntuacion += 10;
            else if (producto.Stock > 5)
                puntuacion += 7;
            else if (producto.Stock > 0)
                puntuacion += 3;

            // Puntos por marca reconocida (5 puntos máx)
            var marcasConocidas = new[] { "monroe", "brembo", "bosch", "denso", "ngk", "kyb", "sachs" };
            var marcaProducto = producto.Marca?.ToLower() ?? "";

            if (marcasConocidas.Any(m => marcaProducto.Contains(m)))
            {
                puntuacion += 5;
            }

            return puntuacion;
        }

        public List<Productos> BuscarPorTexto(string texto, int limite = 20)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return new List<Productos>();

            var textoLimpio = texto.ToLower().Trim();

            return _context.Productos
                .Where(p => p.Activo == true &&
                           (p.Nombre.ToLower().Contains(textoLimpio) ||
                            p.Descripcion.ToLower().Contains(textoLimpio) ||
                            p.Codigo.ToLower().Contains(textoLimpio)))
                .OrderBy(p => p.Nombre)
                .Take(limite)
                .ToList();
        }

        public Productos ObtenerPorId(int id)
        {
            return _context.Productos.Find(id);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }

        // NUEVOS MÉTODOS PARA LA PÁGINA PRINCIPAL
        public List<Productos> ObtenerProductosRecientes(int cantidad = 10)
        {
            try
            {
                return _context.Productos
                    .Where(p => p.Activo == true && p.Stock > 0)
                    .OrderByDescending(p => p.FechaCreacion ?? DateTime.MinValue)
                    .ThenByDescending(p => p.Id)
                    .Take(cantidad)
                    .ToList();
            }
            catch (Exception)
            {
                return new List<Productos>();
            }
        }

        public List<Productos> ObtenerProductosPorCategoria(string categoria, int cantidad = 8)
        {
            try
            {
                var query = _context.Productos
                    .Where(p => p.Activo == true && p.Stock > 0);

                if (!string.IsNullOrEmpty(categoria))
                {
                    query = query.Where(p => p.Categoria.ToLower().Contains(categoria.ToLower()));
                }

                return query
                    .OrderByDescending(p => p.FechaCreacion ?? DateTime.MinValue)
                    .Take(cantidad)
                    .ToList();
            }
            catch (Exception)
            {
                return new List<Productos>();
            }
        }

        public List<string> ObtenerTodasLasCategorias()
        {
            try
            {
                return _context.Productos
                    .Where(p => p.Activo == true && !string.IsNullOrEmpty(p.Categoria))
                    .Select(p => p.Categoria)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToList();
            }
            catch (Exception)
            {
                return new List<string>();
            }
        }

        public List<string> ObtenerTodasLasMarcas()
        {
            try
            {
                return _context.Productos
                    .Where(p => p.Activo == true && !string.IsNullOrEmpty(p.Marca))
                    .Select(p => p.Marca)
                    .Distinct()
                    .OrderBy(m => m)
                    .ToList();
            }
            catch (Exception)
            {
                return new List<string>();
            }
        }
    }

    internal class ProductoConPuntuacion
    {
        public Productos Producto { get; set; }
        public int Puntuacion { get; set; }
    }
}