using UserManagementAPI.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<AuthenticationMiddleware>();
app.UseMiddleware<LoggingMiddleware>();
app.MapControllers();

app.MapGet("/", () => "Root page!");

app.MapGet("/request-count", (HttpContext context) =>
{
    var path = context.Request.Query["path"];
    var count = LoggingMiddleware.GetRequestCount(string.IsNullOrEmpty(path) ? string.Empty : path.ToString());
    return Results.Ok(new { Path = path, Count = count });
});

app.Run();