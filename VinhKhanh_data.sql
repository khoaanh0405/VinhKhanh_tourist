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
   1. USERS (Đã bỏ IsLocked)
========================= */
CREATE TABLE Users (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    DisplayName NVARCHAR(100) NOT NULL,
    Username NVARCHAR(100) NOT NULL UNIQUE,
    Email NVARCHAR(200) NOT NULL,
    Password NVARCHAR(200) NOT NULL,
    Role NVARCHAR(50) NOT NULL
        CHECK (Role IN ('Admin','Manager','Tourist')),
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
   3. BẢNG MỚI: UI TRANSLATIONS (Dịch giao diện động)
========================= */
CREATE TABLE UITranslations (
    TranslationId INT IDENTITY(1,1) PRIMARY KEY,
    LanguageCode NVARCHAR(10) NOT NULL,
    ResourceKey NVARCHAR(100) NOT NULL,  
    ResourceValue NVARCHAR(500) NOT NULL,
    FOREIGN KEY (LanguageCode) REFERENCES Languages(LanguageCode) ON DELETE CASCADE,
    CONSTRAINT UQ_UITranslation UNIQUE (LanguageCode, ResourceKey)
);
CREATE INDEX IX_UITranslations_Lang ON UITranslations(LanguageCode);

/* =========================
   4. POIs
========================= */
CREATE TABLE POIs (
    PoiId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    Latitude FLOAT NOT NULL,
    Longitude FLOAT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
CREATE INDEX IX_POIs_Name ON POIs(Name);

/* =========================
   5. POI IMAGES 
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
   6. RESTAURANTS (Đã bỏ IsLocked)
========================= */
CREATE TABLE Restaurants (
    RestaurantId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    Address NVARCHAR(300),
    PoiId INT NOT NULL,
    ManagerUserId INT NULL,
    FOREIGN KEY (PoiId) REFERENCES POIs(PoiId) ON DELETE CASCADE,
    FOREIGN KEY (ManagerUserId) REFERENCES Users(UserId)
);
CREATE INDEX IX_Restaurants_PoiId ON Restaurants(PoiId);
CREATE INDEX IX_Restaurants_ManagerUserId ON Restaurants(ManagerUserId);

/* =========================
   7. FOODS
========================= */
CREATE TABLE Foods (
    FoodId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    Price DECIMAL(10,2) NOT NULL,
    RestaurantId INT NOT NULL,
    FOREIGN KEY (RestaurantId) REFERENCES Restaurants(RestaurantId) ON DELETE CASCADE
);
CREATE INDEX IX_Foods_RestaurantId ON Foods(RestaurantId);

/* =========================
   8. NARRATIONS 
========================= */
CREATE TABLE Narrations (
    NarrationId INT IDENTITY(1,1) PRIMARY KEY,
    PoiId INT NOT NULL,
    LanguageCode NVARCHAR(10) NOT NULL,
    Text NVARCHAR(MAX) NOT NULL,
    VoiceName NVARCHAR(100) NULL,
    SpeechRate FLOAT NOT NULL DEFAULT 0.5,
    Volume FLOAT NOT NULL DEFAULT 1.0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (PoiId) REFERENCES POIs(PoiId) ON DELETE CASCADE,
    FOREIGN KEY (LanguageCode) REFERENCES Languages(LanguageCode) ON DELETE CASCADE,
    CONSTRAINT UQ_Poi_Language UNIQUE (PoiId, LanguageCode)
);
CREATE INDEX IX_Narrations_PoiId ON Narrations(PoiId);

/* =========================
   9. GEOFENCES
========================= */
CREATE TABLE Geofences (
    GeofenceId INT IDENTITY(1,1) PRIMARY KEY,
    PoiId INT NOT NULL,
    Radius FLOAT NOT NULL,
    FOREIGN KEY (PoiId) REFERENCES POIs(PoiId) ON DELETE CASCADE
);
CREATE INDEX IX_Geofences_PoiId ON Geofences(PoiId);

/* =========================
   10. QRCODES
========================= */
CREATE TABLE Playlists (
    PlaylistId INT IDENTITY(1,1) PRIMARY KEY,
    Title NVARCHAR(200) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

/* =========================
   10. PLAYLIST ITEMS (N-N)
========================= */
CREATE TABLE PlaylistItems (
    PlaylistItemId INT IDENTITY(1,1) PRIMARY KEY,
    PlaylistId INT NOT NULL,
    PoiId INT NOT NULL,
    DisplayOrder INT NOT NULL DEFAULT 0,
    CONSTRAINT FK_PlaylistItems_Playlist FOREIGN KEY (PlaylistId) REFERENCES Playlists(PlaylistId) ON DELETE CASCADE,
    CONSTRAINT FK_PlaylistItems_POI FOREIGN KEY (PoiId) REFERENCES POIs(PoiId) ON DELETE CASCADE,
    CONSTRAINT UQ_PlaylistItem UNIQUE (PlaylistId, PoiId)
);
CREATE INDEX IX_PlaylistItems_PlaylistId ON PlaylistItems(PlaylistId);

/* =========================
   11. QRCODES (Đã hỗ trợ Playlist)
========================= */
CREATE TABLE QRCodes (
    QRCodeId INT IDENTITY(1,1) PRIMARY KEY,
    PoiId INT NULL,
    PlaylistId INT NULL,
    CodeValue NVARCHAR(200) NOT NULL UNIQUE,
    CONSTRAINT FK_QRCodes_POIs FOREIGN KEY (PoiId) REFERENCES POIs(PoiId) ON DELETE CASCADE,
    CONSTRAINT FK_QRCodes_Playlists FOREIGN KEY (PlaylistId) REFERENCES Playlists(PlaylistId) ON DELETE CASCADE,
    -- Ràng buộc: Một mã QR chỉ được trỏ tới POI HOẶC Playlist, không được cả 2, cũng không được để trống cả 2
    CONSTRAINT CK_QRCode_OneTarget CHECK (
        (PoiId IS NOT NULL AND PlaylistId IS NULL) OR
        (PoiId IS NULL AND PlaylistId IS NOT NULL)
    )
);
CREATE INDEX IX_QRCodes_PoiId ON QRCodes(PoiId);
CREATE INDEX IX_QRCodes_PlaylistId ON QRCodes(PlaylistId);

/* =========================
   11. BẢNG DỊCH THUẬT DATA
========================= */
CREATE TABLE FoodTranslations (
    TranslationId INT IDENTITY(1,1) PRIMARY KEY,
    FoodId INT NOT NULL,
    LanguageCode NVARCHAR(10) NOT NULL,
    Name NVARCHAR(200) NOT NULL, 
    FOREIGN KEY (FoodId) REFERENCES Foods(FoodId) ON DELETE CASCADE,
    FOREIGN KEY (LanguageCode) REFERENCES Languages(LanguageCode) ON DELETE CASCADE,
    CONSTRAINT UQ_FoodTrans UNIQUE (FoodId, LanguageCode)
);
GO

CREATE TABLE Devices (
    DeviceId NVARCHAR(100) PRIMARY KEY,
    FirstSeenAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- 2. Bảng theo dõi phiên hoạt động (Active Users)
CREATE TABLE ActiveSessions (
    DeviceId NVARCHAR(100) PRIMARY KEY,
    LastSeenAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (DeviceId) REFERENCES Devices(DeviceId) ON DELETE CASCADE
);
-- Tạo index để truy vấn đếm user online nhanh hơn
CREATE INDEX IX_ActiveSessions_LastSeen ON ActiveSessions(LastSeenAt);

-- 3. Bảng lịch sử quét QR (QR Scans) - Đã cập nhật hỗ trợ Playlist
CREATE TABLE QRScanLogs (
    LogId INT IDENTITY(1,1) PRIMARY KEY,
    DeviceId NVARCHAR(100) NOT NULL,
    PoiId INT NULL,                      -- Cho phép NULL vì bạn bỏ quét từng quán
    PlaylistId INT NULL,                 -- THÊM MỚI: Để lưu ID của Playlist khi quét
    ScannedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    FOREIGN KEY (DeviceId) REFERENCES Devices(DeviceId) ON DELETE CASCADE,
    FOREIGN KEY (PoiId) REFERENCES POIs(PoiId) ON DELETE SET NULL,
    CONSTRAINT FK_QRScanLogs_Playlists FOREIGN KEY (PlaylistId) REFERENCES Playlists(PlaylistId) ON DELETE CASCADE,
    
    -- Đảm bảo phải quét trúng 1 thứ (POI hoặc Playlist)
    CONSTRAINT CK_ScanTarget CHECK (PoiId IS NOT NULL OR PlaylistId IS NOT NULL)
);

-- Cập nhật Index để kiểm tra trùng lặp (chống spam) cho cả Playlist
CREATE INDEX IX_QRScanLogs_CheckDuplicate ON QRScanLogs(DeviceId, PoiId, PlaylistId, ScannedAt);

/* =========================================================================
   INSERT DỮ LIỆU MẪU
========================================================================= */

-- 1. Thêm Ngôn ngữ
INSERT INTO Languages (LanguageCode, LanguageName) VALUES 
('vi', N'Tiếng Việt'); 

-- 3. Thêm dữ liệu POI
INSERT INTO POIs (Name, Latitude, Longitude) VALUES 
(N'Ốc Oanh', 10.761500, 106.704200),
(N'Ốc Vũ', 10.762000, 106.704500),
(N'Ốc Thảo', 10.761000, 106.703800),
(N'Ớt Xiêm Quán', 10.762500, 106.705000);

-- 4. Thêm Ảnh cho POI
INSERT INTO PoiImages (PoiId, ImageUrl, DisplayOrder) VALUES 
(1, 'https://res.cloudinary.com/dfxbdpxkc/image/upload/v1773670287/pho-am-thuc-vinh-khanh-oc-oanh-1707245308_edp5ru.jpg', 1),
(2, 'https://res.cloudinary.com/dfxbdpxkc/image/upload/v1773670028/oc_vu_w4kpng.jpg', 1),
(3, 'https://res.cloudinary.com/dfxbdpxkc/image/upload/v1773670536/oc_thao_ed6ynw.jpg', 1),
(4, 'https://res.cloudinary.com/dfxbdpxkc/image/upload/v1773669779/ot_xiem_azo6rq.jpg', 1);

-- 5. Thêm Restaurant gắn với POI
INSERT INTO Restaurants (Name, Address, PoiId) VALUES 
(N'Quán Ốc Oanh', N'534 Vĩnh Khánh, Phường 8, Quận 4', 1),
(N'Ốc Vũ', N'37 Vĩnh Khánh, Phường 8, Quận 4', 2),
(N'Ốc thảo', N'383 Vĩnh Khánh, Phường 8, Quận 4', 3),
(N'Ớt Xiêm Quán', N'120 Vĩnh Khánh, Phường 8, Quận 4', 4);

-- 6. Thêm Món Ăn (Foods) cho các quán
INSERT INTO Foods (Name, Price, RestaurantId) VALUES 
(N'Ốc hương rang muối ớt', 120000, 1),
(N'Càng ghẹ rang me', 150000, 1),
(N'Sò điệp nướng mỡ hành', 80000, 2),
(N'Lẩu bò thập cẩm', 250000, 3),
(N'Lẩu thái chua cay', 180000, 4);

-- 7. Thêm Narrations 
-- 7. Thêm Narrations 
INSERT INTO Narrations (PoiId, LanguageCode, Text, VoiceName) VALUES 
-- Dữ liệu của Ốc Oanh (PoiId = 1)
(1, 'vi', N'Chào mừng bạn đến với Ốc Oanh. Đây là quán ốc lâu đời và được đánh giá cao nhất tại phố ẩm thực Vĩnh Khánh. Bạn nhất định phải thử món ốc hương rang muối ớt nhé.', 'vi-VN'),

-- Dữ liệu của Ốc Vũ (Giả sử PoiId = 2)
(2, 'vi', N'Chào mừng bạn đến với Ốc Vũ. Một điểm đến sôi động với thực đơn vô cùng phong phú. Đừng bỏ lỡ món sò điệp nướng mỡ hành thơm lừng và ốc móng tay xào bơ tỏi tại đây nhé.', 'vi-VN'),

-- Dữ liệu của Ốc Thảo (Giả sử PoiId = 3)
(3, 'vi', N'Chào mừng bạn đến với Ốc Thảo. Nơi đây nổi tiếng với hải sản tươi ngon và mức giá cực kỳ bình dân. Món càng ghẹ rang me chua ngọt chắc chắn sẽ làm hài lòng vị giác của bạn.', 'vi-VN'),

(4, 'vi', N'Chào mừng bạn đến với Ớt Xiêm Quán. Một điểm đến sôi động với thực đơn vô cùng phong phú. Đừng bỏ lỡ món sò điệp nướng mỡ hành thơm lừng và ốc móng tay xào bơ tỏi tại đây nhé.', 'vi-VN');

INSERT INTO UITranslations (LanguageCode, ResourceKey, ResourceValue) VALUES
-- ==========================================
-- 1. TRANG CÀI ĐẶT (SettingsPage)
-- ==========================================
('vi', 'SettingsTitle', N'Cài đặt'),
('vi', 'SettingsSubtitle', N'Tùy chỉnh trải nghiệm của bạn'),
('vi', 'SettingsSectionLanguage', N'Ngôn ngữ'),
('vi', 'SettingsLanguageTitle', N'Ngôn ngữ ứng dụng'),
('vi', 'SettingsLanguageDesc', N'Thay đổi ngôn ngữ hiển thị'),
('vi', 'SettingsSectionTools', N'Công cụ'),
('vi', 'SettingsQrTitle', N'Quét mã QR'),
('vi', 'SettingsQrDesc', N'Khám phá địa điểm nhanh chóng'),
('vi', 'SettingsQrOpen', N'Mở camera'),
('vi', 'SettingsSyncTitle', N'Đồng bộ Offline'),
('vi', 'SettingsSyncDesc', N'Tải dữ liệu để dùng khi không có mạng'),
('vi', 'SettingsSyncAction', N'Đồng bộ ngay'),
('vi', 'SettingsSectionNotifications', N'Thông báo & Âm thanh'),
('vi', 'SettingsPushTitle', N'Thông báo đẩy'),
('vi', 'SettingsPushDesc', N'Nhận thông báo về địa điểm mới'),
('vi', 'SettingsSoundTitle', N'Âm thanh ứng dụng'),
('vi', 'SettingsSoundDesc', N'Bật/tắt âm thanh giao diện'),
('vi', 'SettingsSectionInfo', N'Thông tin'),
('vi', 'SettingsAboutTitle', N'Về ứng dụng'),
('vi', 'SettingsAboutDesc', N'Phiên bản, bản quyền, liên hệ'),
('vi', 'SettingsRateTitle', N'Đánh giá ứng dụng'),
('vi', 'SettingsRateDesc', N'Chia sẻ cảm nghĩ của bạn'),
('vi', 'SettingsFooterTagline', N'Khám phá Vĩnh Khánh theo cách của bạn'),
('vi', 'SettingsFooterVersion', N'Phiên bản 1.0.0'),
('vi', 'SettingsLanguageChangedMsg', N'Đã đổi ngôn ngữ sang:'),

-- ==========================================
-- 2. TRANG CHỦ (HomePage)
-- ==========================================
('vi', 'HomeGreeting', N'Xin chào,'),
('vi', 'ExploreTitle', N'Khám phá Vĩnh Khánh'),
('vi', 'TopFavorites', N'Top được yêu thích'),
('vi', 'AllLocations', N'Tất cả địa điểm'),
('vi', 'HomeSort', N'Sắp xếp'),
('vi', 'HomeDistanceMeters', N'{0} m'),
('vi', 'HomeDistanceKm', N'{0} km'),

-- ==========================================
-- 3. MÀN HÌNH QUÉT QR (QrScannerPage)
-- ==========================================
('vi', 'Title_ScanQR', N'Quét mã QR'),
('vi', 'QrGuideText', N'📷 Đưa mã QR vào khung hình'),

-- ==========================================
-- 4. MÀN HÌNH BẢN ĐỒ (MapPage)
-- ==========================================
('vi', 'MapButtonDetail', N'Xem chi tiết'),
('vi', 'Map_PinDetailHint', N'Nhấn để xem chi tiết'),

-- ==========================================
-- 5. MÀN HÌNH CHI TIẾT (POIDetailPage)
-- ==========================================
('vi', 'Detail_LocationName', N'Khu ẩm thực Vĩnh Khánh'),
('vi', 'Detail_MenuTitle', N'Thực đơn'),
('vi', 'Detail_EmptyMenu', N'Chưa có thông tin thực đơn.'),
('vi', 'DetailAudioTitle', N'Nhấn để nghe'),
('vi', 'DetailAudioReady', N'Sẵn sàng phát'),
('vi', 'Audio_Playing', N'Đang phát...'),
('vi', 'Audio_Ready', N'Sẵn sàng phát'),

-- ==========================================
-- 6. CÁC THÔNG BÁO VÀ HỘP THOẠI (Alerts)
-- ==========================================
('vi', 'Alert_Success', N'Thành công'),
('vi', 'Alert_Notice', N'Thông báo'),
('vi', 'Alert_NoAudio', N'Chưa có audio cho địa điểm này.'),
('vi', 'Alert_AutoAudioTitle', N'Bật tự động thuyết minh'),
('vi', 'Alert_AutoAudioDesc', N'Ứng dụng sẽ tự động phát âm thanh giới thiệu chi tiết khi bạn đi ngang qua các địa điểm. Bạn có muốn kích hoạt?'),
('vi', 'Alert_AutoAudioOnSuccess', N'Đã BẬT thuyết minh tự động 🎧'),
('vi', 'Alert_TurnOffAudioTitle', N'Tắt tự động thuyết minh'),
('vi', 'Alert_TurnOffAudioDesc', N'Bạn có muốn tắt tính năng tự động phát âm thanh giới thiệu không?'),
('vi', 'Alert_AutoAudioOffSuccess', N'Đã TẮT thuyết minh tự động 🔇'),

-- ==========================================
-- 7. NÚT BẤM DÙNG CHUNG (Buttons)
-- ==========================================
('vi', 'Btn_OK', N'OK'),
('vi', 'Btn_TurnOn', N'Bật'),
('vi', 'Btn_TurnOff', N'Tắt'),
('vi', 'Btn_Cancel', N'Hủy'),
('vi', 'Btn_Back', N'Quay lại'),

('vi', 'Playlist_Title', N'Danh Sách Phát'),
('vi', 'Playlist_Loading', N'Đang tải danh sách...'),
('vi', 'Playlist_Empty', N'Playlist này chưa có địa điểm nào.'),
('vi', 'Playlist_Error', N'Không thể tải danh sách. Kiểm tra kết nối mạng.'),
('vi', 'Playlist_ItemCount', N'{0} địa điểm'),
('vi', 'Playlist_PlayingStatus', N'🔊 Đang phát thuyết minh...');
GO

-- 10. Thêm Admin
INSERT INTO Users (DisplayName, Username, Email, Password, Role)
VALUES (
    N'Quản trị viên Hệ thống',    
    'admin',                   
    'admin@vinhkhanh.com',        
    '$2a$12$DtQn17/piuDv2TPjriFBc.H2KN9qMfzccwU140LSnf4GXuDeKBQIS', 
    'Admin'                       
);
