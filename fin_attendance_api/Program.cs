using AutoMapper;
using Core.Reporte;
using FibAttendanceApi;
using FibAttendanceApi.Core.IclockTransaction;
using FibAttendanceApi.Core.Reporte.AttendanceMatrix;
using FibAttendanceApi.Core.Reporte.HorasExtras;
using FibAttendanceApi.Core.OhldService;
using FibAttendanceApi.Data;
using Microsoft.EntityFrameworkCore;
using FibAttendanceApi.Core.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbcontext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlServerOptionsAction: sqlOptions =>
        {
            sqlOptions.UseCompatibilityLevel(110);
        }
    ));

// En tu archivo Program.cs o donde configures los servicios
// Registra el interceptor como servicio primero
builder.Services.AddSingleton<TriggerTableInterceptor>();

// Luego úsalo en el DbContext
builder.Services.AddDbContext<ApplicationDbcontext>((serviceProvider, options) =>
{
    var interceptor = serviceProvider.GetRequiredService<TriggerTableInterceptor>();
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
           .AddInterceptors(interceptor);
});

builder.Services.AddControllers();

// Servicios espec�ficos de la aplicaci�n
builder.Services.AddScoped<IIclockTransactionService, IclockTtransactionService>();
builder.Services.AddScoped<IAttendanceAnalysisService, AttendanceAnalysisService>();
builder.Services.AddAttendanceMatrixServices();
builder.Services.AddExtraHoursReportServices();

// Servicio de sincronización OHLD
builder.Services.AddHttpClient<OhldSyncService>();
builder.Services.AddScoped<OhldSyncService>();

// AutoMapper
builder.Services.AddAutoMapper(typeof(Program).Assembly);

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(option =>
{
    option.SwaggerDoc("v1", new OpenApiInfo { Title = "Fib Attendance API", Version = "v1" });
    option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter a valid token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });
    option.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type=ReferenceType.SecurityScheme,
                    Id="Bearer"
                }
            },
            new string[]{}
        }
    });
});

// CORS configurado para IIS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader()
                   .WithExposedHeaders("x-pagination");
        });
});

// Security services
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<ITokenService, TokenService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });


var app = builder.Build();

// Configure the HTTP request pipeline.
// Habilitar Swagger en ambos ambientes para IIS
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

// No usar HTTPS redirection para IIS con HTTP
// app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();