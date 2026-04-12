namespace client.lib.core;

public static class DatabaseConstants
{
    public const string DatabaseFilename = "VinhKhanhOffline.db3";

    public const SQLite.SQLiteOpenFlags Flags =
        // Mở file ở chế độ đọc/ghi
        SQLite.SQLiteOpenFlags.ReadWrite |
        // Tự động tạo file nếu chưa có
        SQLite.SQLiteOpenFlags.Create |
        // Hỗ trợ truy cập từ nhiều luồng (Multi-threading)
        SQLite.SQLiteOpenFlags.SharedCache;

    public static string DatabasePath =>
        Path.Combine(FileSystem.AppDataDirectory, DatabaseFilename);
}