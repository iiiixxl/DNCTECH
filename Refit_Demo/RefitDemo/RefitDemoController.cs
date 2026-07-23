using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Refit;
using Refit_Demo.RefitDemo.Contracts;
using Refit_Demo.RefitDemo.Models;
using AspNetAuthorize = Microsoft.AspNetCore.Authorization.AuthorizeAttribute;

namespace Refit_Demo.RefitDemo;

/// <summary>
/// 业务入口：只依赖 IDemoTodoApi，不手写 HttpClient。
/// </summary>
[ApiController]
[Route("refit-demo")]
public class RefitDemoController : ControllerBase
{
    private readonly IDemoTodoApi _api;

    public RefitDemoController(IDemoTodoApi api)
    {
        _api = api;
    }

    /// <summary>Query 对象展开 → GET /fake-remote/todos?completed=&amp;limit=</summary>
    [AllowAnonymous]
    [HttpGet("todos")]
    public async Task<IActionResult> GetTodos([FromQuery] bool? completed, [FromQuery] int? limit, CancellationToken ct)
    {
        var list = await _api.GetTodos(new TodoQueryParams
        {
            Completed = completed,
            Limit = limit
        });
        return Ok(new { via = "Refit IDemoTodoApi.GetTodos", count = list.Count, items = list });
    }

    /// <summary>Task&lt;T&gt;：404 时抛 ApiException。</summary>
    [AllowAnonymous]
    [HttpGet("todos/{id:int}")]
    public async Task<IActionResult> GetTodo(int id, CancellationToken ct)
    {
        try
        {
            var item = await _api.GetTodo(id);
            return Ok(new { via = "Refit GetTodo (Task<T>)", item });
        }
        catch (ApiException ex)
        {
            return StatusCode((int)ex.StatusCode, new
            {
                via = "ApiException",
                statusCode = (int)ex.StatusCode,
                uri = ex.Uri?.ToString(),
                content = ex.Content
            });
        }
    }

    /// <summary>IApiResponse&lt;T&gt;：不抛，自行判断 IsSuccessful。</summary>
    [AllowAnonymous]
    [HttpGet("todos/{id:int}/safe")]
    public async Task<IActionResult> GetTodoSafe(int id, CancellationToken ct)
    {
        var response = await _api.GetTodoSafe(id);
        if (!response.IsSuccessful)
        {
            var apiError = response.Error as ApiException;
            var status = (int)(response.StatusCode ?? System.Net.HttpStatusCode.InternalServerError);
            return StatusCode(status, new
            {
                via = "IApiResponse (no throw)",
                isSuccessful = false,
                statusCode = status,
                error = apiError?.Content ?? response.Error?.Message
            });
        }

        return Ok(new
        {
            via = "IApiResponse (no throw)",
            isSuccessful = true,
            item = response.Content
        });
    }

    [AllowAnonymous]
    [HttpPost("todos")]
    public async Task<IActionResult> CreateTodo([FromBody] CreateTodoRequest request, CancellationToken ct)
    {
        try
        {
            var item = await _api.CreateTodo(request);
            return Ok(new { via = "Refit CreateTodo [Body]", item });
        }
        catch (ApiException ex)
        {
            return StatusCode((int)ex.StatusCode, new
            {
                via = "ApiException",
                statusCode = (int)ex.StatusCode,
                content = ex.Content
            });
        }
    }

    /// <summary>
    /// 需先登录拿 Token，再带 Bearer 调本接口；
    /// AuthHeaderHandler 会把 Token 转发到 FakeRemote 的 [Authorize] 接口。
    /// </summary>
    [AspNetAuthorize]
    [HttpGet("todos/secure/mine")]
    public async Task<IActionResult> GetMySecureTodos(CancellationToken ct)
    {
        try
        {
            var list = await _api.GetMySecureTodos();
            return Ok(new
            {
                via = "Refit + DemoAuthHeaderHandler → FakeRemote [Authorize]",
                caller = User.Identity?.Name,
                count = list.Count,
                items = list
            });
        }
        catch (ApiException ex)
        {
            return StatusCode((int)ex.StatusCode, new
            {
                via = "ApiException",
                statusCode = (int)ex.StatusCode,
                hint = "确认已登录且 Authorization Bearer 已传入；Handler 是否转发成功",
                content = ex.Content
            });
        }
    }
}
