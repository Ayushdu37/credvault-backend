using System.Text;
using CreditManagement.API.Configuration;
using CreditManagement.API.Middleware;
using CreditManagement.Application.Interfaces;
using CreditManagement.Application.Services;
using CreditManagement.Domain.Interfaces;
using CreditManagement.Infrastructure.Persistence;
using CreditManagement.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog — structured logging to console + rolling file ──
builder.Host.UseSerilog((context, config) =>
{
    config.ReadFrom.Configuration(context.Configuration);
});

// ── Database — register DbContext with SQL Server connection ──
builder.Services.AddDbContext<CreditManagementDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("CreditManagementDb"));
});

// ── Dependency Injection — register repositories and services ──
builder.Services.AddScoped<ICardRepository, CardRepository>();
builder.Services.AddScoped<IBillRepository, BillRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<ICardService, CardService>();
builder.Services.AddScoped<IBillService, BillService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

// ── JWT Authentication — VALIDATE tokens issued by Identity Service ──
// Uses the same SecretKey, Issuer, and Audience so tokens work across both services
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()!;

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
    };
});

builder.Services.AddAuthorization();

// ── Controllers ──
builder.Services.AddControllers();

// ── Swagger — API docs with JWT Bearer support (Swashbuckle v10 syntax) ──
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CredVault Credit Management Service",
        Version = "v1",
        Description = "Cards, Bills, and Payment Management"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Enter the JWT token from Identity Service"
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

var app = builder.Build();

// ── Middleware Pipeline ──

app.UseGlobalExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

Log.Information("CredVault Credit Management Service started on {Urls}", app.Urls);

app.Run();
