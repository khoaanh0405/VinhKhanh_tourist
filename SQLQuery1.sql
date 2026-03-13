USE master;
GO

-- Xóa database cũ nếu đã tồn tại để làm mới hoàn toàn
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
    Username NVARCHAR(100) NOT NULL UNIQUE,
    Password NVARCHAR(200) NOT NULL,
    Role NVARCHAR(50) NOT NULL
        CHECK (Role IN ('Admin','Manager','Staff')),
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);


/* =========================
   2. LANGUAGES
========================= */
CREATE TABLE Languages (
    LanguageCode NVARCHAR(10) PRIMARY KEY,
    LanguageName NVARCHAR(100) NOT NULL
);


/* =========================
   3. POIs (Đã tích hợp Rating trực tiếp)
========================= */
CREATE TABLE POIs (
    PoiId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    Latitude FLOAT NOT NULL,
    Longitude FLOAT NOT NULL,
    Description NVARCHAR(500) NULL,

    AverageRating FLOAT NOT NULL DEFAULT 0.0, -- Điểm đánh giá (VD: 4.8)
    ReviewCount INT NOT NULL DEFAULT 0,       -- Số lượt đánh giá (VD: 1250)

    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

CREATE INDEX IX_POIs_Name ON POIs(Name);


/* =========================
   4. POI IMAGES (Dành cho Cloudinary)
========================= */
CREATE TABLE PoiImages (
    ImageId INT IDENTITY(1,1) PRIMARY KEY,
    PoiId INT NOT NULL,

    ImageUrl NVARCHAR(1000) NOT NULL, 
    PublicId NVARCHAR(255) NULL, 
    DisplayOrder INT NOT NULL DEFAULT 0,

    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,

    FOREIGN KEY (PoiId) REFERENCES POIs(PoiId)
        ON DELETE CASCADE
);

CREATE INDEX IX_PoiImages_PoiId ON PoiImages(PoiId);


/* =========================
   5. RESTAURANTS
========================= */
CREATE TABLE Restaurants (
    RestaurantId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    Address NVARCHAR(300),
    Description NVARCHAR(500),
    PoiId INT NOT NULL,
    FOREIGN KEY (PoiId) REFERENCES POIs(PoiId)
        ON DELETE CASCADE
);

CREATE INDEX IX_Restaurants_PoiId ON Restaurants(PoiId);


/* =========================
   6. FOODS
========================= */
CREATE TABLE Foods (
    FoodId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    Price DECIMAL(10,2) NOT NULL,
    Description NVARCHAR(500),
    RestaurantId INT NOT NULL,
    FOREIGN KEY (RestaurantId) REFERENCES Restaurants(RestaurantId)
        ON DELETE CASCADE
);

CREATE INDEX IX_Foods_RestaurantId ON Foods(RestaurantId);


/* =========================
   7. NARRATIONS (TTS + CLOUDINARY AUDIO)
========================= */
CREATE TABLE Narrations (
    NarrationId INT IDENTITY(1,1) PRIMARY KEY,
    PoiId INT NOT NULL,
    LanguageCode NVARCHAR(10) NOT NULL,

    Text NVARCHAR(MAX) NOT NULL,

    AudioUrl NVARCHAR(1000) NULL,  
    AudioPublicId NVARCHAR(255) NULL, 

    DurationSeconds INT NULL,
    UseAudioFile BIT NOT NULL DEFAULT 0,

    VoiceName NVARCHAR(100) NULL,
    SpeechRate FLOAT NOT NULL DEFAULT 0.5,
    Volume FLOAT NOT NULL DEFAULT 1.0,

    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

    FOREIGN KEY (PoiId) REFERENCES POIs(PoiId)
        ON DELETE CASCADE,
    FOREIGN KEY (LanguageCode) REFERENCES Languages(LanguageCode),

    CONSTRAINT UQ_Poi_Language UNIQUE (PoiId, LanguageCode)
);

CREATE INDEX IX_Narrations_PoiId ON Narrations(PoiId);


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

CREATE INDEX IX_Geofences_PoiId ON Geofences(PoiId);


/* =========================
   9. QRCODES
========================= */
CREATE TABLE QRCodes (
    QRCodeId INT IDENTITY(1,1) PRIMARY KEY,
    PoiId INT NOT NULL,
    CodeValue NVARCHAR(200) NOT NULL UNIQUE,
    FOREIGN KEY (PoiId) REFERENCES POIs(PoiId)
        ON DELETE CASCADE
);

CREATE INDEX IX_QRCodes_PoiId ON QRCodes(PoiId);

/* =========================
   10. Reviews
========================= */
CREATE TABLE Reviews (
    ReviewId INT IDENTITY(1,1) PRIMARY KEY,
    PoiId INT NOT NULL,
    UserName NVARCHAR(100) NOT NULL DEFAULT N'Khách ẩn danh',
    Rating INT NOT NULL CHECK (Rating >= 1 AND Rating <= 5),
    Comment NVARCHAR(1000) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (PoiId) REFERENCES POIs(PoiId) ON DELETE CASCADE
);
CREATE INDEX IX_Reviews_PoiId ON Reviews(PoiId);
GO

/* =========================================================================
   10. BƠM DỮ LIỆU MẪU (DEMO DATA) - QUÁN ĂN VÀ TOP RATED
========================================================================= */

-- 1. Thêm Ngôn ngữ
INSERT INTO Languages (LanguageCode, LanguageName) VALUES ('vi', N'Tiếng Việt'), ('en', N'English');

-- 2. Thêm dữ liệu POI (Các quán ăn ở Vĩnh Khánh) với Rating khác nhau để test xếp hạng
INSERT INTO POIs (Name, Latitude, Longitude, Description, AverageRating, ReviewCount) 
VALUES 
(N'Ốc Oanh Vĩnh Khánh', 10.761500, 106.704200, N'Quán ốc nổi tiếng và đông khách nhất khu phố ẩm thực Vĩnh Khánh với hải sản tươi sống và nước chấm đặc trưng.', 4.8, 1250),
(N'Ốc Vũ', 10.762000, 106.704500, N'Không gian thoáng mát, menu đa dạng. Đặc biệt nổi tiếng với các món ốc rang muối ớt và càng ghẹ rang me.', 4.5, 840),
(N'Lẩu Bò Khu Nhà Cháy', 10.761000, 106.703800, N'Lẩu bò truyền thống với nước dùng đậm đà, thịt bò mềm và rau rừng tươi ngon. Phù hợp cho nhóm bạn bè tụ tập.', 4.2, 560),
(N'Sườn Nướng Đảo', 10.762500, 106.705000, N'Chuyên các món sườn nướng BBQ xốt cay ngọt, cực kỳ hợp để nhâm nhi cùng bia mát lạnh.', 4.7, 920),
(N'Hải Sản Biển Đông', 10.760500, 106.703500, N'Hải sản bắt tại hồ, chế biến tại chỗ. Không gian rộng rãi phù hợp cho gia đình.', 4.6, 610);

-- 3. Thêm Ảnh cho POI (Dùng link ảnh tĩnh từ Unsplash cho đẹp)
INSERT INTO PoiImages (PoiId, ImageUrl, DisplayOrder) VALUES 
(1, 'https://res.cloudinary.com/dfxbdpxkc/image/upload/v1772255164/placeholder_img_ypdb0p.webp', 1),
(2, 'https://res.cloudinary.com/dfxbdpxkc/image/upload/v1772255164/haisanvk1_tank_oag0wf.jpg', 1),
(3, 'https://res.cloudinary.com/dfxbdpxkc/image/upload/v1772255164/monanmau_xaj5lo.jpg', 1),
(4, 'https://images.unsplash.com/photo-1544025162-d76694265947?q=80&w=1000', 1),
(5, 'https://images.unsplash.com/photo-1599598425947-330026e10a27?q=80&w=1000', 1);

-- 4. Thêm Restaurant gắn với POI
INSERT INTO Restaurants (Name, Address, Description, PoiId) VALUES 
(N'Quán Ốc Oanh', N'534 Vĩnh Khánh, Phường 8, Quận 4', N'Chuyên hải sản tươi sống.', 1),
(N'Ốc Vũ', N'37 Vĩnh Khánh, Phường 8, Quận 4', N'Chuyên các món ốc xào, nướng.', 2),
(N'Lẩu Bò Nam Cường', N'Vĩnh Khánh, Phường 8, Quận 4', N'Chuyên lẩu bò các loại.', 3),
(N'Sườn Nướng Đảo', N'120 Vĩnh Khánh, Phường 8, Quận 4', N'Sườn nướng tảng khổng lồ.', 4);

-- 5. Thêm Món Ăn (Foods) cho các quán
INSERT INTO Foods (Name, Price, Description, RestaurantId) VALUES 
(N'Ốc hương rang muối ớt', 120000, N'Ốc hương size lớn, cay nồng mặn ngọt.', 1),
(N'Càng ghẹ rang me', 150000, N'Càng ghẹ tươi, sốt me chua ngọt đậm đà.', 1),
(N'Sò điệp nướng mỡ hành', 80000, N'Sò điệp béo ngậy cùng đậu phộng.', 2),
(N'Lẩu bò thập cẩm', 250000, N'Nước dùng hầm xương 12 tiếng, đầy đủ gân, đuôi, thịt.', 3),
(N'Sườn cây nướng cay', 180000, N'Sườn cây nướng than hoa sốt đặc biệt.', 4);

-- 6. Thêm Narrations (Thuyết minh - Dùng UseAudioFile = 0 để TTS đọc tự động)
INSERT INTO Narrations (PoiId, LanguageCode, Text, UseAudioFile, VoiceName) VALUES 
(1, 'vi', N'Chào mừng bạn đến với Ốc Oanh. Đây là quán ốc lâu đời và được đánh giá cao nhất tại phố ẩm thực Vĩnh Khánh. Bạn nhất định phải thử món ốc hương rang muối ớt nhé.', 0, 'vi-VN'),
(2, 'vi', N'Bạn đang ở gần Ốc Vũ. Một địa điểm tuyệt vời để thưởng thức hải sản với không gian nhộn nhịp đặc trưng của Sài Gòn về đêm.', 0, 'vi-VN'),
(3, 'vi', N'Nếu bạn đang thèm một chút đồ nước nóng hổi, lẩu bò Vĩnh Khánh ngay cạnh đây là một lựa chọn tuyệt vời.', 0, 'vi-VN'),
(4, 'vi', N'Mùi sườn nướng thơm lừng đang tỏa ra từ Sườn Nướng Đảo. Quán nổi tiếng với các món thịt nướng BBQ tảng lớn.', 0, 'vi-VN');
GO

INSERT INTO Reviews (PoiId, UserName, Rating, Comment) VALUES 
(1, N'Tuấn Anh', 5, N'Hải sản ở đây cực kỳ tươi. Món ốc hương rang muối ớt đậm đà, ăn là ghiền!'),
(1, N'Mai Phương', 4, N'Quán hơi đông nên phục vụ có lúc chậm, nhưng bù lại đồ ăn rất ngon và nóng hổi.'),
(2, N'Lê Đình Hoàng', 5, N'Không gian thoáng, giá cả hợp lý. Sò điệp nướng mỡ hành ở đây là chân ái.');
GO