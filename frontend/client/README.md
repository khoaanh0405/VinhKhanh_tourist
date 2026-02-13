# Vĩnh Khánh Tourism Mobile App

Ứng dụng mobile hướng dẫn du lịch đa ngôn ngữ giới thiệu ẩm thực và văn hóa Vĩnh Khánh, Bến Tre.

## Tính năng chính

### 1. Thuyết minh đa ngôn ngữ
- Hỗ trợ 11 ngôn ngữ: Tiếng Việt, English, Français, Deutsch, Español, Italiano, Русский, 日本語, 한국어, 中文, ไทย
- Phát âm thanh tự động (TTS - Text to Speech)
- Điều chỉnh tốc độ phát (0.5x - 2.0x)
- Tua nhanh/tua lùi 10 giây
- Mini player luôn hiển thị khi đang phát

### 2. Bản đồ tương tác
- Hiển thị tất cả điểm tham quan trên Google Maps
- Xác định vị trí hiện tại
- Tìm kiếm địa điểm gần bạn (trong bán kính 5km)
- Chỉ đường đến địa điểm
- Chế độ xem bản đồ: Bình thường, Vệ tinh, Địa hình

### 3. Quét mã QR
- Quét mã QR tại địa điểm để xem thông tin chi tiết
- Tự động chuyển đến trang chi tiết địa điểm
- Hỗ trợ đèn flash khi quét trong môi trường tối

### 4. Giới thiệu nhà hàng & món ăn
- Danh sách nhà hàng tại mỗi địa điểm
- Thực đơn chi tiết với giá cả
- Mô tả món ăn
- Địa chỉ và thông tin liên hệ

### 5. Quản lý cá nhân
- Đánh dấu địa điểm yêu thích
- Lưu lịch sử tham quan
- Thống kê số địa điểm đã ghé thăm
- Tùy chỉnh ngôn ngữ hiển thị

## Cấu trúc dự án

```
lib/
├── main.dart                          # Entry point
├── core/
│   └── constants/
│       └── app_constants.dart         # API endpoints, constants
├── models/
│   └── models.dart                    # Data models (POI, Restaurant, Food, etc.)
├── services/
│   └── api_service.dart               # API service layer
├── providers/
│   ├── app_provider.dart              # Main app state management
│   └── audio_player_provider.dart     # Audio player state management
├── screens/
│   ├── main_screen.dart               # Main screen with bottom navigation
│   ├── home/
│   │   └── home_screen.dart           # Home screen
│   ├── map/
│   │   └── map_screen.dart            # Map screen with Google Maps
│   ├── poi/
│   │   └── poi_detail_screen.dart     # POI detail screen
│   ├── qr/
│   │   └── qr_scanner_screen.dart     # QR code scanner
│   └── favorites_profile.dart         # Favorites & Profile screens
└── widgets/
    ├── widgets.dart                   # Reusable widgets (POICard, RestaurantCard, etc.)
    ├── audio_player_widget.dart       # Full audio player
    └── audio_player_mini.dart         # Mini audio player
```

## Cài đặt

### Yêu cầu
- Flutter SDK 3.0+
- Dart SDK 3.0+
- Android Studio / Xcode
- Google Maps API Key

### Bước 1: Clone repository
```bash
git clone [repository-url]
cd vinh_khanh_tourism_flutter
```

### Bước 2: Cài đặt dependencies
```bash
flutter pub get
```

### Bước 3: Cấu hình API
Mở file `lib/core/constants/app_constants.dart` và thay đổi `baseUrl`:
```dart
static const String baseUrl = 'https://your-api-domain.com/api';
```

### Bước 4: Cấu hình Google Maps

#### Android
1. Mở `android/app/src/main/AndroidManifest.xml`
2. Thêm API key:
```xml
<application>
    <meta-data
        android:name="com.google.android.geo.API_KEY"
        android:value="YOUR_GOOGLE_MAPS_API_KEY"/>
</application>
```

#### iOS
1. Mở `ios/Runner/AppDelegate.swift`
2. Thêm:
```swift
import GoogleMaps

GMSServices.provideAPIKey("YOUR_GOOGLE_MAPS_API_KEY")
```

### Bước 5: Chạy ứng dụng
```bash
# Android
flutter run

# iOS
flutter run -d ios

# Web (development)
flutter run -d chrome
```

## Build production

### Android APK
```bash
flutter build apk --release
```

### Android App Bundle
```bash
flutter build appbundle --release
```

### iOS
```bash
flutter build ios --release
```

## API Endpoints được sử dụng

### Authentication
- POST `/User/login` - Đăng nhập
- POST `/User/register` - Đăng ký
- GET `/User/me` - Thông tin user

### POIs (Points of Interest)
- GET `/POIs` - Danh sách tất cả địa điểm
- GET `/POIs/{id}` - Chi tiết địa điểm
- GET `/POIs/{id}/language/{lang}` - Địa điểm với ngôn ngữ cụ thể
- GET `/POIs/nearby?latitude={lat}&longitude={lng}&radiusKm={radius}` - Địa điểm gần

### Narrations
- GET `/Narrations` - Danh sách thuyết minh
- GET `/Narrations/{id}` - Chi tiết thuyết minh
- GET `/Narrations/poi/{poiId}` - Thuyết minh của địa điểm
- GET `/Narrations/poi/{poiId}/language/{lang}` - Thuyết minh theo ngôn ngữ

### Restaurants
- GET `/Restaurant` - Danh sách nhà hàng
- GET `/Restaurant/{id}` - Chi tiết nhà hàng

### Foods
- GET `/Food/ByRestaurant/{restaurantId}` - Món ăn của nhà hàng

### Languages
- GET `/Language` - Danh sách ngôn ngữ hỗ trợ

### Audio
- GET `/AudioFile/by-narration/{narrationId}` - File âm thanh của thuyết minh

## Các tính năng nổi bật

### 1. Audio Player
- Phát âm thanh thuyết minh tự động
- Điều khiển: Play/Pause, Skip Forward/Backward
- Progress bar tương tác
- Tốc độ phát linh hoạt
- Mini player luôn hiển thị

### 2. Geofencing (Sẵn sàng)
- Tự động phát thuyết minh khi đến gần địa điểm
- Thông báo khi vào vùng geofence

### 3. Offline Support (Có thể mở rộng)
- Cache dữ liệu địa điểm
- Tải trước file âm thanh
- Hoạt động khi không có internet

### 4. Multi-language
- Chuyển đổi ngôn ngữ mượt mà
- Lưu ngôn ngữ ưa thích
- Thuyết minh đa ngôn ngữ

## State Management

Ứng dụng sử dụng **Provider** pattern để quản lý state:

- **AppProvider**: Quản lý state chính của app (POIs, Languages, User, Favorites)
- **AudioPlayerProvider**: Quản lý audio player state

## Local Storage

Sử dụng **SharedPreferences** để lưu:
- Authentication token
- Ngôn ngữ đã chọn
- Danh sách yêu thích
- Lịch sử tham quan

## Permissions yêu cầu

### Android (`android/app/src/main/AndroidManifest.xml`)
```xml
<uses-permission android:name="android.permission.INTERNET"/>
<uses-permission android:name="android.permission.ACCESS_FINE_LOCATION"/>
<uses-permission android:name="android.permission.ACCESS_COARSE_LOCATION"/>
<uses-permission android:name="android.permission.CAMERA"/>
```

### iOS (`ios/Runner/Info.plist`)
```xml
<key>NSCameraUsageDescription</key>
<string>Cần quyền camera để quét mã QR</string>
<key>NSLocationWhenInUseUsageDescription</key>
<string>Cần quyền vị trí để hiển thị địa điểm gần bạn</string>
<key>NSLocationAlwaysUsageDescription</key>
<string>Cần quyền vị trí để tự động phát thuyết minh</string>
```

## Tối ưu hóa

### Performance
- Lazy loading danh sách
- Image caching
- API response caching
- Debounce cho search

### UX
- Loading indicators
- Error handling
- Offline mode
- Smooth animations

## Testing

```bash
# Run tests
flutter test

# Run tests with coverage
flutter test --coverage
```

## Troubleshooting

### Lỗi Google Maps không hiển thị
- Kiểm tra API key đã được thêm đúng chưa
- Kiểm tra API key đã enable Maps SDK chưa
- Kiểm tra internet connection

### Lỗi Audio không phát
- Kiểm tra URL audio có đúng không
- Kiểm tra network connection
- Kiểm tra file format (hỗ trợ mp3, wav, aac)

### Lỗi QR Scanner
- Kiểm tra quyền camera
- Kiểm tra format QR code

## Đóng góp

Mọi đóng góp đều được chào đón! Vui lòng:
1. Fork repository
2. Tạo branch mới (`git checkout -b feature/AmazingFeature`)
3. Commit changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to branch (`git push origin feature/AmazingFeature`)
5. Tạo Pull Request

## License

MIT License

## Liên hệ

Email: your-email@example.com
Website: https://your-website.com
