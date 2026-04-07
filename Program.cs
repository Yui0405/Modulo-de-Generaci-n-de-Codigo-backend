using GeneracionApi.Config;
using GeneracionApi.Domain;
using GeneracionApi.Repositories;
using GeneracionApi.Services;
using GeneracionApi.Services.Pipeline;
using GeneracionApi.Services.Pipeline.Filters;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// ──────────────────────────────────────────────
// Configuración de MongoDB
// ──────────────────────────────────────────────
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDbSettings"));

// MongoDbSettings como singleton para inyección directa en repositorios
builder.Services.AddSingleton<MongoDbSettings>(sp =>
    builder.Configuration.GetSection("MongoDbSettings").Get<MongoDbSettings>()!);

builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var settings = builder.Configuration
        .GetSection("MongoDbSettings")
        .Get<MongoDbSettings>()!;
    return new MongoClient(settings.ConnectionString);
});

// ──────────────────────────────────────────────
// Controllers
// ──────────────────────────────────────────────
builder.Services.AddControllers();

// ──────────────────────────────────────────────
// CORS — permitir al frontend (Vite en :5173)
// ──────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// ──────────────────────────────────────────────
// Repositories (MongoDB)
// ──────────────────────────────────────────────
builder.Services.AddScoped<IRepositorio<Generacion>, GeneracionRepository>();
builder.Services.AddScoped<IRepositorio<Diagrama>, DiagramaRepository>();
builder.Services.AddScoped<IRepositorio<ConfigGeneracion>, ConfigGeneracionRepository>();
builder.Services.AddScoped<IRepositorio<ArtefactoGenerado>, ArtefactoRepository>();
builder.Services.AddScoped<IRepositorio<TraceLog>, LogRepository>();

// ──────────────────────────────────────────────
// Pipeline Filters
// ──────────────────────────────────────────────
builder.Services.AddScoped<IFiltroGeneracion, ValidacionMetamodeloFilter>();
builder.Services.AddScoped<IFiltroGeneracion, TransformacionFilter>();
builder.Services.AddScoped<IFiltroGeneracion, AplicarConfiguracionFilter>();
builder.Services.AddScoped<IFiltroGeneracion, GeneracionArtefactosFilter>();
builder.Services.AddScoped<IFiltroGeneracion, RegistroAuditoriaFilter>();

// Pipeline orchestrator
builder.Services.AddScoped<IPipeline, GeneracionPipeline>();

// ──────────────────────────────────────────────
// Services
// ──────────────────────────────────────────────
builder.Services.AddScoped<ITrazabilidadService, TrazabilidadService>();
builder.Services.AddScoped<IGeneracionService, GeneracionService>();
builder.Services.AddScoped<IIntegracionService, IntegracionService>();

var app = builder.Build();

// ──────────────────────────────────────────────
// Pipeline HTTP
// ──────────────────────────────────────────────
app.UseCors("Frontend");
app.MapControllers();

app.Run();
