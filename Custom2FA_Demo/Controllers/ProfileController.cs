using Custom2FA_Demo.Models;
using Custom2FA_Demo.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Custom2FA_Demo.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public class ProfileController : ControllerBase
{
    private readonly SignInService _signIn;

    public ProfileController(SignInService signIn)
    {
        _signIn = signIn;
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        if (!User.TryGetUserId(out var userId)) return Unauthorized();

        var profile = await _signIn.GetProfileAsync(userId);
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpPost("profile/contact")]
    public async Task<IActionResult> UpdateContact([FromBody] ContactRequest req)
    {
        if (!User.TryGetUserId(out var userId)) return Unauthorized();

        var result = await _signIn.UpdateContactAsync(userId, req.Email, req.EmailConfirmed, req.Phone, req.PhoneConfirmed);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
}
