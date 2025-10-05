using RestSQL;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddRestSQL();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var configFolder = builder.Configuration.GetSection("RestSQL").GetValue<string>("ConfigFolder")
    ?? throw new InvalidOperationException("ConfigFolder not set");

app.UseRestSQL(configFolder);

app.Run();
