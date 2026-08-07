using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Identity2FA_Demo.Pages.Account;

public class LoginWith2faModel : PageModel
{
    private readonly SignInManager<IdentityUser> _signInManager;

    public LoginWith2faModel(SignInManager<IdentityUser> signInManager)
    {
        _signInManager = signInManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public bool RememberMe { get; set; }

    public class InputModel
    {
        [Required, StringLength(7, MinimumLength = 6)]
        [Display(Name = "验证码")]
        public string TwoFactorCode { get; set; } = "";

        public bool RememberMachine { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(bool rememberMe)
    {
        // 依赖第一步登录留下的临时 2FA cookie
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user == null)
            return RedirectToPage("./Login");

        RememberMe = rememberMe;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(bool rememberMe)
    {
        if (!ModelState.IsValid) return Page();

        RememberMe = rememberMe;
        var authenticatorCode = Input.TwoFactorCode.Replace(" ", string.Empty).Replace("-", string.Empty);

        var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(
            authenticatorCode, rememberMe, Input.RememberMachine);

        if (result.Succeeded)
            return RedirectToPage("/Index");

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "账号已锁定。");
            return Page();
        }

        ModelState.AddModelError(string.Empty, "验证码无效。");
        return Page();
    }
}
