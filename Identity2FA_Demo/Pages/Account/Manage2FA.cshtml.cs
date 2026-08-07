using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Identity2FA_Demo.Pages.Account;

[Authorize]
public class Manage2FAModel : PageModel
{
    private const string AuthenticatorUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";

    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly UrlEncoder _urlEncoder;

    public Manage2FAModel(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        UrlEncoder urlEncoder)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _urlEncoder = urlEncoder;
    }

    public bool Is2faEnabled { get; set; }
    public string? SharedKey { get; set; }
    public string? AuthenticatorUri { get; set; }
    public string[]? RecoveryCodes { get; set; }
    public string? StatusMessage { get; set; }

    [BindProperty]
    public string Code { get; set; } = "";

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        Is2faEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
        await LoadSharedKeyAndUriAsync(user);
        return Page();
    }

    public async Task<IActionResult> OnPostEnableAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var verificationCode = Code.Replace(" ", string.Empty).Replace("-", string.Empty);
        var isValid = await _userManager.VerifyTwoFactorTokenAsync(
            user, _userManager.Options.Tokens.AuthenticatorTokenProvider, verificationCode);

        if (!isValid)
        {
            ModelState.AddModelError(nameof(Code), "验证码无效，请重试。");
            Is2faEnabled = false;
            await LoadSharedKeyAndUriAsync(user);
            return Page();
        }

        await _userManager.SetTwoFactorEnabledAsync(user, true);
        var codes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 5);
        RecoveryCodes = codes?.ToArray();
        Is2faEnabled = true;
        StatusMessage = "2FA 已启用。请保存恢复码（只显示一次）。";
        await LoadSharedKeyAndUriAsync(user);
        return Page();
    }

    public async Task<IActionResult> OnPostDisableAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        await _userManager.SetTwoFactorEnabledAsync(user, false);
        await _userManager.ResetAuthenticatorKeyAsync(user);
        await _signInManager.RefreshSignInAsync(user);
        StatusMessage = "2FA 已关闭。";
        Is2faEnabled = false;
        await LoadSharedKeyAndUriAsync(user);
        return Page();
    }

    private async Task LoadSharedKeyAndUriAsync(IdentityUser user)
    {
        var key = await _userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(key))
        {
            await _userManager.ResetAuthenticatorKeyAsync(user);
            key = await _userManager.GetAuthenticatorKeyAsync(user);
        }

        SharedKey = FormatKey(key!);
        var email = await _userManager.GetEmailAsync(user) ?? user.UserName ?? "user";
        AuthenticatorUri = string.Format(
            CultureInfo.InvariantCulture,
            AuthenticatorUriFormat,
            _urlEncoder.Encode("Identity2FA_Demo"),
            _urlEncoder.Encode(email),
            key);
    }

    private static string FormatKey(string unformattedKey)
    {
        var result = new StringBuilder();
        var currentPosition = 0;
        while (currentPosition + 4 < unformattedKey.Length)
        {
            result.Append(unformattedKey.AsSpan(currentPosition, 4)).Append(' ');
            currentPosition += 4;
        }
        if (currentPosition < unformattedKey.Length)
            result.Append(unformattedKey.AsSpan(currentPosition));
        return result.ToString().ToLowerInvariant();
    }
}
