using Custom2FA_Demo.Domain;
using Custom2FA_Demo.Models;
using Custom2FA_Demo.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QRCoder;

namespace Custom2FA_Demo.Controllers;

[ApiController]
[Authorize]
[Route("api/2fa")]
public class TwoFactorController : ControllerBase
{
    private readonly SignInService _signIn;

    public TwoFactorController(SignInService signIn)
    {
        _signIn = signIn;
    }

    [HttpGet("setup")]
    public async Task<IActionResult> Setup()
    {
        if (!User.TryGetUserId(out var userId)) return Unauthorized();

        var (result, key, uri) = await _signIn.GetSetupInfoAsync(userId);
        if (!result.Succeeded)
            return BadRequest(result);

        // 服务端生成 PNG data URL，避免依赖外网 CDN（原先 jsdelivr 路径本身就不存在）
        string? qrCodeImage = null;
        if (!string.IsNullOrEmpty(uri))
        {
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(uri, QRCodeGenerator.ECCLevel.Q);
            var png = new PngByteQRCode(data);
            var bytes = png.GetGraphic(6);
            qrCodeImage = "data:image/png;base64," + Convert.ToBase64String(bytes);
        }

        return Ok(new { sharedKey = key, authenticatorUri = uri, qrCodeImage });
    }

    [HttpPost("enable")]
    public async Task<IActionResult> Enable([FromBody] Enable2FaRequest req)
    {
        if (!User.TryGetUserId(out var userId)) return Unauthorized();

        var result = await _signIn.ConfirmEnable2FaAsync(userId, req.Code);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpPost("methods")]
    public async Task<IActionResult> SetMethods([FromBody] SetMethodsRequest req)
    {
        if (!User.TryGetUserId(out var userId)) return Unauthorized();

        var result = await _signIn.SetTwoFactorMethodsAsync(userId, (TwoFactorMethods)req.Methods);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpPost("disable")]
    public async Task<IActionResult> Disable()
    {
        if (!User.TryGetUserId(out var userId)) return Unauthorized();

        var result = await _signIn.Disable2FaAsync(userId);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
}
