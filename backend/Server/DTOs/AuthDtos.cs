namespace Server.DTOs
{
	public record LoginRequest(string Username, string Password);

	public record LoginResponse(
		string AccessToken,
		string Role,
		string DisplayName,
		int? ManagedRestaurantId
	);
}