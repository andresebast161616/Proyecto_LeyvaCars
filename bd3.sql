-- Crear base de datos
CREATE DATABASE LeyvaCar;
GO
-- Usar la base de datos
USE LeyvaCar;
GO
-- ========================================
-- TABLA: Usuarios (unificada para todos)
-- ========================================
CREATE TABLE Usuarios (
    Id_Usuario INT IDENTITY(1,1) PRIMARY KEY,
    Nombre NVARCHAR(100) NOT NULL,
    Apellido NVARCHAR(100) NOT NULL,
    Email NVARCHAR(255) NOT NULL UNIQUE,
    Telefono NVARCHAR(20) NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    Rol VARCHAR(20) NOT NULL CHECK (Rol IN ('Administrador', 'Vendedor', 'Cliente')), 
    FechaRegistro DATETIME2 DEFAULT GETDATE(),
    Activo BIT DEFAULT 0,
);
GO
CREATE INDEX IX_Usuarios_Email ON Usuarios(Email);
CREATE INDEX IX_Usuarios_Telefono ON Usuarios(Telefono);
GO
-- ========================================
-- TABLA: CodigoVerificacion
-- ========================================
CREATE TABLE CodigoVerificacion (
    Id_Codigo INT IDENTITY(1,1) PRIMARY KEY,
    Id_Usuario INT NOT NULL,
    Codigo NVARCHAR(6) NOT NULL,
    TipoVerificacion NVARCHAR(50) NOT NULL, -- 'registro', 'restablecer_password'
    FechaCreacion DATETIME2 DEFAULT GETDATE(),
    FechaExpiracion DATETIME2 NOT NULL,
    Usado BIT DEFAULT 0,
    FOREIGN KEY (Id_Usuario) REFERENCES Usuarios(Id_Usuario)
        ON DELETE CASCADE
);
GO
-- ========================================
-- TABLA: Productos
-- ========================================
CREATE TABLE Productos (
    Id_Producto INT IDENTITY(1,1) PRIMARY KEY,
    Codigo NVARCHAR(50) UNIQUE NOT NULL,
    Nombre NVARCHAR(255) NOT NULL,
    Descripcion NVARCHAR(MAX),
    Categoria NVARCHAR(100),
    Precio DECIMAL(10,2) NOT NULL,
    Stock INT DEFAULT 0,
    Marca NVARCHAR(100),
    Modelo VARCHAR(50),
    ModelosCompatibles NVARCHAR(MAX), -- JSON con modelos compatibles
    FechaCreacion DATETIME2 DEFAULT GETDATE(),
    Activo BIT DEFAULT 1,
    CONSTRAINT CHK_Precio_Positivo CHECK (Precio >= 0),
    CONSTRAINT CHK_Stock_NoNegativo CHECK (Stock >= 0)
);
GO
-- Índices para búsquedas rápidas
CREATE INDEX IX_Productos_Nombre ON Productos(Nombre);
CREATE INDEX IX_Productos_Categoria ON Productos(Categoria);
CREATE INDEX IX_Productos_Codigo ON Productos(Codigo);
GO
-- ========================================
-- TABLA: Producto_Imagenes
-- ========================================
CREATE TABLE Producto_Imagenes (
    Id_Imagen INT IDENTITY(1,1) PRIMARY KEY,
    Id_Producto INT NOT NULL,
    Imagen NVARCHAR(500) NOT NULL, -- URL o ruta de la imagen
    Orden INT NOT NULL DEFAULT 0, -- Orden de visualización
    Alt_Text NVARCHAR(255), -- Texto alternativo para SEO/accesibilidad
    FechaCreacion DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (Id_Producto) REFERENCES Productos(Id_Producto)
        ON DELETE CASCADE,
    CONSTRAINT UQ_Producto_Imagen_Orden UNIQUE (Id_Producto, Orden)
);
GO
CREATE INDEX IX_Producto_Imagenes_Producto ON Producto_Imagenes(Id_Producto);
CREATE INDEX IX_Producto_Imagenes_Orden ON Producto_Imagenes(Id_Producto, Orden);
GO
-- ========================================
-- TABLA: Carrito
-- ========================================
CREATE TABLE Carrito (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Usuario_Id INT NOT NULL,
    Producto_Id INT NOT NULL,
    Cantidad INT NOT NULL DEFAULT 1,
    Fecha_Creacion DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (Usuario_Id) REFERENCES Usuarios(Id_Usuario),
    FOREIGN KEY (Producto_Id) REFERENCES Productos(Id_Producto)
        ON DELETE CASCADE,
    CONSTRAINT CHK_Carrito_Cantidad_Positiva CHECK (Cantidad > 0)
);
GO
CREATE INDEX IX_Carrito_Usuario ON Carrito(Usuario_Id);
CREATE INDEX IX_Carrito_Producto ON Carrito(Producto_Id);
GO
-- ========================================
-- TABLA: Consultas
-- ========================================
CREATE TABLE Consultas (
    Id_Consulta INT IDENTITY(1,1) PRIMARY KEY,
    Id_Usuario INT NOT NULL, -- Referencia al usuario/cliente
    MarcaVehiculo NVARCHAR(100),
    ModeloVehiculo NVARCHAR(100),
    AnioVehiculo INT,
    RutaImagen NVARCHAR(500),
    NombresDetectadosIA NVARCHAR(MAX), -- JSON con nombres detectados
    Id_Producto INT NULL,
    TipoConsulta NVARCHAR(50), -- 'EncontradoEnBD', 'BusquedaManual', 'PedidoEspecial'
    MensajeWhatsApp NVARCHAR(MAX),
    Estado NVARCHAR(50) DEFAULT 'Pendiente', -- 'Pendiente', 'Respondido', 'Convertido', 'Cancelado'
    RespuestaVendedor NVARCHAR(MAX) NULL,
    Id_Vendedor INT NULL, -- Usuario vendedor que respondió
    FechaConsulta DATETIME2 DEFAULT GETDATE(),
    FechaRespuesta DATETIME2 NULL,
    FOREIGN KEY (Id_Usuario) REFERENCES Usuarios(Id_Usuario),
    FOREIGN KEY (Id_Producto) REFERENCES Productos(Id_Producto),
    FOREIGN KEY (Id_Vendedor) REFERENCES Usuarios(Id_Usuario),
    CONSTRAINT CHK_AnioVehiculo CHECK (AnioVehiculo IS NULL OR (AnioVehiculo >= 1900 AND AnioVehiculo <= YEAR(GETDATE()) + 1))
);
GO
CREATE INDEX IX_Consultas_Estado ON Consultas(Estado);
CREATE INDEX IX_Consultas_Fecha ON Consultas(FechaConsulta);
CREATE INDEX IX_Consultas_Usuario ON Consultas(Id_Usuario);
GO
-- ========================================
-- TABLA: Pedidos
-- ========================================
CREATE TABLE Pedidos (
    Id_Pedido INT IDENTITY(1,1) PRIMARY KEY,
    Id_Consulta INT NOT NULL,
    Id_Usuario INT NOT NULL, -- El cliente que realiza el pedido
    Id_Producto INT NOT NULL,
    Cantidad INT DEFAULT 1,
    PrecioAcordado DECIMAL(10,2) NOT NULL,
    Estado NVARCHAR(50) DEFAULT 'Pendiente', -- 'Pendiente', 'Confirmado', 'Enviado', 'Entregado', 'Cancelado'
    Notas NVARCHAR(MAX),
    FechaCreacion DATETIME2 DEFAULT GETDATE(),
    FechaActualizacion DATETIME2 NULL,
    FOREIGN KEY (Id_Consulta) REFERENCES Consultas(Id_Consulta),
    FOREIGN KEY (Id_Usuario) REFERENCES Usuarios(Id_Usuario),
    FOREIGN KEY (Id_Producto) REFERENCES Productos(Id_Producto),
    CONSTRAINT CHK_Cantidad_Positiva CHECK (Cantidad > 0),
    CONSTRAINT CHK_Precio_Acordado_Positivo CHECK (PrecioAcordado >= 0)
);
GO
CREATE INDEX IX_Pedidos_Estado ON Pedidos(Estado);
CREATE INDEX IX_Pedidos_Fecha ON Pedidos(FechaCreacion);
CREATE INDEX IX_Pedidos_Usuario ON Pedidos(Id_Usuario);
GO
-- ========================================
-- DATOS DE EJEMPLO
-- ========================================
-- Insertar usuarios de ejemplo
INSERT INTO Usuarios (Nombre, Apellido, Email, Telefono, PasswordHash, Rol, Activo)
VALUES 
-- Administrador
('Admin', 'Sistema', 'admin@leyvacar.com', '999000111', 'hash_admin', 'Administrador', 1),
-- Vendedor
('Carlos', 'Vendedor', 'carlos@leyvacar.com', '999000222', 'hash_vendedor', 'Vendedor', 1),
-- Clientes
('Juan', 'Pérez', 'juan.perez@email.com', '987654321', 'hash_juan', 'Cliente', 1),
('María', 'López', 'maria.lopez@email.com', '912345678', 'hash_maria', 'Cliente', 1),
('Pedro', 'Ruiz', 'pedro.ruiz@email.com', '923456789', 'hash_pedro', 'Cliente', 0);
GO
-- Insertar productos
INSERT INTO Productos (Codigo, Nombre, Descripcion, Categoria, Precio, Stock, Marca, ModelosCompatibles)
VALUES 
('AMT-001', 'Amortiguador Delantero Monroe', 'Amortiguador delantero gas premium', 'Suspensión', 280.00, 5, 'Monroe',
 N'["Ford Focus 2018-2023", "Ford Escape 2020-2023"]'),
 
('ESP-001', 'Resorte Helicoidal Delantero', 'Espiral de suspensión delantera', 'Suspensión', 150.00, 8, 'Original',
 N'["Ford Focus 2018-2023"]'),
 
('FRN-001', 'Disco de Freno Delantero', 'Disco ventilado 280mm', 'Frenos', 180.00, 10, 'Brembo',
 N'["Toyota Corolla 2019-2024"]'),
 
('PAS-001', 'Pastillas de Freno Delanteras', 'Juego de pastillas cerámicas', 'Frenos', 120.00, 15, 'Brembo',
 N'["Toyota Corolla 2019-2024"]'),
 
('SPK-TOY-001', 'Bujía NGK Iridium IX BKR6EIX', 'Bujía de iridio de larga duración para motor gasolina', 'Encendido', 55.00, 30, 'NGK',
 N'["Toyota Yaris 2016-2023", "Toyota Vitz 2018-2022"]'),
('FLT-TOY-002', 'Filtro de Aire Denso 143-3053', 'Filtro de aire con marco sellado para alto flujo', 'Mantenimiento', 60.00, 18, 'Denso',
 N'["Toyota Corolla 2019-2024"]'),
('AMO-TOY-003', 'Amortiguador Trasero Monroe Reflex', 'Amortiguador trasero gas-óleo premium', 'Suspensión', 290.00, 10, 'Monroe',
 N'["Toyota Hilux 2016-2024"]'),
('BRA-TOY-004', 'Pastillas de Freno Brembo Ceramic', 'Juego de pastillas cerámicas de alto rendimiento', 'Frenos', 130.00, 22, 'Brembo',
 N'["Toyota Corolla 2019-2024"]'),
('BAT-NIS-001', 'Batería Yuasa 12V 50Ah Serie 5000', 'Batería de arranque serie 5000, alta fiabilidad', 'Eléctrico', 320.00, 10, 'Yuasa',
 N'["Nissan Versa 2015-2022"]'),
('STR-NIS-002', 'Arranque Denso (Starter)', 'Motor de arranque compatible OEM DENSO', 'Eléctrico', 450.00, 6, 'Denso',
 N'["Nissan Sentra 2015-2022"]'),
('FUE-NIS-003', 'Bomba de Combustible Delphi FG1063', 'Bomba eléctrica sumergible de alta presión', 'Sistema de Combustible', 320.00, 6, 'Delphi',
 N'["Nissan Frontier 2014-2021"]'),
('AMB-KIA-001', 'Sensor de Oxígeno Bosch Banda Ancha', 'Sensor Lambda banda ancha para control de mezcla y emisiones', 'Emisiones', 95.00, 14, 'Bosch',
 N'["Kia Sportage 2019-2024", "Kia Sorento 2020-2024"]'),
('LMP-KIA-002', 'Bombilla H4 Philips VisionPlus', 'Bombilla halógena H4 12V 60/55W con +60 % más luz', 'Iluminación', 65.00, 25, 'Philips',
 N'["Kia Rio 2018-2023", "Kia Picanto 2017-2022"]'),
('BTR-KIA-003', 'Batería Etna Free 60D23L', 'Batería libre de mantenimiento 12V 60Ah', 'Eléctrico', 480.00, 10, 'Etna',
 N'["Kia Picanto 2017-2022"]'),
('COR-KIA-004', 'Correa de Transmisión Synchronized Gates', 'Correa síncrona de distribución reforzada', 'Transmisión', 210.00, 8, 'Gates',
 N'["Kia Sportage 2017-2021"]'),
('RAD-TOY-005', 'Radiador de Agua TYC 2451', 'Radiador de aluminio con tanque plástico reforzado', 'Refrigeración', 480.00, 5, 'TYC',
 N'["Toyota Yaris 2019-2023"]'),
('BEL-NIS-006', 'Correa Poli-V Gates Serpentina', 'Correa multi-ranura para accesorios (alternador, bomba, A/C)', 'Transmisión accesorios', 75.00, 18, 'Gates',
 N'["Nissan Versa 2015-2022"]'),
('EXH-KIA-007', 'Silenciador Intermedio Walker 21345', 'Silenciador de acero aluminizado, reduce ruido de escape', 'Escape', 380.00, 4, 'Walker',
 N'["Kia Sorento 2016-2020"]'),
('FRN-TOY-008', 'Disco de Freno Trasero Brembo 300mm', 'Disco de freno ventilado 300 mm, aplicación premium', 'Frenos', 220.00, 9, 'Brembo',
 N'["Toyota Hilux 2016-2024"]'),
('STR-TOY-009', 'Strut Delantero KYB Excel-G', 'Amortiguador Frente gas-aceite KYB alta calidad', 'Suspensión', 265.00, 12, 'KYB',
 N'["Toyota Corolla 2019-2024"]'),
('FLR-NIS-010', 'Filtro de Cabina Mann-Filter CUK 2730', 'Filtro de habitáculo premium, reduce polvo y alérgenos', 'Mantenimiento', 55.00, 16, 'Mann-Filter',
 N'["Nissan Sentra 2015-2022"]');
GO
-- Insertar imágenes de productos de ejemplo
INSERT INTO Producto_Imagenes (Id_Producto, Imagen, Orden, Alt_Text)
VALUES 
-- Amortiguador Monroe (Id_Producto = 1)
(1, 'https://i.ibb.co/sample1/amortiguador-monroe-principal.jpg', 1, 'Amortiguador Delantero Monroe vista frontal'),
(1, 'https://i.ibb.co/sample1/amortiguador-monroe-detalle.jpg', 2, 'Detalle del pistón del amortiguador'),
(1, 'https://i.ibb.co/sample1/amortiguador-monroe-instalado.jpg', 3, 'Amortiguador Monroe instalado'),
-- Resorte Helicoidal (Id_Producto = 2)
(2, 'https://i.ibb.co/sample2/resorte-helicoidal-principal.jpg', 1, 'Resorte helicoidal delantero'),
(2, 'https://i.ibb.co/sample2/resorte-helicoidal-medidas.jpg', 2, 'Especificaciones y medidas del resorte'),
-- Disco de Freno Brembo (Id_Producto = 3)
(3, 'https://i.ibb.co/sample3/disco-freno-brembo.jpg', 1, 'Disco de freno ventilado Brembo 280mm'),
(3, 'https://i.ibb.co/sample3/disco-freno-detalle.jpg', 2, 'Detalle de ventilación del disco'),
-- Pastillas de Freno (Id_Producto = 4)
(4, 'https://i.ibb.co/sample4/pastillas-brembo-set.jpg', 1, 'Set completo de pastillas cerámicas Brembo'),
(4, 'https://i.ibb.co/sample4/pastillas-brembo-detalle.jpg', 2, 'Detalle de material cerámico'),
-- Bujías NGK (Id_Producto = 5)
(5, 'https://i.ibb.co/sample5/bujia-ngk-iridium.jpg', 1, 'Bujía NGK Iridium IX'),
(5, 'https://i.ibb.co/sample5/bujia-ngk-punta.jpg', 2, 'Detalle punta de iridio');
GO