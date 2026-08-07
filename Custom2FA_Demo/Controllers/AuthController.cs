using Custom2FA_Demo.Models;
using Custom2FA_Demo.Services;
using Microsoft.AspNetCore.Mvc;

namespace Custom2FA_Demo.Controllers;

[ApiController]
[Route("api")]
public class AuthController : ControllerBase
{
    private readonly SignInService _signIn;

    public AuthController(SignInService signIn)
    {
        _signIn = signIn;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        var result = await _signIn.RegisterAsync(req.UserName, req.Password, req.Email, req.Phone);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var result = await _signIn.PasswordSignInAsync(req.UserName, req.Password);
        return result.Succeeded || result.RequiresTwoFactor ? Ok(result) : BadRequest(result);
    }

    [HttpPost("login/2fa")]
    public async Task<IActionResult> LoginWith2Fa([FromBody] TwoFactorRequest req)
    {
        var result = await _signIn.TwoFactorSignInAsync(req.MfaTicket, req.Provider, req.Code);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpPost("login/recovery")]
    public async Task<IActionResult> LoginWithRecovery([FromBody] RecoveryRequest req)
    {
        var result = await _signIn.RecoveryCodeSignInAsync(req.MfaTicket, req.RecoveryCode);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
}
