USE master;
GO

IF EXISTS (SELECT name FROM sys.databases WHERE name = N'TourismDB')
BEGIN
    ALTER DATABASE TourismDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE TourismDB;
END
GO

CREATE DATABASE TourismDB;
GO

USE TourismDB;
GO

/* =========================
   1. USERS
========================= */
CREATE TABLE Users (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(100) NOT NULL,
    Password NVARCHAR(200) NOT NULL,
    Role NVARCHAR(50) NOT NULL,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

/* =========================
   2. LANGUAGES
========================= */
CREATE TABLE Languages (
    LanguageCode NVARCHAR(10) PRIMARY KEY,
    LanguageName NVARCHAR(100) NOT NULL
);

/* =========================
   3. POIs
========================= */
CREATE TABLE POIs (
    PoiId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    Latitude FLOAT NOT NULL,
    Longitude FLOAT NOT NULL,
    Description NVARCHAR(500) NULL,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

/* =========================
   4. POI IMAGES
========================= */
CREATE TABLE PoiImages (
    ImageId INT IDENTITY(1,1) PRIMARY KEY,
    PoiId INT NOT NULL,
    ImageUrl NVARCHAR(500) NOT NULL,
    DisplayOrder INT DEFAULT 0,
    FOREIGN KEY (PoiId) REFERENCES POIs(PoiId)
        ON DELETE CASCADE
);

/* =========================
   5. RESTAURANTS
========================= */
CREATE TABLE Restaurants (
    RestaurantId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(200),
    Address NVARCHAR(300),
    Description NVARCHAR(500),
    PoiId INT NOT NULL,
    FOREIGN KEY (PoiId) REFERENCES POIs(PoiId)
        ON DELETE CASCADE
);

/* =========================
   6. FOODS
========================= */
CREATE TABLE Foods (
    FoodId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(200),
    Price DECIMAL(10,2),
    Description NVARCHAR(500),
    RestaurantId INT NOT NULL,
    FOREIGN KEY (RestaurantId) REFERENCES Restaurants(RestaurantId)
        ON DELETE CASCADE
);

/* =========================
   7. NARRATIONS (TTS ONLY)
========================= */
CREATE TABLE Narrations (
    NarrationId INT IDENTITY(1,1) PRIMARY KEY,
    PoiId INT NOT NULL,
    LanguageCode NVARCHAR(10) NOT NULL,
    Text NVARCHAR(MAX) NOT NULL,

    -- Thông tin cấu hình TTS (optional)
    VoiceName NVARCHAR(100) NULL,
    SpeechRate FLOAT DEFAULT 0.5,
    Volume FLOAT DEFAULT 1.0,

    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),

    FOREIGN KEY (PoiId) REFERENCES POIs(PoiId)
        ON DELETE CASCADE,
    FOREIGN KEY (LanguageCode) REFERENCES Languages(LanguageCode),

    CONSTRAINT UQ_Poi_Language UNIQUE (PoiId, LanguageCode)
);

/* =========================
   8. GEOFENCES
========================= */
CREATE TABLE Geofences (
    GeofenceId INT IDENTITY(1,1) PRIMARY KEY,
    PoiId INT NOT NULL,
    Radius FLOAT NOT NULL,
    FOREIGN KEY (PoiId) REFERENCES POIs(PoiId)
        ON DELETE CASCADE
);

/* =========================
   9. QRCODES
========================= */
CREATE TABLE QRCodes (
    QRCodeId INT IDENTITY(1,1) PRIMARY KEY,
    PoiId INT NOT NULL,
    CodeValue NVARCHAR(200) NOT NULL,
    FOREIGN KEY (PoiId) REFERENCES POIs(PoiId)
        ON DELETE CASCADE
);

/* =========================
   INSERT LANGUAGES
========================= */
INSERT INTO Languages (LanguageCode, LanguageName) VALUES
('vi','Vietnamese'),
('en','English'),
('fr','French'),
('de','German'),
('ja','Japanese'),
('ko','Korean'),
('zh','Chinese');


/* =========================
   INSERT POIs
========================= */
INSERT INTO POIs (Name, Latitude, Longitude, Description) VALUES
(N'Ốc Đào - Vĩnh Khánh', 10.757774, 106.701834, N'Quán ốc nổi tiếng lâu năm tại Quận 4'),
(N'Ốc Oanh - Vĩnh Khánh', 10.757920, 106.701920, N'Quán ốc xào me và ốc len xào dừa nổi tiếng'),
(N'Hải Sản Vĩnh Khánh 1', 10.758050, 106.702100, N'Hải sản tươi sống chọn tại bể'),
(N'Quán Ốc Tô', 10.758200, 106.702250, N'Quán bình dân, phục vụ nhanh'),
(N'Hải Sản Làng Chài', 10.758350, 106.702400, N'Hải sản cao cấp và không gian rộng');


/* =========================
   INSERT POI IMAGES
========================= */
INSERT INTO PoiImages (PoiId, ImageUrl, DisplayOrder)
SELECT PoiId, N'https://i.imgur.com/a1.jpg', 1 FROM POIs WHERE Name=N'Ốc Đào - Vĩnh Khánh'
UNION ALL
SELECT PoiId, N'https://i.imgur.com/b1.jpg', 1 FROM POIs WHERE Name=N'Ốc Oanh - Vĩnh Khánh'
UNION ALL
SELECT PoiId, N'https://i.imgur.com/c1.jpg', 1 FROM POIs WHERE Name=N'Hải Sản Vĩnh Khánh 1'
UNION ALL
SELECT PoiId, N'https://i.imgur.com/d1.jpg', 1 FROM POIs WHERE Name=N'Quán Ốc Tô'
UNION ALL
SELECT PoiId, N'https://i.imgur.com/e1.jpg', 1 FROM POIs WHERE Name=N'Hải Sản Làng Chài';


/* =========================
   INSERT RESTAURANTS
========================= */
INSERT INTO Restaurants (Name, Address, Description, PoiId)
SELECT N'Ốc Đào', N'212 Vĩnh Khánh, Quận 4',
N'Quán ốc lâu năm, đông khách',
PoiId FROM POIs WHERE Name=N'Ốc Đào - Vĩnh Khánh'
UNION ALL
SELECT N'Ốc Oanh', N'213 Vĩnh Khánh, Quận 4',
N'Ốc xào me nổi tiếng',
PoiId FROM POIs WHERE Name=N'Ốc Oanh - Vĩnh Khánh'
UNION ALL
SELECT N'Hải Sản VK1', N'220 Vĩnh Khánh, Quận 4',
N'Hải sản tươi sống',
PoiId FROM POIs WHERE Name=N'Hải Sản Vĩnh Khánh 1'
UNION ALL
SELECT N'Ốc Tô', N'230 Vĩnh Khánh, Quận 4',
N'Quán bình dân',
PoiId FROM POIs WHERE Name=N'Quán Ốc Tô'
UNION ALL
SELECT N'Làng Chài', N'235 Vĩnh Khánh, Quận 4',
N'Hải sản cao cấp',
PoiId FROM POIs WHERE Name=N'Hải Sản Làng Chài';


/* =========================
   INSERT FOODS
========================= */
INSERT INTO Foods (Name, Price, Description, RestaurantId)
SELECT N'Ốc hương rang muối',60000,N'Món bán chạy',RestaurantId FROM Restaurants WHERE Name=N'Ốc Đào'
UNION ALL
SELECT N'Nghêu hấp sả',45000,N'Tươi và thơm',RestaurantId FROM Restaurants WHERE Name=N'Ốc Đào'
UNION ALL
SELECT N'Ốc len xào dừa',55000,N'Béo ngậy',RestaurantId FROM Restaurants WHERE Name=N'Ốc Oanh'
UNION ALL
SELECT N'Tôm nướng',120000,N'Tôm sú tươi',RestaurantId FROM Restaurants WHERE Name=N'Hải Sản VK1'
UNION ALL
SELECT N'Ốc móng tay xào tỏi',60000,N'Đậm đà',RestaurantId FROM Restaurants WHERE Name=N'Ốc Tô'
UNION ALL
SELECT N'Cua hấp bia',180000,N'Tươi sống',RestaurantId FROM Restaurants WHERE Name=N'Làng Chài';


/* =========================
   INSERT GEOFENCES
========================= */
INSERT INTO Geofences (PoiId, Radius)
SELECT PoiId, 40 FROM POIs;


/* =========================
   INSERT QR CODES
========================= */
INSERT INTO QRCodes (PoiId, CodeValue)
SELECT PoiId, CONCAT('QR-', PoiId) FROM POIs;


/* =========================
   INSERT NARRATIONS (VI + EN)
========================= */

-- Ốc Đào
INSERT INTO Narrations (PoiId, LanguageCode, Text)
SELECT PoiId,'vi',
N'Ốc Đào là quán ốc lâu năm tại phố Vĩnh Khánh, nổi tiếng với các món ốc hương rang muối và nghêu hấp sả.'
FROM POIs WHERE Name=N'Ốc Đào - Vĩnh Khánh';

INSERT INTO Narrations (PoiId, LanguageCode, Text)
SELECT PoiId,'en',
N'Oc Dao is a long-standing seafood restaurant on Vinh Khanh street, famous for salted grilled snails and steamed clams.'
FROM POIs WHERE Name=N'Ốc Đào - Vĩnh Khánh';


-- Ốc Oanh
INSERT INTO Narrations (PoiId, LanguageCode, Text)
SELECT PoiId,'vi',
N'Ốc Oanh được yêu thích với các món ốc xào me, ốc len xào dừa và sò điệp nướng mỡ hành.'
FROM POIs WHERE Name=N'Ốc Oanh - Vĩnh Khánh';

INSERT INTO Narrations (PoiId, LanguageCode, Text)
SELECT PoiId,'en',
N'Oc Oanh is popular for tamarind stir-fried snails and grilled scallops with scallion oil.'
FROM POIs WHERE Name=N'Ốc Oanh - Vĩnh Khánh';


-- Hải Sản VK1
INSERT INTO Narrations (PoiId, LanguageCode, Text)
SELECT PoiId,'vi',
N'Hải Sản Vĩnh Khánh 1 chuyên phục vụ hải sản tươi sống, khách có thể chọn trực tiếp tại bể.'
FROM POIs WHERE Name=N'Hải Sản Vĩnh Khánh 1';

INSERT INTO Narrations (PoiId, LanguageCode, Text)
SELECT PoiId,'en',
N'Vinh Khanh Seafood 1 serves fresh seafood that customers can choose directly from the tanks.'
FROM POIs WHERE Name=N'Hải Sản Vĩnh Khánh 1';


-- Ốc Tô
INSERT INTO Narrations (PoiId, LanguageCode, Text)
SELECT PoiId,'vi',
N'Quán Ốc Tô là quán bình dân, phục vụ nhanh và phù hợp tụ tập bạn bè.'
FROM POIs WHERE Name=N'Quán Ốc Tô';

INSERT INTO Narrations (PoiId, LanguageCode, Text)
SELECT PoiId,'en',
N'Oc To is a casual restaurant with fast service, suitable for gathering with friends.'
FROM POIs WHERE Name=N'Quán Ốc Tô';


-- Làng Chài
INSERT INTO Narrations (PoiId, LanguageCode, Text)
SELECT PoiId,'vi',
N'Hải Sản Làng Chài nổi tiếng với cua hấp bia và hàu nướng phô mai.'
FROM POIs WHERE Name=N'Hải Sản Làng Chài';

INSERT INTO Narrations (PoiId, LanguageCode, Text)
SELECT PoiId,'en',
N'Lang Chai Seafood is famous for beer-steamed crab and grilled oysters with cheese.'
FROM POIs WHERE Name=N'Hải Sản Làng Chài';
