using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Proyecto_Leyva_Cars.Services
{
    public class ServicioGemini
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _modelo;

        public ServicioGemini()
        {
            _httpClient = new HttpClient();
            _apiKey = ConfigurationManager.AppSettings["GeminiApiKey"] ?? "AIzaSyCPwIAQ1Xgeaf1AIuQZ2b9DBjO-Piaynog";
            _modelo = ConfigurationManager.AppSettings["GeminiModel"] ?? "gemini-2.0-flash-exp";
        }

        public async Task<string> ProcesarImagenAsync(string mensaje, byte[] imageBytes = null, string mimeType = null)
        {
            try
            {
                // DEBUG: Ver qué prompt se envía
                System.Diagnostics.Debug.WriteLine("=== PROMPT ENVIADO A GEMINI ===");
                System.Diagnostics.Debug.WriteLine(mensaje);
                System.Diagnostics.Debug.WriteLine("==============================");
                
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_modelo}:generateContent?key={_apiKey}";

                var parts = new List<object>();

                // Agregar imagen si existe
                if (imageBytes != null && mimeType != null)
                {
                    parts.Add(new
                    {
                        inline_data = new
                        {
                            mime_type = mimeType,
                            data = Convert.ToBase64String(imageBytes)
                        }
                    });
                }

                // Agregar texto
                parts.Add(new { text = mensaje });

                var payload = new
                {
                    contents = new[]
                    {
                        new { parts = parts }
                    }
                };

                // TEMPORALMENTE: Usar string simple sin JSON
                var jsonPayload = BuildSimpleJsonPayload(mensaje, imageBytes, mimeType);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                
                // DEBUG: Ver respuesta completa de Gemini
                System.Diagnostics.Debug.WriteLine("=== RESPUESTA COMPLETA DE GEMINI ===");
                System.Diagnostics.Debug.WriteLine(responseContent);
                System.Diagnostics.Debug.WriteLine("===================================");
                
                // TEMPORALMENTE: Extraer texto directamente
                var responseObj = ExtractTextFromResponse(responseContent);
                
                // DEBUG: Ver texto extraído
                System.Diagnostics.Debug.WriteLine("=== TEXTO EXTRAÍDO ===");
                System.Diagnostics.Debug.WriteLine(responseObj);
                System.Diagnostics.Debug.WriteLine("=====================");

                return responseObj ?? "No se recibió respuesta válida de la API";
            }
            catch (Exception ex)
            {
                return $"Error al procesar con Gemini: {ex.Message}";
            }
        }

        // NUEVO: Método principal con DOS consultas separadas
        public async Task<RespuestaIA> AnalizarPiezaAsync(byte[] imageBytes, string mimeType, string vehiculo)
        {
            var resultado = new RespuestaIA
            {
                DescripcionNatural = "No se pudo identificar la pieza",
                NombresDetectados = new List<string> { "pieza automotriz" }
            };

            try
            {
                // ==========================================
                // 🗣️ PRIMERA CONSULTA: DESCRIPCIÓN NATURAL
                // ==========================================
                var promptDescripcion = @"Describe esta pieza automotriz de forma directa y técnica.

Incluye únicamente:
- Tipo de pieza y función principal
- Características físicas visibles (forma, color, material)
- Estado aparente (nuevo, usado, desgastado)
- Ubicación típica en el vehículo

Responde de forma concisa y técnica, sin saludos ni introducciones:";

                System.Diagnostics.Debug.WriteLine("=== PRIMERA CONSULTA: DESCRIPCIÓN ===");
                var descripcion = await ProcesarImagenAsync(promptDescripcion, imageBytes, mimeType);
                
                if (!string.IsNullOrEmpty(descripcion) && descripcion != "No se recibió respuesta válida de la API")
                {
                    // NUEVO: Limpiar la descripción de caracteres especiales
                    var descripcionLimpia = descripcion
                        .Replace("\\n", " ")
                        .Replace("\n", " ")
                        .Replace("\\r", " ")
                        .Replace("\r", " ")
                        .Replace("\\", "")
                        .Replace("**", "")
                        .Replace("¡Claro!", "")
                        .Replace("Con gusto", "")
                        .Replace("te explico", "")
                        .Replace("¡", "")
                        .Replace("!", "")
                        .Trim();
                    
                    // Remover frases de saludo comunes
                    if (descripcionLimpia.StartsWith("Con gusto") || 
                        descripcionLimpia.StartsWith("Claro") ||
                        descripcionLimpia.StartsWith("Por supuesto"))
                    {
                        var puntoIndex = descripcionLimpia.IndexOf('.');
                        if (puntoIndex > 0 && puntoIndex < descripcionLimpia.Length - 1)
                        {
                            descripcionLimpia = descripcionLimpia.Substring(puntoIndex + 1).Trim();
                        }
                    }
                    
                    resultado.DescripcionNatural = descripcionLimpia;
                }

                // ==========================================
                // 🔍 SEGUNDA CONSULTA: NOMBRES PARA BÚSQUEDA  
                // ==========================================
                var promptNombres = @"Analiza esta imagen de pieza automotriz y responde ÚNICAMENTE con nombres técnicos separados por comas.

Dame 5 nombres diferentes que se usan para esta pieza, separados por comas:
- Nombre técnico principal
- Nombre comercial común
- Sinónimo en español
- Nombre en inglés  
- Variación del nombre

Ejemplo respuesta: pastilla de freno, balata, brake pad, pastilla freno, pastilla

RESPONDE SOLO LA LISTA DE NOMBRES SEPARADOS POR COMAS, SIN TEXTO ADICIONAL:";

                System.Diagnostics.Debug.WriteLine("=== SEGUNDA CONSULTA: NOMBRES ===");
                var respuestaNombres = await ProcesarImagenAsync(promptNombres, imageBytes, mimeType);
                
                if (!string.IsNullOrEmpty(respuestaNombres) && respuestaNombres != "No se recibió respuesta válida de la API")
                {
                    // Procesar nombres separados por comas
                    var nombres = ProcesarNombresSeparadosPorComas(respuestaNombres);
                    
                    if (nombres.Count > 0)
                    {
                        resultado.NombresDetectados = nombres;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"=== RESULTADO FINAL ===");
                System.Diagnostics.Debug.WriteLine($"Descripción: {resultado.DescripcionNatural}");
                System.Diagnostics.Debug.WriteLine($"Nombres: [{string.Join(", ", resultado.NombresDetectados)}]");
                System.Diagnostics.Debug.WriteLine("=====================");

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR en AnalizarPiezaAsync: {ex.Message}");
                // Mantener valores por defecto
            }

            return resultado;
        }

        public async Task<List<string>> ExtraerNombresPiezasAsync(byte[] imageBytes, string mimeType, string vehiculo)
        {
            // ACTUALIZADO: Ahora usa el nuevo método pero mantiene compatibilidad
            var respuesta = await AnalizarPiezaAsync(imageBytes, mimeType, vehiculo);
            return respuesta.NombresDetectados;
        }

        // NUEVO: Procesar respuesta JSON directa
        private RespuestaIA ProcesarRespuestaConArreglo(string respuesta)
        {
            var resultado = new RespuestaIA
            {
                DescripcionNatural = "No se pudo identificar la pieza",
                NombresDetectados = new List<string> { "pieza automotriz" }
            };

            if (string.IsNullOrEmpty(respuesta))
                return resultado;

            try
            {
                // DEBUG: Para ver qué devuelve exactamente la IA
                System.Diagnostics.Debug.WriteLine("=== DEBUG RESPUESTA IA ===");
                System.Diagnostics.Debug.WriteLine($"Respuesta Original: {respuesta}");
                System.Diagnostics.Debug.WriteLine("========================");

                // NUEVO: Intentar procesar como JSON array directamente
                var jsonArray = ParsearJsonArrayGemini(respuesta);
                
                if (jsonArray != null && jsonArray.Count >= 5)
                {
                    // Índice 0: Descripción
                    resultado.DescripcionNatural = jsonArray[0];
                    
                    // Índices 1-4: Nombres para búsqueda
                    resultado.NombresDetectados = new List<string>();
                    for (int i = 1; i < 5; i++)
                    {
                        if (!string.IsNullOrWhiteSpace(jsonArray[i]))
                        {
                            resultado.NombresDetectados.Add(jsonArray[i].Trim().ToLower());
                        }
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"DEBUG - Descripción extraída: {resultado.DescripcionNatural}");
                    System.Diagnostics.Debug.WriteLine($"DEBUG - Nombres extraídos: [{string.Join(", ", resultado.NombresDetectados)}]");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("DEBUG - No se pudo parsear como JSON, intentando extracción inteligente");
                    // Extracción inteligente de nombres de texto normal
                    var nombresExtraidos = ExtraerNombresDeTextoNormal(respuesta);
                    
                    if (nombresExtraidos.Count > 0)
                    {
                        resultado.DescripcionNatural = $"Pieza automotriz identificada: {nombresExtraidos[0]}";
                        resultado.NombresDetectados = nombresExtraidos;
                        System.Diagnostics.Debug.WriteLine($"DEBUG - Nombres inteligentes extraídos: [{string.Join(", ", nombresExtraidos)}]");
                    }
                    else
                    {
                        // Último recurso: método manual
                        resultado.NombresDetectados = ExtraerNombresManual(respuesta);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DEBUG - Error procesando respuesta: {ex.Message}");
                // Si hay error, intentar extracción manual
                resultado.NombresDetectados = ExtraerNombresManual(respuesta);
            }

            return resultado;
        }

        // NUEVO: Procesar nombres separados por comas (más simple)
        private List<string> ProcesarNombresSeparadosPorComas(string respuesta)
        {
            var nombres = new List<string>();
            
            try
            {
                System.Diagnostics.Debug.WriteLine($"DEBUG - Procesando nombres de: '{respuesta}'");
                
                // Limpiar la respuesta de caracteres problemáticos
                var respuestaLimpia = respuesta
                    .Replace("\\n", " ")
                    .Replace("\n", " ")
                    .Replace("\\", "")
                    .Trim();
                
                // Dividir por comas
                var partes = respuestaLimpia.Split(',');
                
                foreach (var parte in partes)
                {
                    var nombreLimpio = parte.Trim()
                                          .ToLower()
                                          .Replace(".", "")
                                          .Replace(":", "")
                                          .Replace(";", "")
                                          .Replace("\"", "")
                                          .Replace("'", "")
                                          .Trim();
                    
                    // Solo agregar si es válido y no duplicado
                    if (!string.IsNullOrEmpty(nombreLimpio) && 
                        nombreLimpio.Length > 2 && 
                        !nombres.Contains(nombreLimpio))
                    {
                        nombres.Add(nombreLimpio);
                        System.Diagnostics.Debug.WriteLine($"DEBUG - Nombre agregado: '{nombreLimpio}'");
                    }
                }
                
                // Si no se encontraron nombres válidos, usar método manual como respaldo
                if (nombres.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("DEBUG - No se encontraron nombres, usando método manual");
                    nombres = ExtraerNombresManual(respuesta);
                }
                
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DEBUG - Error procesando nombres: {ex.Message}");
                nombres = ExtraerNombresManual(respuesta);
            }
            
            System.Diagnostics.Debug.WriteLine($"DEBUG - Nombres finales: [{string.Join(", ", nombres)}]");
            return nombres;
        }

        // NUEVO: Parser específico para arrays JSON de Gemini
        private List<string> ParsearJsonArrayGemini(string respuesta)
        {
            if (string.IsNullOrEmpty(respuesta))
                return null;

            try
            {
                // Buscar el inicio y fin del array JSON
                var inicioArray = respuesta.IndexOf('[');
                var finArray = respuesta.LastIndexOf(']');
                
                if (inicioArray >= 0 && finArray > inicioArray)
                {
                    var jsonContent = respuesta.Substring(inicioArray, finArray - inicioArray + 1);
                    System.Diagnostics.Debug.WriteLine($"DEBUG - JSON extraído: {jsonContent}");
                    
                    // Usar el método existente ParseJsonArrayManually
                    return ParseJsonArrayManually(jsonContent);
                }
                
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DEBUG - Error parseando JSON: {ex.Message}");
                return null;
            }
        }

        // NUEVO: Extraer nombres de texto normal cuando no viene en JSON
        private List<string> ExtraerNombresDeTextoNormal(string respuesta)
        {
            var nombres = new List<string>();
            
            if (string.IsNullOrEmpty(respuesta))
                return nombres;

            try
            {
                // Patrones comunes para identificar nombres de piezas
                var patrones = new[]
                {
                    @"\*\*([^*]+)\*\*",                        // **resorte helicoidal**
                    @"es un (.+?) o",                          // "es un resorte helicoidal o"
                    @"es un (.+?)\.",                          // "es un resorte helicoidal."
                    @"llamado (.+?) o",                        // "llamado resorte o"
                    @"conocido como (.+?) o",                  // "conocido como muelle o"
                    @"también llamado (.+?)\.",                // "también llamado muelle."
                    @"tipo (.+?) que",                         // "tipo resorte que"
                };

                foreach (var patron in patrones)
                {
                    var matches = System.Text.RegularExpressions.Regex.Matches(respuesta, patron, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    foreach (System.Text.RegularExpressions.Match match in matches)
                    {
                        if (match.Groups.Count > 1)
                        {
                            var nombre = match.Groups[1].Value.Trim().ToLower();
                            if (!string.IsNullOrEmpty(nombre) && nombre.Length > 2 && !nombres.Contains(nombre))
                            {
                                nombres.Add(nombre);
                            }
                        }
                    }
                }

                // Si no encontramos nombres con patrones, buscar palabras clave
                if (nombres.Count == 0)
                {
                    var palabrasClave = new[] { "resorte", "muelle", "spring", "amortiguador", "suspension", "helicoidal", "disco", "freno", "brake", "rotor" };
                    
                    foreach (var palabra in palabrasClave)
                    {
                        if (respuesta.ToLower().Contains(palabra))
                        {
                            nombres.Add(palabra);
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"DEBUG - Nombres extraídos con patrones: [{string.Join(", ", nombres)}]");
                return nombres;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DEBUG - Error en extracción inteligente: {ex.Message}");
                return nombres;
            }
        }

        // NUEVO: Limpiar respuesta de IA antes de procesar
        private string LimpiarRespuestaIA(string respuesta)
        {
            if (string.IsNullOrEmpty(respuesta))
                return "";

            return respuesta
                .Replace("\\n", "\n")           // Convertir \n literales a saltos reales
                .Replace("\\r", "\r")           // Convertir \r literales  
                .Replace("\\\"", "\"")          // Convertir \" literales a comillas
                .Replace("\\\n", "\n")          // Limpiar backslashes antes de saltos
                .Replace("\\", "")              // Quitar backslashes restantes
                .Trim();
        }

        // MEJORADO: Extraer nombres de formato arreglo ["nombre1", "nombre2"]
        private List<string> ExtraerNombresDeArreglo(string arregloTexto, string respuestaCompleta = "")
        {
            var nombres = new List<string>();
            
            try
            {
                // DEBUG: Ver texto sin procesar
                System.Diagnostics.Debug.WriteLine($"DEBUG - ArregloTexto entrada: '{arregloTexto}'");
                
                // MEJORADO: Buscar arreglo en todo el texto, no solo entre corchetes
                var textoCompleto = arregloTexto;
                
                // Si no tiene corchetes, buscar en toda la línea
                var inicioCorchete = textoCompleto.IndexOf('[');
                var finCorchete = textoCompleto.LastIndexOf(']');
                
                if (inicioCorchete >= 0 && finCorchete > inicioCorchete)
                {
                    var contenido = textoCompleto.Substring(inicioCorchete + 1, finCorchete - inicioCorchete - 1);
                    
                    // DEBUG: Ver contenido extraído
                    System.Diagnostics.Debug.WriteLine($"DEBUG - Contenido entre corchetes: '{contenido}'");
                    
                    // MEJORADO: Dividir por comas pero considerar comillas
                    var elementos = DividirElementosArreglo(contenido);
                    
                    foreach (var elemento in elementos)
                    {
                        var nombreLimpio = elemento.Trim()
                                                 .Trim('"')     // Quitar comillas dobles
                                                 .Trim('\'')    // Quitar comillas simples
                                                 .Trim()
                                                 .ToLower();
                        
                        // DEBUG: Ver cada nombre procesado
                        System.Diagnostics.Debug.WriteLine($"DEBUG - Nombre limpio: '{nombreLimpio}'");
                        
                        if (!string.IsNullOrEmpty(nombreLimpio) && nombreLimpio.Length > 1)
                        {
                            // No agregar duplicados
                            if (!nombres.Contains(nombreLimpio))
                            {
                                nombres.Add(nombreLimpio);
                            }
                        }
                    }
                }
                else
                {
                    // MEJORADO: Buscar arreglo en toda la respuesta, no solo en esta línea
                    System.Diagnostics.Debug.WriteLine("DEBUG - No se encontraron corchetes en línea, buscando en toda la respuesta");
                    
                    // Buscar ARREGLO: en toda la respuesta
                    if (!string.IsNullOrEmpty(respuestaCompleta))
                    {
                        var indiceArreglo = respuestaCompleta.IndexOf("ARREGLO:", StringComparison.OrdinalIgnoreCase);
                    
                    if (indiceArreglo >= 0)
                    {
                        var textoDesdeArreglo = respuestaCompleta.Substring(indiceArreglo);
                        var inicioCorcheteCompleto = textoDesdeArreglo.IndexOf('[');
                        var finCorcheteCompleto = textoDesdeArreglo.IndexOf(']');
                        
                        if (inicioCorcheteCompleto >= 0 && finCorcheteCompleto > inicioCorcheteCompleto)
                        {
                            var contenidoArreglo = textoDesdeArreglo.Substring(inicioCorcheteCompleto + 1, finCorcheteCompleto - inicioCorcheteCompleto - 1);
                            System.Diagnostics.Debug.WriteLine($"DEBUG - Arreglo encontrado en respuesta completa: '{contenidoArreglo}'");
                            
                            var elementos = DividirElementosArreglo(contenidoArreglo);
                            
                            foreach (var elemento in elementos)
                            {
                                var nombreLimpio = elemento.Trim().Trim('"').Trim('\'').Trim().ToLower();
                                if (!string.IsNullOrEmpty(nombreLimpio) && nombreLimpio.Length > 1 && !nombres.Contains(nombreLimpio))
                                {
                                    nombres.Add(nombreLimpio);
                                }
                            }
                        }
                    }
                }
                    
                // Si aún no hay nombres, intentar división directa
                if (nombres.Count == 0)
                    {
                        System.Diagnostics.Debug.WriteLine("DEBUG - Intentando división directa por comas");
                        var elementosDirectos = arregloTexto.Split(',');
                        
                        foreach (var elemento in elementosDirectos)
                        {
                            var nombreLimpio = elemento.Trim().Trim('"').Trim('\'').Trim().ToLower();
                            if (!string.IsNullOrEmpty(nombreLimpio) && nombreLimpio.Length > 1 && !nombres.Contains(nombreLimpio))
                            {
                                nombres.Add(nombreLimpio);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DEBUG - Error en ExtraerNombresDeArreglo: {ex.Message}");
                // Si falla el parsing del arreglo, intentar método manual
                return ExtraerNombresManual(arregloTexto);
            }
            
            System.Diagnostics.Debug.WriteLine($"DEBUG - Nombres finales: [{string.Join(", ", nombres)}]");
            return nombres.Count > 0 ? nombres : new List<string> { "pieza automotriz" };
        }

        // NUEVO: Dividir elementos del arreglo considerando comillas
        private List<string> DividirElementosArreglo(string contenido)
        {
            var elementos = new List<string>();
            var elemento = "";
            var dentroComillas = false;
            var tipoComilla = '"';
            
            for (int i = 0; i < contenido.Length; i++)
            {
                var caracter = contenido[i];
                
                if ((caracter == '"' || caracter == '\'') && !dentroComillas)
                {
                    dentroComillas = true;
                    tipoComilla = caracter;
                }
                else if (caracter == tipoComilla && dentroComillas)
                {
                    dentroComillas = false;
                }
                else if (caracter == ',' && !dentroComillas)
                {
                    if (!string.IsNullOrEmpty(elemento.Trim()))
                    {
                        elementos.Add(elemento.Trim());
                    }
                    elemento = "";
                    continue;
                }
                
                elemento += caracter;
            }
            
            // Agregar último elemento
            if (!string.IsNullOrEmpty(elemento.Trim()))
            {
                elementos.Add(elemento.Trim());
            }
            
            return elementos;
        }

        // MÉTODO LEGACY: Mantener para compatibilidad (por si el nuevo falla)
        private RespuestaIA ProcesarRespuestaDoble(string respuesta)
        {
            var resultado = new RespuestaIA
            {
                DescripcionNatural = "No se pudo identificar la pieza",
                NombresDetectados = new List<string> { "pieza automotriz" }
            };

            if (string.IsNullOrEmpty(respuesta))
                return resultado;

            try
            {
                var lineas = respuesta.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                
                foreach (var linea in lineas)
                {
                    var lineaLimpia = linea.Trim();
                    
                    // Buscar descripción
                    if (lineaLimpia.StartsWith("DESCRIPCIÓN:", StringComparison.OrdinalIgnoreCase) ||
                        lineaLimpia.StartsWith("DESCRIPCION:", StringComparison.OrdinalIgnoreCase))
                    {
                        var descripcion = lineaLimpia.Substring(lineaLimpia.IndexOf(':') + 1).Trim();
                        if (!string.IsNullOrEmpty(descripcion))
                        {
                            resultado.DescripcionNatural = descripcion;
                        }
                    }
                    
                    // Buscar nombres
                    if (lineaLimpia.StartsWith("NOMBRES:", StringComparison.OrdinalIgnoreCase))
                    {
                        var nombresTexto = lineaLimpia.Substring(lineaLimpia.IndexOf(':') + 1).Trim();
                        if (!string.IsNullOrEmpty(nombresTexto))
                        {
                            var nombres = ProcesarListaNombres(nombresTexto);
                            
                            if (nombres.Any())
                            {
                                resultado.NombresDetectados = nombres;
                            }
                        }
                    }
                }

                // Si no encontramos nombres específicos, usar detección manual
                if (resultado.NombresDetectados.Count == 1 && resultado.NombresDetectados[0] == "pieza automotriz")
                {
                    var nombresManual = ExtraerNombresManual(respuesta);
                    if (nombresManual.Any() && !nombresManual.Contains("pieza automotriz"))
                    {
                        resultado.NombresDetectados = nombresManual;
                    }
                }
            }
            catch
            {
                // En caso de error, usar detección manual
                resultado.NombresDetectados = ExtraerNombresManual(respuesta);
            }

            return resultado;
        }

        // NUEVO: Procesar lista de nombres de forma más inteligente
        private List<string> ProcesarListaNombres(string nombresTexto)
        {
            var nombresFinales = new List<string>();
            
            if (string.IsNullOrEmpty(nombresTexto))
                return nombresFinales;

            // Dividir por comas
            var nombresSeparados = nombresTexto.Split(',');
            
            foreach (var nombre in nombresSeparados)
            {
                var nombreLimpio = LimpiarNombre(nombre);
                
                if (!string.IsNullOrEmpty(nombreLimpio))
                {
                    // Agregar el nombre completo limpio
                    if (!nombresFinales.Contains(nombreLimpio))
                    {
                        nombresFinales.Add(nombreLimpio);
                    }
                    
                    // Si es un nombre compuesto, también agregar las partes importantes
                    if (nombreLimpio.Contains(" "))
                    {
                        var palabras = nombreLimpio.Split(' ');
                        
                        // Agregar combinaciones útiles
                        foreach (var palabra in palabras)
                        {
                            if (palabra.Length > 3 && EsPalabraImportante(palabra))
                            {
                                if (!nombresFinales.Contains(palabra))
                                {
                                    nombresFinales.Add(palabra);
                                }
                            }
                        }
                        
                        // Agregar combinaciones de 2 palabras si son relevantes
                        for (int i = 0; i < palabras.Length - 1; i++)
                        {
                            var combinacion = $"{palabras[i]} {palabras[i + 1]}";
                            if (EsCombinacionUtil(combinacion) && !nombresFinales.Contains(combinacion))
                            {
                                nombresFinales.Add(combinacion);
                            }
                        }
                    }
                }
            }
            
            return nombresFinales;
        }

        private bool EsPalabraImportante(string palabra)
        {
            // Palabras que NO son importantes por sí solas
            var palabrasExcluir = new[] { "de", "del", "la", "el", "para", "con", "sin", "por", "auto", "carro", "vehiculo" };
            
            if (palabrasExcluir.Contains(palabra.ToLower()))
                return false;
                
            // Palabras importantes en autopartes
            var palabrasImportantes = new[]
            {
                "amortiguador", "resorte", "disco", "pastilla", "filtro", "aceite",
                "bujia", "correa", "bomba", "radiador", "termostato", "manguera",
                "sensor", "valvula", "piston", "biela", "cigueñal", "volante",
                "embrague", "transmision", "diferencial", "cardán", "rotula",
                "terminal", "brazo", "barra", "estabilizadora", "suspension",
                "direccion", "cremallera", "liquido", "frenos", "helicoidal",
                "muelle", "spring", "coil", "shock", "absorber", "strut", "brake", "pad"
            };
            
            return palabrasImportantes.Contains(palabra.ToLower()) || palabra.Length > 4;
        }

        private bool EsCombinacionUtil(string combinacion)
        {
            var combinacionesUtiles = new[]
            {
                "resorte helicoidal", "muelle helicoidal", "coil spring", "shock absorber",
                "brake pad", "brake disc", "oil filter", "air filter",
                "suspension delantera", "suspension trasera", "bomba direccion",
                "liquido frenos", "pastilla freno", "disco freno"
            };
            
            return combinacionesUtiles.Contains(combinacion.ToLower());
        }

        private string LimpiarNombre(string nombre)
        {
            if (string.IsNullOrEmpty(nombre))
                return "";

            return nombre.Trim()
                        .ToLower()
                        .Replace("\"", "")
                        .Replace("'", "")
                        .Replace(".", "")
                        .Replace(";", "")
                        .Trim();
        }

        private List<string> ExtraerNombresManual(string texto)
        {
            var nombres = new List<string>();
            var palabrasComunes = new[]
            {
                "amortiguador", "resorte", "disco", "pastilla", "filtro", "aceite",
                "bujia", "correa", "bomba", "radiador", "termostato", "manguera",
                "sensor", "valvula", "piston", "biela", "cigueñal", "volante",
                "embrague", "transmision", "diferencial", "cardán", "rotula",
                "terminal", "brazo", "barra", "estabilizadora", "suspension",
                "direccion", "cremallera", "bomba direccion", "liquido frenos"
            };

            foreach (var palabra in palabrasComunes)
            {
                if (texto.ToLower().Contains(palabra.ToLower()) && !nombres.Contains(palabra))
                {
                    nombres.Add(palabra);
                }
            }

            if (nombres.Count == 0)
            {
                nombres.Add("pieza automotriz");
            }

            return nombres;
        }

        private string BuildSimpleJsonPayload(string mensaje, byte[] imageBytes, string mimeType)
        {
            var jsonBuilder = new StringBuilder();
            jsonBuilder.Append("{\"contents\":[{\"parts\":[");

            if (imageBytes != null && mimeType != null)
            {
                jsonBuilder.Append("{\"inline_data\":{\"mime_type\":\"");
                jsonBuilder.Append(mimeType);
                jsonBuilder.Append("\",\"data\":\"");
                jsonBuilder.Append(Convert.ToBase64String(imageBytes));
                jsonBuilder.Append("\"}},");
            }

            jsonBuilder.Append("{\"text\":\"");
            jsonBuilder.Append(mensaje.Replace("\"", "\\\""));
            jsonBuilder.Append("\"}]}]}");

            return jsonBuilder.ToString();
        }

        private string ExtractTextFromResponse(string responseContent)
        {
            try
            {
                // Buscar texto entre "text": y la siguiente coma/cierre
                var textStart = responseContent.IndexOf("\"text\":");
                if (textStart == -1) return "No se encontró texto en la respuesta";

                textStart = responseContent.IndexOf("\"", textStart + 7);
                if (textStart == -1) return "Error parseando respuesta";

                var textEnd = responseContent.IndexOf("\"", textStart + 1);
                if (textEnd == -1) return "Error parseando respuesta";

                return responseContent.Substring(textStart + 1, textEnd - textStart - 1);
            }
            catch
            {
                return "Error extrayendo texto de la respuesta";
            }
        }

        private List<string> ParseJsonArrayManually(string jsonArray)
        {
            var result = new List<string>();
            try
            {
                // Remover corchetes
                var content = jsonArray.Trim('[', ']');
                
                // Dividir por comas y limpiar comillas
                var items = content.Split(',');
                foreach (var item in items)
                {
                    var cleaned = item.Trim().Trim('"');
                    if (!string.IsNullOrEmpty(cleaned))
                    {
                        result.Add(cleaned);
                    }
                }
            }
            catch
            {
                result.Add("pieza automotriz");
            }

            return result.Count > 0 ? result : new List<string> { "pieza automotriz" };
        }
    }

    // NUEVA CLASE: Para respuesta estructurada
    public class RespuestaIA
    {
        public string DescripcionNatural { get; set; }
        public List<string> NombresDetectados { get; set; }
        public string Confianza { get; set; } = "Media";
        public string Categoria { get; set; } = "Autopartes";
    }

    // Clases para deserializar respuesta de Gemini (temporalmente no usadas)
    public class GeminiApiResponse
    {
        public Candidate[] candidates { get; set; }
    }

    public class Candidate
    {
        public Content content { get; set; }
    }

    public class Content
    {
        public Part[] parts { get; set; }
    }

    public class Part
    {
        public string text { get; set; }

    }

}
