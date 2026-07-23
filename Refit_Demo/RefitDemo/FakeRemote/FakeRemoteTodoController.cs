using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Refit_Demo.RefitDemo.Models;

namespace Refit_Demo.RefitDemo.FakeRemote;

/// <summary>
/// 本机「假远端」API：模拟外部微服务，供 Refit 出站调用。不依赖外网。
/// </summary>
[ApiController]
[Route("fake-remote/todos")]
public class FakeRemoteTodoController : ControllerBase
{
    private readonly InMemoryTodoStore _store;

    public FakeRemoteTodoController(InMemoryTodoStore store)
    {
        _store = store;
    }

    [AllowAnonymous]
    [HttpGet]
    public ActionResult<List<TodoItem>> List([FromQuery] bool? completed, [FromQuery] int? limit)
    {
        return Ok(_store.Query(completed, limit).ToList());
    }

    [AllowAnonymous]
    [HttpGet("{id:int}")]
    public ActionResult<TodoItem> Get(int id)
    {
        var item = _store.Get(id);
        return item is null ? NotFound(new { message = $"Todo {id} not found" }) : Ok(item);
    }

    [AllowAnonymous]
    [HttpPost]
    public ActionResult<TodoItem> Create([FromBody] CreateTodoRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new { message = "Title is required" });
        }

        var owner = User.Identity?.IsAuthenticated == true ? User.Identity.Name : "anonymous";
        var item = _store.Create(request, owner);
        return CreatedAtAction(nameof(Get), new { id = item.Id }, item);
    }

    /// <summary>需 JWT：用来验证 DemoAuthHeaderHandler 是否正确转发 Token。</summary>
    [Authorize]
    [HttpGet("secure/mine")]
    public ActionResult<List<TodoItem>> Mine()
    {
        var name = User.Identity?.Name ?? string.Empty;
        return Ok(_store.GetByOwner(name).ToList());
    }
}
