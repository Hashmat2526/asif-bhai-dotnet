using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.WebEncoders.Testing;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ITodoService>(new TodoService());

var app = builder.Build();

// Assignment 1 (1)
app.MapGet("/get-count", (HttpContext context) =>
{
  string? text = context.Request.Query["text"];

  if (string.IsNullOrEmpty(text))
  {
    // Return an error response if the "text" parameter is missing
    return Results.BadRequest("Text parameter is missing.");
  }

  return TypedResults.Ok(new
  {
    totalCount = text.Replace(" ", "").Length,
    totalWords = text.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length
  });
});

//Assignment 1 (2)
//search for specific word and get its position
app.MapPost("/search", (SearchRequest request) =>
{

  var sentence = request.Sentence;
  var index = sentence.IndexOf(request.Word, StringComparison.OrdinalIgnoreCase);

  return TypedResults.Ok($"{request.Word} is at {index} index");
})
.AddEndpointFilter(async (context, next) =>
{
  var errors = new Dictionary<string, string[]>();
  var body = context.GetArgument<SearchRequest>(0);
  if (string.IsNullOrEmpty(body.Word) || string.IsNullOrEmpty(body.Sentence))
  {
    if (string.IsNullOrEmpty(body.Word))
    {
      errors.Add(nameof(SearchRequest.Word), ["Word cannot be empty"]);
    }
    if (string.IsNullOrEmpty(body.Sentence))
    {
      errors.Add(nameof(SearchRequest.Sentence), ["Sentence cannot be empty"]);
    }

    if (errors.Count > 0)
    {
      return Results.ValidationProblem(errors);
    };
  }
  return await next(context);
});

//Assignment 2
app.MapGet("/get-grade", (HttpContext context) =>
{
  string? score = context.Request.Query["score"];

  if (string.IsNullOrEmpty(score))
  {
    return Results.BadRequest($"score canont be empty or null");
  }

  var grade = GetGrade(int.Parse(score));

  if (string.IsNullOrEmpty(grade))
  {
    return TypedResults.NotFound("not a valid score");
  }

  return TypedResults.Ok(new
  {
    grade
  });
});

//Assignment 3
app.MapGet("/factorial", (HttpContext context) =>
{
  string? num = context.Request.Query["num"];


  if (string.IsNullOrEmpty(num) || int.Parse(num) < 1 || int.Parse(num) > 19)
  {
    return Results.BadRequest($"num canont be empty or null or negative or cannot be greater than 19");
  }

  var factorial = CalculateFactorial(int.Parse(num));

  if (string.IsNullOrEmpty(factorial.ToString()))
  {
    return TypedResults.NotFound("factorial not found");
  }

  return TypedResults.Ok(new
  {
    factorial
  });
});

//function part of assignment 3
static int? CalculateFactorial(int num)
{
  var factorial = num;

  if (num == 2 || num == 1)
  {
    return factorial;
  }

  while (num > 2)
  {
    factorial *= num - 1;
    num--;
  }

  return factorial;
}


//Assignment 4
app.MapPost("/todos", (Todo todo, ITodoService todoService) =>
{
  todoService.AddTodo(todo);
  return TypedResults.Created("todo created ", todo);
})
.AddEndpointFilter(async (context, next) =>
{
  var todoArguments = context.GetArgument<Todo>(0);
  var errors = new Dictionary<string, string[]>();

  if (todoArguments.Id < 1)
  {
    errors.Add(nameof(Todo.Id), ["Id cannot be less than 1"]);
  }

  if (todoArguments.IsCompleted)
  {
    errors.Add(nameof(Todo.IsCompleted), ["todo can not be created with status isCompleted to be true"]);
  }

  if (todoArguments.DueTime < DateTime.UtcNow)
  {
    errors.Add(nameof(Todo.DueTime), ["due date cannot be set in the past"]);
  }

  if (errors.Count > 0)
  {
    return Results.ValidationProblem(errors);
  }

  return await next(context);
});

//assignment 4 part 2
app.MapGet("/todos", (ITodoService todoService) => todoService.GetTodos());

//assignment 4 part 3


//assignment 4 part 4
app.MapGet("/todos/{id}", Results<Ok<Todo>, NotFound> (int id, ITodoService todoService) =>
{
  var todo = todoService.GetTodoById(id);
  return todo is null ? TypedResults.NotFound()
  : TypedResults.Ok(todo);
});

app.MapDelete("/todos/{id}", (int id, ITodoService todoService) =>
{
  todoService.DeleteTodoById(id);
  return TypedResults.NoContent();
});

app.Run();

static string? GetGrade(int score)
{
  if (score >= 90 && score <= 100)
  {
    return "your grade is A";
  }
  else if (score < 90 && score >= 80)
  {
    return "your grade is B";
  }
  else if (score < 80 && score >= 70)
  {
    return "your grade is C";
  }
  else if (score < 70)
  {
    return "your grade is F";
  }
  else
  {
    return null;
  }
}

public class SearchRequest
{
  public SearchRequest(string word, string sentence)
  {
    Word = word;
    Sentence = sentence;
  }

  public string Word { get; set; }
  public string Sentence { get; set; }
}

public record Todo(int Id, string Name, DateTime DueTime, bool IsCompleted) { }

interface ITodoService
{
  Todo? GetTodoById(int id);
  List<Todo> GetTodos();
  Todo? AddTodo(Todo todo);

  void DeleteTodoById(int id);
}

class TodoService : ITodoService
{
  private readonly List<Todo> todos = [];

  public Todo? GetTodoById(int id)
  {
    return todos.FirstOrDefault(t => t.Id == id);
  }

  public List<Todo> GetTodos()
  {
    return todos;
  }

  public void DeleteTodoById(int id)
  {
    todos.RemoveAll(t => t.Id == id);
  }

  public Todo AddTodo(Todo todo)
  {
    todos.Add(todo);
    return todo;
  }

}


/*

// this code is saved for a reference
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Rewrite;

var builder = WebApplication.CreateBuilder(args);

//create services
// builder.Services.AddSingleton<ITaskService>(new InMemoryTaskService());

var app = builder.Build();

app.UseRewriter(new RewriteOptions().AddRedirect("tasks/(.*)", "todos/$1"));

app.Use(async (context, next) =>
{
  Console.WriteLine($"[{context.Request.Method} {context.Request.Path} now body {context.Request.Body} ]");
  await next(context);
});
app.MapGet("/", () => "Hello World!");

var todos = new List<Todo>();

app.MapGet("/todos", () => todos);

app.MapGet("/todos/{id}", Results<Ok<Todo>, NotFound> (int id) =>
{
  var targetTodo = todos.SingleOrDefault(t => id == t.Id);

  return targetTodo is null
  ? TypedResults.NotFound()
  : TypedResults.Ok(targetTodo);
});

app.MapPost("/todos", (Todo task) =>
{
  todos.Add(task);
  return TypedResults.Created("/todos/{id}", task);
})
.AddEndpointFilter(async (context, next) =>
{
  var taskArguments = context.GetArgument<Todo>(0);
  var errors = new Dictionary<string, string[]>();
  if (taskArguments.DueTime < DateTime.UtcNow)
  {
    errors.Add(nameof(Todo.DueTime), ["Cannot have due date in the past"]);
  }
  if (taskArguments.IsCompleted)
  {
    errors.Add(nameof(Todo.IsCompleted), ["Cannot add completed todo"]);
  }

  if (errors.Count > 0)
  {
    return Results.ValidationProblem(errors);
  }

  return await next(context);
});


app.MapDelete("/todos/{id}", (int id) =>
{
  Console.WriteLine(id + " no re");
  todos.RemoveAll(t => id == t.Id);
  return TypedResults.NoContent();
}
);

app.Run();

public record Todo(int Id, string Name, DateTime DueTime, bool IsCompleted) { }


interface ITaskService
{
  Todo? GetTodoById(int id);
  List<Todo> GetTodos();
  void DeleteTodoById(int id);
  Todo AddTodo(Todo task);
}

class InMemoryTaskService : ITaskService
{
  private readonly List<Todo> _todos = [];

  public Todo AddTodo(Todo task)
  {
    _todos.Add(task);
    return task;
  }

  public void DeleteTodoById(int id)
  {
    _todos.RemoveAll((task) => id == task.Id);
  }

  public Todo? GetTodoById(int id)
  {
    return _todos.SingleOrDefault(t => t.Id == id);
  }

  public List<Todo> GetTodos()
  {
    return _todos;
  }
}


------ second file
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Rewrite;

var builder = WebApplication.CreateBuilder(args);

//create services
builder.Services.AddSingleton<ITaskService>(new InMemoryTaskService());

var app = builder.Build();

app.UseRewriter(new RewriteOptions().AddRedirect("tasks/(.*)", "todos/$1"));

app.Use(async (context, next) =>
{
  Console.WriteLine($"[{context.Request.Method} {context.Request.Path} now body {context.Request.Body} ]");
  await next(context);
});
app.MapGet("/", () => "Hello World!");

app.MapGet("/todos", (ITaskService service) => service.GetTodos());

app.MapGet("/todos/{id}", Results<Ok<Todo>, NotFound> (int id, ITaskService service) =>
{
  var targetTodo = service.GetTodoById(id);
  return targetTodo is null
  ? TypedResults.NotFound()
  : TypedResults.Ok(targetTodo);
});

app.MapPost("/todos", (Todo task, ITaskService service) =>
{
  service.AddTodo(task);
  return TypedResults.Created("/todos/{id}", task);
})
.AddEndpointFilter(async (context, next) =>
{
  var taskArguments = context.GetArgument<Todo>(0);
  var errors = new Dictionary<string, string[]>();
  if (taskArguments.DueTime < DateTime.UtcNow)
  {
    errors.Add(nameof(Todo.DueTime), ["Cannot have due date in the past"]);
  }
  if (taskArguments.IsCompleted)
  {
    errors.Add(nameof(Todo.IsCompleted), ["Cannot add completed todo"]);
  }

  if (errors.Count > 0)
  {
    return Results.ValidationProblem(errors);
  }

  return await next(context);
});

app.MapDelete("/todos/{id}", (int id, ITaskService service) =>
{
  Console.WriteLine(id + " no re");
  service.DeleteTodoById(id);
  return TypedResults.NoContent();
}
);

app.Run();

public record Todo(int Id, string Name, DateTime DueTime, bool IsCompleted) { }

interface ITaskService
{
  Todo? GetTodoById(int id);
  List<Todo> GetTodos();
  void DeleteTodoById(int id);
  Todo AddTodo(Todo task);
}

class InMemoryTaskService : ITaskService
{
  private readonly List<Todo> _todos = [];

  public Todo AddTodo(Todo task)
  {
    _todos.Add(task);
    return task;
  }

  public void DeleteTodoById(int id)
  {
    _todos.RemoveAll((task) => id == task.Id);
  }

  public Todo? GetTodoById(int id)
  {
    return _todos.SingleOrDefault(t => t.Id == id);
  }

  public List<Todo> GetTodos()
  {
    return _todos;
  }
}

*/