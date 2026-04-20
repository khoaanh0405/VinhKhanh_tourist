namespace Server.DTOs
{
    public record LoginResponse(
        string AccessToken,
        string Role,
        string DisplayName,
        int? ManagedRestaurantId
    );
}