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
GO
