using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using ReUseBairro.Api.Domain;
using ReUseBairro.Api.Infrastructure;
using ReUseBairro.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("Padrao")));

// Injeção de dependência: interface -> implementação
builder.Services.AddSingleton<IGeoService, GeoService>();
builder.Services.AddSingleton<IEstimadorValor, EstimadorValorSimples>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IOfertaService, OfertaService>();
builder.Services.AddScoped<IMatchService, MatchService>();
builder.Services.AddScoped<IRotaService, RotaService>();
builder.Services.AddScoped<IPevService, PevService>();
builder.Services.AddScoped<ICarteiraService, CarteiraService>();
builder.Services.AddScoped<IImpactoService, ImpactoService>();

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Aplica as migrations automaticamente ao iniciar
using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();

// Tratamento centralizado de erros de negócio
app.Use(async (ctx, next) =>
{
    try { await next(); }
    catch (NaoEncontradoException ex)
    {
        ctx.Response.StatusCode = StatusCodes.Status404NotFound;
        await ctx.Response.WriteAsJsonAsync(new { erro = ex.Message });
    }
    catch (RegraNegocioException ex)
    {
        ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
        await ctx.Response.WriteAsJsonAsync(new { erro = ex.Message });
    }
});

app.UseSwagger();
app.UseSwaggerUI();
app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapControllers();
app.Run();

public partial class Program { }