using AutoMapper;
using Core.Reporte;
using FibAttendanceApi;
using FibAttendanceApi.Core.IclockTransaction;
using FibAttendanceApi.Core.Reporte.AttendanceMatrix;
using FibAttendanceApi.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbcontext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();

// Servicios específicos de la aplicación
builder.Services.AddScoped<IIclockTransactionService, IclockTtransactionService>();
builder.Services.AddScoped<IAttendanceAnalysisService, AttendanceAnalysisService>();
builder.Services.AddAttendanceMatrixServices();

// AutoMapper
builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<MappingProfile>();
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

app.UseAuthorization();
app.MapControllers();

app.Run();