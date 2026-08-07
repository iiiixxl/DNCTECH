using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Identity2FA_Demo.Pages.Account;

public class LoginModel : PageModel
{
    private readonly SignInManager<IdentityUser> _signInManager;

    public LoginModel(SignInManager<IdentityUser> signInManager)
    {
        _signInManager = signInManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = "";

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = "";

        public bool RememberMe { get; set; }
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        // PasswordSignInAsync 内部：密码 OK 后若开启 2FA → RequiresTwoFactor
        // 并写入临时 Cookie（TwoFactorUserIdScheme），不写正式登录 Cookie
        var result = await _signInManager.PasswordSignInAsync(
            Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
            return RedirectToPage("/Index");

        if (result.RequiresTwoFactor)
            return RedirectToPage("./LoginWith2fa", new { Input.RememberMe });

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "账号已锁定，请稍后再试。");
            return Page();
        }

        ModelState.AddModelError(string.Empty, "邮箱或密码错误。");
        return Page();
    }
}
