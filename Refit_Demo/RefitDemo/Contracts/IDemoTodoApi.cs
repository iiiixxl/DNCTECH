using Refit;
using Refit_Demo.RefitDemo.Models;

namespace Refit_Demo.RefitDemo.Contracts;

/// <summary>
/// Refit 契约：只声明「要什么」，由源码生成器生成 HttpClient 实现。
/// 路径对应 FakeRemoteTodoController。
/// </summary>
public interface IDemoTodoApi
{
    [Get("/fake-remote/todos")]
    Task<List<TodoItem>> GetTodos(TodoQueryParams @params);

    [Get("/fake-remote/todos/{id}")]
    Task<TodoItem> GetTodo(int id);

    /// <summary>不抛 ApiException，自行看 IsSuccessful / Error。</summary>
    [Get("/fake-remote/todos/{id}")]
    Task<IApiResponse<TodoItem>> GetTodoSafe(int id);

    [Post("/fake-remote/todos")]
    Task<TodoItem> CreateTodo([Body] CreateTodoRequest request);

    /// <summary>需 JWT；演示 AuthHeaderHandler 横切注入 Bearer。</summary>
    [Get("/fake-remote/todos/secure/mine")]
    Task<List<TodoItem>> GetMySecureTodos();
}
