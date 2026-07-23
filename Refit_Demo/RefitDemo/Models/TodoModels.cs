namespace Refit_Demo.RefitDemo.Models;

public class TodoItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool Completed { get; set; }
    public string? Owner { get; set; }
}

public class CreateTodoRequest
{
    public string Title { get; set; } = string.Empty;
    public bool Completed { get; set; }
}

public class TodoQueryParams
{
    public bool? Completed { get; set; }
    public int? Limit { get; set; }
}
