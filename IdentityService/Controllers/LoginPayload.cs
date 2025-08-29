namespace IdentityService.Controllers;

public class LoginPayload
{
    public string Username { get; set; }
    public string Password { get; set; }
    public string ReturnUrl { get; set; }
}