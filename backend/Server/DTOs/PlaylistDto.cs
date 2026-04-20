namespace Server.DTOs
{
    // ── Trả về danh sách Playlist ────────────────────────────
    public record PlaylistSummaryDto(
        int PlaylistId,
        string Title,
        int ItemCount,
        DateTime CreatedAt
    );

    // ── Chi tiết 1 item trong Playlist (trả về cho Mobile) ───
    public record PlaylistItemDetailDto(
        int PoiId,
        string PoiName,
        string? RestaurantName,
        string? Address,
        double Latitude,
        double Longitude,
        int DisplayOrder
    );

    // ── Payload khi tạo Playlist mới ─────────────────────────
    public record CreatePlaylistDto(
        string Title,
        List<int> PoiIds       // Danh sách PoiId được chọn
    );
}