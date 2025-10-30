let currentConsultaId = null;

document.addEventListener('DOMContentLoaded', function () {
    initializeComponents();
});

function initializeComponents() {
    // Configurar zona de arrastre
    const dropZone = document.getElementById('dropZone');
    const imageInput = document.getElementById('imageInput');
    const imagePreview = document.getElementById('imagePreview');
    const previewImg = document.getElementById('previewImg');

    // Eventos de arrastre
    dropZone.addEventListener('click', () => imageInput.click());

    dropZone.addEventListener('dragover', (e) => {
        e.preventDefault();
        dropZone.classList.add('border-primary', 'bg-light');
    });

    dropZone.addEventListener('dragleave', () => {
        dropZone.classList.remove('border-primary', 'bg-light');
    });

    dropZone.addEventListener('drop', (e) => {
        e.preventDefault();
        dropZone.classList.remove('border-primary', 'bg-light');
        const files = e.dataTransfer.files;
        if (files.length > 0) {
            handleImageSelect(files[0]);
        }
    });

    imageInput.addEventListener('change', (e) => {
        if (e.target.files.length > 0) {
            handleImageSelect(e.target.files[0]);
        }
    });

    document.getElementById('removeImage').addEventListener('click', () => {
        imageInput.value = '';
        imagePreview.classList.add('d-none');
        document.getElementById('dropContent').classList.remove('d-none');
    });

    // Botón analizar
    document.getElementById('analizarBtn').addEventListener('click', analizarImagen);
}

function handleImageSelect(file) {
    if (file && file.type.startsWith('image/')) {
        const reader = new FileReader();
        reader.onload = (e) => {
            document.getElementById('previewImg').src = e.target.result;
            document.getElementById('imagePreview').classList.remove('d-none');
            document.getElementById('dropContent').classList.add('d-none');
        };
        reader.readAsDataURL(file);
    } else {
        mostrarError('Por favor selecciona un archivo de imagen válido');
    }
}

async function analizarImagen() {
    const marca = document.getElementById('marcaVehiculo').value.trim();
    const modelo = document.getElementById('modeloVehiculo').value.trim();
    const anio = document.getElementById('anioVehiculo').value.trim();
    const archivo = document.getElementById('imageInput').files[0];

    // Validaciones
    if (!archivo) {
        mostrarError('Por favor selecciona una imagen');
        return;
    }

    if (!marca || !modelo || !anio) {
        mostrarError('Por favor completa todos los datos del vehículo');
        return;
    }

    // Mostrar estado de carga
    mostrarCargando(true);

    // Agregar mensaje del usuario al chat
    agregarMensajeUsuario(archivo, marca, modelo, anio);

    const formData = new FormData();
    formData.append('image', archivo);
    formData.append('marcaVehiculo', marca);
    formData.append('modeloVehiculo', modelo);
    formData.append('anioVehiculo', anio);
    formData.append('mensaje', 'Identificar pieza automotriz');

    try {
        const response = await fetch('/Chat/ProcesarImagenYBuscar', {
            method: 'POST',
            body: formData
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const result = await response.json();

        // DEBUG Console
        console.log('=== DEBUG JAVASCRIPT ===');
        console.log('Respuesta completa:', result);
        console.log('Descripción IA:', result.descripcionIA);
        console.log('Nombres detectados:', result.nombresDetectados);
        console.log('RESPUESTA RAW DE IA:', result.respuestaRaw);
        console.log('Productos encontrados:', result.productos?.length || 0);
        console.log('========================');

        if (result.success) {
            currentConsultaId = result.consultaId;

            // Mostrar respuesta de la IA en el chat
            agregarMensajeIA(result.descripcionIA, result.nombresDetectados);

            // Mostrar productos en el panel derecho
            mostrarProductos(result.productos, result.tieneResultados);
        } else {
            mostrarError(result.error || 'Error desconocido en el servidor');
        }
    } catch (error) {
        console.error('Error:', error);
        mostrarError('Error de conexión: ' + error.message);
    } finally {
        mostrarCargando(false);
    }
}

function mostrarCargando(mostrar) {
    const chatLoading = document.getElementById('chatLoading');
    const statusIndicator = document.getElementById('statusIndicator');
    const analizarBtn = document.getElementById('analizarBtn');

    if (mostrar) {
        chatLoading.classList.remove('d-none');
        statusIndicator.textContent = 'Analizando...';
        statusIndicator.className = 'badge bg-warning';
        analizarBtn.disabled = true;
        analizarBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Analizando...';
    } else {
        chatLoading.classList.add('d-none');
        statusIndicator.textContent = 'Listo';
        statusIndicator.className = 'badge bg-success';
        analizarBtn.disabled = false;
        analizarBtn.innerHTML = '🔍 Analizar con IA';
    }
}

function agregarMensajeUsuario(archivo, marca, modelo, anio) {
    const chatMessages = document.getElementById('chatMessages');

    const mensajeHtml = `
        <div class="chat-message user mb-3">
            <div class="mensaje-usuario">
                <div class="mb-2">
                    <img src="${URL.createObjectURL(archivo)}" class="imagen-chat img-thumbnail">
                </div>
                <div><strong>Vehículo:</strong> ${marca} ${modelo} ${anio}</div>
                <div class="small mt-1">Solicito identificación de esta pieza</div>
            </div>
        </div>
    `;

    // Si hay contenido inicial, reemplazarlo
    if (chatMessages.innerHTML.includes('Completa los datos')) {
        chatMessages.innerHTML = '';
    }

    chatMessages.innerHTML += mensajeHtml;
    chatMessages.scrollTop = chatMessages.scrollHeight;
}

function agregarMensajeIA(descripcion, nombres) {
    const chatMessages = document.getElementById('chatMessages');

    const mensajeHtml = `
        <div class="chat-message ai mb-3">
            <div class="mensaje-ia">
                <div class="fw-bold text-primary mb-2">
                    🤖 Análisis de IA
                </div>
                <div class="mb-3">${descripcion}</div>
                <div class="small">
                    <strong>Nombres detectados:</strong> 
                    <div class="mt-1">
                        ${nombres.map(nombre => `<span class="badge bg-light text-dark me-1">${nombre}</span>`).join('')}
                    </div>
                </div>
            </div>
        </div>
    `;

    chatMessages.innerHTML += mensajeHtml;
    chatMessages.scrollTop = chatMessages.scrollHeight;
}

function mostrarProductos(productos, tieneResultados) {
    const container = document.getElementById('productosContainer');

    if (!tieneResultados || productos.length === 0) {
        container.innerHTML = `
            <div class="text-center py-4 px-3">
                <div class="display-1 text-warning mb-3">🔍</div>
                <h6 class="fw-bold">Sin Coincidencias</h6>
                <p class="small text-muted mb-3">No encontramos productos similares</p>
                <button onclick="consultarPedidoEspecial()" class="btn btn-warning btn-sm w-100">
                    📞 Pedido Especial
                </button>
            </div>
        `;
        return;
    }

    let html = '';
    productos.forEach((producto, index) => {
        html += `
            <div class="border-bottom p-3 producto-card">
                <div class="row g-2 align-items-center">
                    <div class="col-4">
                        <div class="ratio ratio-1x1 bg-light rounded">
                            ${producto.imagen ?
                `<img src="${producto.imagen}" class="rounded object-fit-cover">` :
                `<div class="d-flex align-items-center justify-content-center">
                                    <span class="text-muted">📷</span>
                                </div>`
            }
                        </div>
                    </div>
                    <div class="col-8">
                        <h6 class="fw-bold mb-1 small">${producto.nombre}</h6>
                        <div class="small text-muted mb-1">
                            <div>Cód: ${producto.codigo}</div>
                            <div>Marca: ${producto.marca}</div>
                        </div>
                        <div class="fw-bold text-success">S/ ${parseFloat(producto.precio).toFixed(2)}</div>
                        <div class="small ${producto.stock > 0 ? 'text-success' : 'text-danger'}">
                            Stock: ${producto.stock}
                        </div>
                    </div>
                </div>
                <div class="mt-2 d-grid gap-1">
                    <button onclick="consultarWhatsApp(${producto.id})" 
                            class="btn btn-success btn-sm">
                        💬 WhatsApp
                    </button>
                </div>
            </div>
        `;
    });

    container.innerHTML = html;
}

function mostrarError(mensaje) {
    const chatMessages = document.getElementById('chatMessages');

    const errorHtml = `
        <div class="chat-message ai mb-3">
            <div class="alert alert-danger mb-0">
                ⚠️ <strong>Error:</strong> ${mensaje}
            </div>
        </div>
    `;

    chatMessages.innerHTML += errorHtml;
    chatMessages.scrollTop = chatMessages.scrollHeight;
}

// Funciones de WhatsApp
async function consultarWhatsApp(productoId) {
    if (!currentConsultaId) {
        mostrarError('No hay consulta activa');
        return;
    }

    try {
        const response = await fetch('/Chat/GenerarConsultaWhatsApp', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
            },
            body: `productoId=${productoId}&consultaId=${currentConsultaId}`
        });

        const result = await response.json();

        if (result.success) {
            window.open(result.urlWhatsApp, '_blank');
        } else {
            mostrarError(result.error);
        }
    } catch (error) {
        mostrarError('Error de conexión: ' + error.message);
    }
}

async function consultarPedidoEspecial() {
    if (!currentConsultaId) {
        mostrarError('No hay consulta activa');
        return;
    }

    try {
        const response = await fetch('/Chat/ConsultarPedidoEspecial', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
            },
            body: `consultaId=${currentConsultaId}`
        });

        const result = await response.json();

        if (result.success) {
            window.open(result.urlWhatsApp, '_blank');
        } else {
            mostrarError(result.error);
        }
    } catch (error) {
        mostrarError('Error de conexión: ' + error.message);
    }
}