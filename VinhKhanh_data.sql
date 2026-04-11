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
    DisplayName NVARCHAR(100) NOT NULL,
    Username NVARCHAR(100) NOT NULL UNIQUE,
    Password NVARCHAR(200) NOT NULL,
    Role NVARCHAR(50) NOT NULL
        CHECK (Role IN ('Admin','Manager','Tourist')),
    IsLocked BIT NOT NULL DEFAULT 0, -- Tích hợp: 0 là Hoạt động, 1 là Đã khóa
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
    AverageRating FLOAT NOT NULL DEFAULT 0.0, 
    ReviewCount INT NOT NULL DEFAULT 0,       
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
    FOREIGN KEY (PoiId) REFERENCES POIs(PoiId) ON DELETE CASCADE
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
    ManagerUserId INT NULL, -- Tích hợp: Gán chủ quán
    IsLocked BIT NOT NULL DEFAULT 0, -- Tích hợp: Khóa nhà hàng
    FOREIGN KEY (PoiId) REFERENCES POIs(PoiId) ON DELETE CASCADE,
    FOREIGN KEY (ManagerUserId) REFERENCES Users(UserId)
);
CREATE INDEX IX_Restaurants_PoiId ON Restaurants(PoiId);
CREATE INDEX IX_Restaurants_ManagerUserId ON Restaurants(ManagerUserId);

/* =========================
   6. FOODS
========================= */
CREATE TABLE Foods (
    FoodId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    Price DECIMAL(10,2) NOT NULL,
    Description NVARCHAR(500),
    RestaurantId INT NOT NULL,
    FOREIGN KEY (RestaurantId) REFERENCES Restaurants(RestaurantId) ON DELETE CASCADE
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
    FOREIGN KEY (PoiId) REFERENCES POIs(PoiId) ON DELETE CASCADE,
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
    FOREIGN KEY (PoiId) REFERENCES POIs(PoiId) ON DELETE CASCADE
);
CREATE INDEX IX_Geofences_PoiId ON Geofences(PoiId);

/* =========================
   9. QRCODES
========================= */
CREATE TABLE QRCodes (
    QRCodeId INT IDENTITY(1,1) PRIMARY KEY,
    PoiId INT NOT NULL,
    CodeValue NVARCHAR(200) NOT NULL UNIQUE,
    FOREIGN KEY (PoiId) REFERENCES POIs(PoiId) ON DELETE CASCADE
);
CREATE INDEX IX_QRCodes_PoiId ON QRCodes(PoiId);

/* =========================
   10. REVIEWS
========================= */
CREATE TABLE Reviews (
    ReviewId INT IDENTITY(1,1) PRIMARY KEY,
    PoiId INT NOT NULL,
    UserName NVARCHAR(100) NOT NULL DEFAULT N'Khách ẩn danh',
    Rating INT NOT NULL CHECK (Rating >= 1 AND Rating <= 5),
    Comment NVARCHAR(1000) NULL,
    IsHidden BIT NOT NULL DEFAULT 0, -- Tích hợp: Ẩn/Hiện đánh giá
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (PoiId) REFERENCES POIs(PoiId) ON DELETE CASCADE
);
CREATE INDEX IX_Reviews_PoiId ON Reviews(PoiId);
GO

/* =========================================================================
   11. BẢNG DỊCH THUẬT (TRANSLATIONS / LOCALIZATION)
========================================================================= */

-- Dịch cho POIs
CREATE TABLE PoiTranslations (
    TranslationId INT IDENTITY(1,1) PRIMARY KEY,
    PoiId INT NOT NULL,
    LanguageCode NVARCHAR(10) NOT NULL,
    Description NVARCHAR(500) NOT NULL,
    FOREIGN KEY (PoiId) REFERENCES POIs(PoiId) ON DELETE CASCADE,
    FOREIGN KEY (LanguageCode) REFERENCES Languages(LanguageCode),
    CONSTRAINT UQ_PoiTrans UNIQUE (PoiId, LanguageCode)
);

-- Dịch cho Restaurants
CREATE TABLE RestaurantTranslations (
    TranslationId INT IDENTITY(1,1) PRIMARY KEY,
    RestaurantId INT NOT NULL,
    LanguageCode NVARCHAR(10) NOT NULL,
    Description NVARCHAR(500) NOT NULL,
    FOREIGN KEY (RestaurantId) REFERENCES Restaurants(RestaurantId) ON DELETE CASCADE,
    FOREIGN KEY (LanguageCode) REFERENCES Languages(LanguageCode),
    CONSTRAINT UQ_ResTrans UNIQUE (RestaurantId, LanguageCode)
);

-- Dịch cho Foods 
CREATE TABLE FoodTranslations (
    TranslationId INT IDENTITY(1,1) PRIMARY KEY,
    FoodId INT NOT NULL,
    LanguageCode NVARCHAR(10) NOT NULL,
    Name NVARCHAR(200) NOT NULL, 
    Description NVARCHAR(500) NOT NULL,
    FOREIGN KEY (FoodId) REFERENCES Foods(FoodId) ON DELETE CASCADE,
    FOREIGN KEY (LanguageCode) REFERENCES Languages(LanguageCode),
    CONSTRAINT UQ_FoodTrans UNIQUE (FoodId, LanguageCode)
);
GO

/* =========================================================================
   12. BƠM DỮ LIỆU MẪU (DEMO DATA) 
========================================================================= */

-- 1. Thêm Ngôn ngữ
INSERT INTO Languages (LanguageCode, LanguageName) VALUES 
('vi', N'Tiếng Việt'), 
('en', N'English'),
('ko', N'한국어 (Korean)');

-- 2. Thêm dữ liệu POI
INSERT INTO POIs (Name, Latitude, Longitude, Description, AverageRating, ReviewCount) 
VALUES 
(N'Ốc Oanh', 10.761500, 106.704200, N'Quán ốc nổi tiếng và đông khách nhất khu phố ẩm thực Vĩnh Khánh với hải sản tươi sống và nước chấm đặc trưng.', 4.8, 1250),
(N'Ốc Vũ', 10.762000, 106.704500, N'Không gian thoáng mát, menu đa dạng. Đặc biệt nổi tiếng với các món ốc rang muối ớt và càng ghẹ rang me.', 4.5, 840),
(N'Ốc Thảo', 10.761000, 106.703800, N'Hoạt động lâu năm với không gian rộng rãi và menu ốc đa dạng.', 4.2, 560),
(N'Ớt Xiêm Quán', 10.762500, 106.705000, N'Quán nhậu có không gian ấm cúng, phục vụ phong phú từ thịt đến hải sản.', 4.7, 920);

-- 3. Thêm Ảnh cho POI
INSERT INTO PoiImages (PoiId, ImageUrl, DisplayOrder) VALUES 
(1, 'https://res.cloudinary.com/dfxbdpxkc/image/upload/v1773670287/pho-am-thuc-vinh-khanh-oc-oanh-1707245308_edp5ru.jpg', 1),
(2, 'https://res.cloudinary.com/dfxbdpxkc/image/upload/v1773670028/oc_vu_w4kpng.jpg', 1),
(3, 'https://res.cloudinary.com/dfxbdpxkc/image/upload/v1773670536/oc_thao_ed6ynw.jpg', 1),
(4, 'https://res.cloudinary.com/dfxbdpxkc/image/upload/v1773669779/ot_xiem_azo6rq.jpg', 1);

-- 4. Thêm Restaurant gắn với POI
INSERT INTO Restaurants (Name, Address, Description, PoiId) VALUES 
(N'Quán Ốc Oanh', N'534 Vĩnh Khánh, Phường 8, Quận 4', N'Chuyên hải sản tươi sống.', 1),
(N'Ốc Vũ', N'37 Vĩnh Khánh, Phường 8, Quận 4', N'Chuyên các món ốc xào, nướng.', 2),
(N'Ốc thảo', N'383 Vĩnh Khánh, Phường 8, Quận 4', N'Chuyên lẩu bò các loại.', 3),
(N'Ớt Xiêm Quán', N'120 Vĩnh Khánh, Phường 8, Quận 4', N'Quán nhậu có không gian ấm cúng.', 4);

-- 5. Thêm Món Ăn (Foods) cho các quán
INSERT INTO Foods (Name, Price, Description, RestaurantId) VALUES 
(N'Ốc hương rang muối ớt', 120000, N'Ốc hương size lớn, cay nồng mặn ngọt.', 1),
(N'Càng ghẹ rang me', 150000, N'Càng ghẹ tươi, sốt me chua ngọt đậm đà.', 1),
(N'Sò điệp nướng mỡ hành', 80000, N'Sò điệp béo ngậy cùng đậu phộng.', 2),
(N'Lẩu bò thập cẩm', 250000, N'Nước dùng hầm xương 12 tiếng, đầy đủ gân, đuôi, thịt.', 3),
(N'Lẩu thái chua cay', 180000, N'Sườn cây nướng than hoa sốt đặc biệt.', 4);

-- 6. Thêm Narrations 
INSERT INTO Narrations (PoiId, LanguageCode, Text, UseAudioFile, VoiceName) VALUES 
(1, 'vi', N'Chào mừng bạn đến với Ốc Oanh. Đây là quán ốc lâu đời và được đánh giá cao nhất tại phố ẩm thực Vĩnh Khánh. Bạn nhất định phải thử món ốc hương rang muối ớt nhé.', 0, 'vi-VN'),
(2, 'vi', N'Bạn đang ở gần Ốc Vũ. Một địa điểm tuyệt vời để thưởng thức hải sản với không gian nhộn nhịp đặc trưng của Sài Gòn về đêm.', 0, 'vi-VN'),
(3, 'vi', N'Nếu bạn đang thèm một chút hải sản tươi ngon, ốc thảo ngay cạnh đây là một lựa chọn tuyệt vời.', 0, 'vi-VN'),
(4, 'vi', N'Ớt Xiêm Quán nổi bật lên như một điểm dừng chân lý tưởng cho những ai trót say mê không khí sôi động của con phố ẩm thực nức tiếng Quận 4.', 0, 'vi-VN'),
(1, 'en', N'Welcome to Oc Oanh. This is the oldest and highest-rated snail restaurant on Vinh Khanh food street. You absolutely must try the roasted sweet snails with chili salt.', 0, 'en-US'),
(2, 'en', N'You are near Oc Vu. A great place to enjoy fresh seafood with the bustling atmosphere typical of Saigon at night.', 0, 'en-US'),
(3, 'en', N'If you are craving some delicious seafood, Oc Thao right next door is an excellent choice for you.', 0, 'en-US'),
(4, 'en', N'Ot Xiem Quan stands out as an ideal stop for those who are passionate about the vibrant atmosphere of the famous food street in District 4.', 0, 'en-US'),
(1, 'ko', N'빈칸 음식 거리에서 가장 오래되고 평점이 높은 옥오안에 오신 것을 환영합니다. 칠리 소금을 곁들인 구운 달팽이를 꼭 맛보세요.', 0, 'ko-KR'),
(2, 'ko', N'오부 근처에 있습니다. 밤이 되면 사이공 특유의 북적이는 분위기와 함께 해산물을 즐기기에 아주 좋은 곳입니다.', 0, 'ko-KR'),
(3, 'ko', N'신선한 해산물이 먹고 싶다면 바로 옆에 있는 옥타오가 탁월한 선택입니다.', 0, 'ko-KR'),
(4, 'ko', N'옷시엠 식당은 4군 유명 먹자골목의 활기찬 분위기를 사랑하는 이들에게 이상적인 장소입니다.', 0, 'ko-KR');

-- 7. Thêm Reviews
INSERT INTO Reviews (PoiId, UserName, Rating, Comment) VALUES 
(1, N'Tuấn Anh', 5, N'Hải sản ở đây cực kỳ tươi. Món ốc hương rang muối ớt đậm đà, ăn là ghiền!'),
(1, N'Mai Phương', 4, N'Quán hơi đông nên phục vụ có lúc chậm, nhưng bù lại đồ ăn rất ngon và nóng hổi.'),
(2, N'Lê Đình Hoàng', 5, N'Không gian thoáng, giá cả hợp lý. Sò điệp nướng mỡ hành ở đây là chân ái.');
GO

-- 8. POI Translations
INSERT INTO PoiTranslations (PoiId, LanguageCode, Description) VALUES 
(1, 'en', N'The most famous and crowded snail restaurant on Vinh Khanh food street with fresh seafood and signature dipping sauces.'),
(2, 'en', N'Airy space, diverse menu. Especially famous for roasted snails with chili salt and roasted crab claws with tamarind.'),
(3, 'en', N'Long-standing establishment with spacious seating and a diverse snail menu.'),
(4, 'en', N'A cozy pub serving a rich variety of dishes from meat to seafood.'),
(1, 'ko', N'빈칸 음식 거리에서 가장 유명하고 붐비는 해산물 식당으로, 신선한 해산물과 특제 디핑 소스를 제공합니다.'),
(2, 'ko', N'쾌적한 공간과 다양한 메뉴. 특히 칠리 소금을 곁들인 구운 달팽이와 타마린드 소스를 곁들인 구운 게 집게발이 유명합니다.'),
(3, 'ko', N'넓은 좌석과 다양한 달팽이 요리 메뉴를 갖춘 오래된 식당입니다.'),
(4, 'ko', N'육류부터 해산물까지 다양한 요리를 제공하는 아늑한 분위기의 식당입니다.');

-- 9. Restaurant Translations
INSERT INTO RestaurantTranslations (RestaurantId, LanguageCode, Description) VALUES 
(1, 'en', N'Specializes in fresh seafood.'),
(2, 'en', N'Specializes in stir-fried and grilled snail dishes.'),
(3, 'en', N'Specializes in various types of beef hotpot.'),
(4, 'en', N'Cozy pub space for gatherings.'),
(1, 'ko', N'신선한 해산물 전문.'),
(2, 'ko', N'달팽이 볶음 및 구이 요리 전문.'),
(3, 'ko', N'소고기 전골 요리 전문.'),
(4, 'ko', N'모임을 위한 아늑한 식당 공간.');

-- 10. Food Translations 
INSERT INTO FoodTranslations (FoodId, LanguageCode, Name, Description) VALUES 
(1, 'en', N'Roasted sweet snails with chili salt', N'Large sweet snails, spicy, salty and sweet.'),
(2, 'en', N'Roasted crab claws with tamarind', N'Fresh crab claws, rich sweet and sour tamarind sauce.'),
(3, 'en', N'Grilled scallops with scallion oil', N'Rich scallops topped with roasted peanuts.'),
(4, 'en', N'Mixed beef hotpot', N'12-hour bone broth, packed with tendon, oxtail, and beef.'),
(5, 'en', N'Spicy Thai hotpot', N'Charcoal-grilled ribs with special sauce.'),
(1, 'ko', N'칠리 소금 맛 구운 달팽이', N'크고 매콤달콤 짭짤한 달팽이.'),
(2, 'ko', N'타마린드 소스 게 집게발 구이', N'신선한 게 집게발, 진한 새콤달콤 타마린드 소스.'),
(3, 'ko', N'파기름 가리비 구이', N'고소한 땅콩을 곁들인 풍미 가득한 가리비.'),
(4, 'ko', N'모듬 소고기 전골', N'12시간 끓인 사골 육수에 힘줄, 꼬리, 소고기가 듬뿍 들어간 전골.'),
(5, 'ko', N'태국식 매운 전골', N'특제 소스를 곁들인 숯불 돼지갈비 구이.');
GO