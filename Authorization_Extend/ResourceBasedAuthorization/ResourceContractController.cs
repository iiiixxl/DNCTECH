using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Authorization_Extend.ResourceBasedAuthorization;

/// <summary>
/// 演示合同编辑：与订单退款走同一个 <see cref="OwnedResourceAuthorizationHandler"/>，
/// 证明「不是本人就不能编辑」无需为合同再写一个 Handler。
/// </summary>
[ApiController]
[Route("api/resource-contracts")]
[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
public class ResourceContractController : ControllerBase
{
    private readonly IContractStore _contractStore;
    private readonly IAuthorizationService _authorizationService;

    public ResourceContractController(IContractStore contractStore, IAuthorizationService authorizationService)
    {
        _contractStore = contractStore;
        _authorizationService = authorizationService;
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] string title)
    {
        var contract = _contractStore.Find(id);
        if (contract is null)
        {
            return NotFound(new { message = $"合同 {id} 不存在" });
        }

        // 与订单退款相同：一条组合策略同时校验租户 + 归属人
        var result = await _authorizationService.AuthorizeAsync(
            User,
            contract,
            ResourceAuthorizationPolicyNames.OwnerInTenant);

        if (!result.Succeeded)
        {
            return Forbid();
        }

        return Ok(new { approach = "resource-based", message = $"已更新合同 {id} 标题为 {title}" });
    }
}
