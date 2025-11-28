using System.Text;
using Lots.Application.Services;
using Lots.Application.Validators;
using Lots.Domain.Entities;
using Lots.Domain.Interfaces;
using Lots.Domain.Services;
using Lots.Infrastructure.Persistence;
using Lots.Infrastructure.Repository;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// =======================
//  Controllers + Swagger
// =======================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// =======================
//  DB
// =======================
builder.Services.AddSingleton<MySqlConnectionDB>();

// =======================
//  Repositorios
// =======================
builder.Services.AddScoped<ILotRepository, LotRepository>();

// =======================
//  Validadores
// =======================
builder.Services.AddScoped<IValidator<Lot>, LotValidator>();

// =======================
//  Servicios de dominio/aplicación
// =======================
builder.Services.AddScoped<ILotService, LotService>();

// =======================
//  JWT AUTH
// =======================
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key no configurado");
var jwtIssuer = jwtSection["Issuer"];
var jwtAudience = jwtSection["Audience"];

var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();
//  Pipeline HTTP
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();   
app.UseAuthorization();    

app.MapControllers();

app.Run();
