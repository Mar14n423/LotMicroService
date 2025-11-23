using Lots.Application.Services;
using Lots.Application.Validators;
using Lots.Domain.Entities;
using Lots.Domain.Interfaces;
using Lots.Domain.Services;
using Lots.Infrastructure.Persistence;
using Lots.Infrastructure.Repository;

var builder = WebApplication.CreateBuilder(args);

// Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DB
builder.Services.AddSingleton<MySqlConnectionDB>();

// Repositorios
builder.Services.AddScoped<ILotRepository, LotRepository>();

// Validadores
builder.Services.AddScoped<IValidator<Lot>, LotValidator>();

// Servicios de dominio/aplicación
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
