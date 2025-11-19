using Lots.Application.Services;
using Lots.Domain.Interfaces;
using Lots.Domain.Services;
using Lots.Infrastructure.Persistence;
using Lots.Infrastructure.Repository;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<MySqlConnectionDB>();

// Repositorios
builder.Services.AddScoped<ILotRepository, LotRepository>();

// Servicios
builder.Services.AddScoped<ILotService, LotService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();
app.Run();
