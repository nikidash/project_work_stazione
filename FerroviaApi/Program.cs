using Microsoft.EntityFrameworkCore;
using FerroviaApi.Data; // Namespace corretto: FerroviaApi

var builder = WebApplication.CreateBuilder(args);

// 1. Aggiunge il DbContext
var connectionString = builder.Configuration.GetConnectionString("FerroviaDb")
    ?? throw new InvalidOperationException("Connection string 'FerroviaDb' not found.");

builder.Services.AddDbContext<FerroviaApiDbContext>(options =>
    options.UseNpgsql(connectionString));

// Aggiunge i servizi Web API e Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

 //app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();