namespace SecureApi.Shared.Auth;

public record TokenRequest(string Username, string Password);

public record TokenResponse(string AccessToken, string RefreshToken, int ExpiresIn);
public record RefreshRequest(string RefreshToken);
