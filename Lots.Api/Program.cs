using Lots.Application.Services;
using Lots.Application.Validators; 
using Lots.Domain.Entities;
using Lots.Domain.Interfaces;
using Lots.Domain.Ports;
using Lots.Domain.Services;
using Lots.Infraestructure.Messaging;
using Lots.Infraestructure.Persistence;
using Lots.Infrastructure.Data;
using Lots.Infrastructure.Gateways;
using Lots.Infrastructure.Messaging;
using Lots.Infrastructure.Persistence;
using Lots.Infrastructure.Repository;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

DatabaseConnection.Initialize(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient("MedicinesApi", client =>
{
    client.BaseAddress = new Uri("http://localhost:5149/"); 
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});
builder.Services.AddScoped<MedicineGateway>();
builder.Services.AddScoped<IValidator<Lot>, LotValidator>(); 
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<ILotRepository>(sp =>
    new LotRepository(DatabaseConnection.Instance.GetConnection(), null));
builder.Services.AddScoped<ILotService, LotService>();

builder.Services.AddSingleton<IEventPublisher, RabbitPublisher>();
builder.Services.AddSingleton<IIdempotencyStore, IdempotencyRepository>();
builder.Services.AddTransient<IOutboxRepository>(sp =>
    new OutboxRepository(DatabaseConnection.Instance.GetConnection(), null));
//builder.Services.AddHostedService<OutboxProcessor>();
//builder.Services.AddHostedService<RabbitConsumer>();


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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();