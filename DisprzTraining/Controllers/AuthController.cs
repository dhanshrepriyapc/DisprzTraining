using DisprzTraining.Business;
using DisprzTraining.DTOs;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;

    public AuthController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto login)
    {
        var user = await _userService.AuthenticateAsync(login.Username, login.Password);
        if (user == null)
            return Unauthorized("Invalid username or password");

        var token = _userService.GenerateJwtToken(user); // You need this method in UserService
        return Ok(new { token });
    }
}