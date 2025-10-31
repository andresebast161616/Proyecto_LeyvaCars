-- Crear base de datos
CREATE DATABASE LeyvaCar;
GO

-- Usar la base de datos
USE LeyvaCar;
GO

-- ========================================
-- TABLA: Usuarios
-- ========================================
CREATE TABLE Usuarios (
    Id_Usuario INT IDENTITY(1,1) PRIMARY KEY, -- Identificador único
    Nombre NVARCHAR(100) NOT NULL,
    Apellido NVARCHAR(100) NOT NULL,
    Email NVARCHAR(255) NOT NULL UNIQUE, -- Correo único
    PasswordHash NVARCHAR(255) NOT NULL, -- Hash de la contraseña
    FechaRegistro DATETIME2 DEFAULT GETDATE(),
    Activo BIT DEFAULT 0 -- Indicador de verificación
);
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
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Codigo NVARCHAR(50) UNIQUE NOT NULL,
    Nombre NVARCHAR(255) NOT NULL,
    Descripcion NVARCHAR(MAX),
    Categoria NVARCHAR(100),
    Precio DECIMAL(10,2),
    Stock INT DEFAULT 0,
    UrlImagen NVARCHAR(500),
    Marca NVARCHAR(100),
    ModelosCompatibles NVARCHAR(MAX), -- JSON con modelos compatibles
    FechaCreacion DATETIME2 DEFAULT GETDATE(),
    Activo BIT DEFAULT 1
);
GO

-- Índices para búsquedas rápidas
CREATE INDEX IX_Productos_Nombre ON Productos(Nombre);
CREATE INDEX IX_Productos_Categoria ON Productos(Categoria);
GO

-- ========================================
-- TABLA: Consultas
-- ========================================
CREATE TABLE Consultas (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    NombreCliente NVARCHAR(255),
    TelefonoCliente NVARCHAR(20),
    MarcaVehiculo NVARCHAR(100),
    ModeloVehiculo NVARCHAR(100),
    AnioVehiculo INT,
    RutaImagen NVARCHAR(500),
    NombresDetectadosIA NVARCHAR(MAX), -- JSON con nombres detectados
    ProductoId INT NULL,
    TipoConsulta NVARCHAR(50), -- 'EncontradoEnBD', 'BusquedaManual', 'PedidoEspecial'
    MensajeWhatsApp NVARCHAR(MAX),
    Estado NVARCHAR(50) DEFAULT 'Pendiente', -- 'Pendiente', 'Respondido', 'Convertido', 'Cancelado'
    RespuestaVendedor NVARCHAR(MAX) NULL,
    FechaConsulta DATETIME2 DEFAULT GETDATE(),
    FechaRespuesta DATETIME2 NULL,
    FOREIGN KEY (ProductoId) REFERENCES Productos(Id)
);
GO

-- ========================================
-- TABLA: Pedidos
-- ========================================
CREATE TABLE Pedidos (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ConsultaId INT,
    NombreCliente NVARCHAR(255),
    TelefonoCliente NVARCHAR(20),
    ProductoId INT,
    Cantidad INT DEFAULT 1,
    PrecioAcordado DECIMAL(10,2),
    Estado NVARCHAR(50) DEFAULT 'Pendiente',
    Notas NVARCHAR(MAX),
    FechaCreacion DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (ConsultaId) REFERENCES Consultas(Id),
    FOREIGN KEY (ProductoId) REFERENCES Productos(Id)
);
GO

-- ========================================
-- DATOS DE EJEMPLO
-- ========================================
INSERT INTO Productos (Codigo, Nombre, Descripcion, Categoria, Precio, Stock, Marca, ModelosCompatibles)
VALUES 
('AMT-001', 'Amortiguador Delantero Monroe', 'Amortiguador delantero gas premium', 'Suspensión', 280.00, 5, 'Monroe',
 N'["Ford Focus 2018-2023", "Ford Escape 2020-2023"]'),
('ESP-001', 'Resorte Helicoidal Delantero', 'Espiral de suspensión delantera', 'Suspensión', 150.00, 8, 'Original',
 N'["Ford Focus 2018-2023"]'),
('FRN-001', 'Disco de Freno Delantero', 'Disco ventilado 280mm', 'Frenos', 180.00, 10, 'Brembo',
 N'["Toyota Corolla 2019-2024"]'),
('PAS-001', 'Pastillas de Freno Delanteras', 'Juego de pastillas cerámicas', 'Frenos', 120.00, 15, 'Brembo',
 N'["Toyota Corolla 2019-2024"]');
 ('SPK-TOY-001', 'Bujía NGK Iridium IX BKR6EIX', 'Bujía de iridio de larga duración para motor gasolina', 'Encendido', 55.00, 30,
   NULL, 'NGK',
   N'["Toyota Yaris 2016-2023", "Toyota Vitz 2018-2022"]'),

('FLT-TOY-002', 'Filtro de Aire Denso 143-3053', 'Filtro de aire con marco sellado para alto flujo', 'Mantenimiento', 60.00, 18,
   NULL, 'Denso',
   N'["Toyota Corolla 2019-2024"]'),

('AMO-TOY-003', 'Amortiguador Trasero Monroe Reflex', 'Amortiguador trasero gas-óleo premium', 'Suspensión', 290.00, 10,
   NULL, 'Monroe',
   N'["Toyota Hilux 2016-2024"]'),

('BRA-TOY-004', 'Pastillas de Freno Brembo Ceramic', 'Juego de pastillas cerámicas de alto rendimiento', 'Frenos', 130.00, 22,
   NULL, 'Brembo',
   N'["Toyota Corolla 2019-2024"]'),

('BAT-NIS-001', 'Batería Yuasa 12V 50Ah Serie 5000', 'Batería de arranque serie 5000, alta fiabilidad', 'Eléctrico', 320.00, 10,
   NULL, 'Yuasa',
   N'["Nissan Versa 2015-2022"]'),

('STR-NIS-002', 'Arranque Denso (Starter)', 'Motor de arranque compatible OEM DENSO', 'Eléctrico', 450.00, 6,
   NULL, 'Denso',
   N'["Nissan Sentra 2015-2022"]'),

('FUE-NIS-003', 'Bomba de Combustible Delphi FG1063', 'Bomba eléctrica sumergible de alta presión', 'Sistema de Combustible', 320.00, 6,
   NULL, 'Delphi',
   N'["Nissan Frontier 2014-2021"]'),

('AMB-KIA-001', 'Sensor de Oxígeno Bosch Banda Ancha', 'Sensor Lambda banda ancha para control de mezcla y emisiones', 'Emisiones', 95.00, 14,
   NULL, 'Bosch',
   N'["Kia Sportage 2019-2024", "Kia Sorento 2020-2024"]'),

('LMP-KIA-002', 'Bombilla H4 Philips VisionPlus', 'Bombilla halógena H4 12V 60/55W con +60 % más luz', 'Iluminación', 65.00, 25,
   NULL, 'Philips',
   N'["Kia Rio 2018-2023", "Kia Picanto 2017-2022"]'),

('BTR-KIA-003', 'Batería Etna Free 60D23L', 'Batería libre de mantenimiento 12V 60Ah', 'Eléctrico', 480.00, 10,
   NULL, 'Etna',
   N'["Kia Picanto 2017-2022"]'),

('COR-KIA-004', 'Correa de Transmisión Synchronized Gates', 'Correa síncrona de distribución reforzada', 'Transmisión', 210.00, 8,
   NULL, 'Gates',
   N'["Kia Sportage 2017-2021"]'),

('RAD-TOY-005', 'Radiador de Agua TYC 2451', 'Radiador de aluminio con tanque plástico reforzado', 'Refrigeración', 480.00, 5,
   NULL, 'TYC',
   N'["Toyota Yaris 2019-2023"]'),

('BEL-NIS-006', 'Correa Poli-V Gates Serpentina', 'Correa multi-ranura para accesorios (alternador, bomba, A/C)', 'Transmisión accesorios', 75.00, 18,
   NULL, 'Gates',
   N'["Nissan Versa 2015-2022"]'),

('EXH-KIA-007', 'Silenciador Intermedio Walker 21345', 'Silenciador de acero aluminizado, reduce ruido de escape', 'Escape', 380.00, 4,
   NULL, 'Walker',
   N'["Kia Sorento 2016-2020"]'),

('FRN-TOY-008', 'Disco de Freno Trasero Brembo 300mm', 'Disco de freno ventilado 300 mm, aplicación premium', 'Frenos', 220.00, 9,
   NULL, 'Brembo',
   N'["Toyota Hilux 2016-2024"]'),

('STR-TOY-009', 'Strut Delantero KYB Excel-G', 'Amortiguador Frente gas-aceite KYB alta calidad', 'Suspensión', 265.00, 12,
   NULL, 'KYB',
   N'["Toyota Corolla 2019-2024"]'),

('FLR-NIS-010', 'Filtro de Cabina Mann-Filter CUK 2730', 'Filtro de habitáculo premium, reduce polvo y alérgenos', 'Mantenimiento', 55.00, 16,
   NULL, 'Mann-Filter',
   N'["Nissan Sentra 2015-2022"]');
GO

