namespace Authentication_jwt_cookie.Models;

/// <summary>
/// 商品实体（演示数据，用于 JWT / Cookie 双认证接口返回）。
/// </summary>
public class Product
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal Price { get; set; }
}
