namespace Api.Models;

/// <summary>
/// Request model for user authentication
/// </summary>
/// <param name="Username">User identifier for authentication</param>
/// <param name="Password">User password for authentication</param>
public record UserLogin(string Username, string Password);
