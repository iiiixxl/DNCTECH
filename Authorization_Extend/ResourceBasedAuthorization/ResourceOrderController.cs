using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Authorization_Extend.ResourceBasedAuthorization;

/// <summary>
/// 【基于资源的动态授权】演示：功能权限之外，再校验「你能不能操作『这一条』资源」。
/// </summary>
/// <remarks>
/// 与前几种方案的本质区别：
/// - 角色 / PolicyCode / 仿 ABP 回答的是「你能不能退款」（功能权限，和具体订单无关）。
/// - 本方案回答的是「你能不能退『这张』订单」（资源权限，必须拿到订单实例才能判断）。
///
/// 做法：在业务逻辑里注入 IAuthorizationService，调用 AuthorizeAsync(User, 资源对象, 策略名)，
/// 由资源型 Handler(AuthorizationHandler&lt;TRequirement, TResource&gt;) 比对资源归属。
/// 这样即使用户有 orders.refund 功能权限，也退不了别人 / 别租户的订单，堵住越权漏洞。
/// </remarks>
[ApiController]
[Route("api/resource-orders")]
[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
public class ResourceOrderController : ControllerBase
{
    private readonly IOrderStore _orderStore;
    private readonly IAuthorizationService _authorizationService;

    public ResourceOrderController(IOrderStore orderStore, IAuthorizationService authorizationService)
    {
        _orderStore = orderStore;
        _authorizationService = authorizationService;
    }

    /// <summary>列出全部订单（仅用于对照观察数据，不做资源校验）。</summary>
    [HttpGet]
    public IActionResult GetAll() => Ok(new { approach = "resource-based", data = _orderStore.All() });

    /// <summary>
    /// 退款：必须是「订单创建者本人」才能退。演示数据所有权控制。
    /// </summary>
    [HttpPost("{id:int}/refund")]
    public async Task<IActionResult> Refund(int id)
    {
        var order = _orderStore.Find(id);
        if (order is null)
        {
            return NotFound(new { message = $"订单 {id} 不存在" });
        }

        // 关键：把「具体资源对象」交给授权系统，而不只是检查一个策略名
        var result = await _authorizationService.AuthorizeAsync(
            User,
            order,
            ResourceAuthorizationPolicyNames.OrderOwner);

        if (!result.Succeeded)
        {
            return Forbid();
        }

        return Ok(new { approach = "resource-based", message = $"已为订单 {id} 退款 {order.Amount:C}" });
    }

    /// <summary>
    /// 查看订单详情：必须是「同租户」才能看。演示多租户数据隔离。
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetDetail(int id)
    {
        var order = _orderStore.Find(id);
        if (order is null)
        {
            return NotFound(new { message = $"订单 {id} 不存在" });
        }

        var result = await _authorizationService.AuthorizeAsync(
            User,
            order,
            ResourceAuthorizationPolicyNames.SameTenant);

        if (!result.Succeeded)
        {
            return Forbid();
        }

        return Ok(new { approach = "resource-based", data = order });
    }
}
