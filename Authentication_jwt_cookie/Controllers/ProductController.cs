using Authentication_jwt_cookie.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Authentication_jwt_cookie.Controllers;

/// <summary>
/// JWT 或 Cookie 任一认证通过即可访问的商品接口。
/// </summary>
/// <remarks>
/// 类上叠加两个 [Authorize] 表示「或」关系：
/// 携带有效 JWT（Authorization: Bearer ...）或有效 Cookie 均可访问。
/// </remarks>
[ApiController]
[Route("[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
public class ProductController : ControllerBase
{
    private static readonly Product[] Products =
    [
        new Product { Id = 1, Name = "机械键盘", Price = 599.00m },
        new Product { Id = 2, Name = "无线鼠标", Price = 199.00m },
        new Product { Id = 3, Name = "显示器支架", Price = 129.00m }
    ];

    [HttpGet("{id:int}", Name = "GetProductById")]
    public ActionResult<Product> GetById(int id)
    {
        var product = Products.FirstOrDefault(p => p.Id == id);
        if (product is null)
        {
            return NotFound();
        }

        return product;
    }
}
