using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ReUseBairro.Api.Domain;
using ReUseBairro.Api.Dtos;
using ReUseBairro.Api.Infrastructure;
using ReUseBairro.Api.Services;

namespace ReUseBairro.Tests;

public static class TestDb
{
    public static AppDbContext Criar()
    {
        var conexao = new SqliteConnection("DataSource=:memory:");
        conexao.Open();
        var opcoes = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(conexao).Options;
        var db = new AppDbContext(opcoes);
        db.Database.EnsureCreated();
        return db;
    }

    public static Usuario Morador() =>
        new() { Nome = "Ana", Tipo = TipoUsuario.Morador, Carteira = new CarteiraCredito() };

    public static Usuario Coletor(bool verificado = true, double lat = 0, double lon = 0) =>
        new() { Nome = "Zé", Tipo = TipoUsuario.Coletor, Verificado = verificado, Latitude = lat, Longitude = lon, Carteira = new CarteiraCredito() };
}

public class GeoServiceTests
{
    private readonly GeoService _geo = new();

    [Fact]
    public void Distancia_MesmoPonto_EhZero() =>
        Assert.Equal(0, _geo.DistanciaKm(-23.55, -46.63, -23.55, -46.63), 3);

    [Fact]
    public void Distancia_UmGrauDeLatitude_EhAproximadamente111Km() =>
        Assert.InRange(_geo.DistanciaKm(0, 0, 1, 0), 111.0, 111.4);
}

public class DominioTests
{
    [Fact]
    public void Carteira_DebitarSemSaldo_Lanca()
    {
        var carteira = new CarteiraCredito();
        carteira.Creditar(10, "teste");
        Assert.Throws<RegraNegocioException>(() => carteira.Debitar(11, "resgate"));
        Assert.Equal(10, carteira.Saldo);
    }

    [Fact]
    public void Pev_AcimaDaCapacidade_Lanca()
    {
        var pev = new Pev { CapacidadeKg = 100 };
        pev.Receber(90);
        Assert.Throws<RegraNegocioException>(() => pev.Receber(20));
        Assert.True(pev.PrecisaColeta);
    }

    [Fact]
    public void Oferta_AceitarComColetorNaoVerificado_Lanca()
    {
        var oferta = new Oferta();
        Assert.Throws<RegraNegocioException>(() => oferta.Aceitar(TestDb.Coletor(verificado: false)));
    }
}

public class ServicosTests
{
    [Fact]
    public async Task Fluxo_CriarAceitarConcluir_CreditaMorador()
    {
        var db = TestDb.Criar();
        var morador = TestDb.Morador();
        var coletor = TestDb.Coletor();
        db.Usuarios.AddRange(morador, coletor);
        await db.SaveChangesAsync();

        var service = new OfertaService(db, new EstimadorValorSimples());
        var oferta = await service.CriarAsync(new CriarOfertaDto
        {
            MoradorId = morador.Id, Descricao = "Monitor antigo",
            Categoria = CategoriaItem.Eletronico, PesoKg = 5, Latitude = -23.55, Longitude = -46.63
        });
        await service.AceitarAsync(oferta.Id, coletor.Id);
        await service.ConcluirAsync(oferta.Id);

        var carteira = await db.Carteiras.FirstAsync(c => c.UsuarioId == morador.Id);
        Assert.Equal(50, carteira.Saldo); // 5 kg x 10 créditos/kg
    }

    [Fact]
    public async Task Match_RetornaApenasColetoresNoRaio_OrdenadosPorDistancia()
    {
        var db = TestDb.Criar();
        var morador = TestDb.Morador();
        var perto = TestDb.Coletor(lat: 0.01);
        var medio = TestDb.Coletor(lat: 0.03);
        var longe = TestDb.Coletor(lat: 1.0);
        db.Usuarios.AddRange(morador, perto, medio, longe);
        await db.SaveChangesAsync();
        db.Ofertas.Add(new Oferta { MoradorId = morador.Id, Descricao = "Sofá", Latitude = 0, Longitude = 0 });
        await db.SaveChangesAsync();

        var resultado = await new MatchService(db, new GeoService()).BuscarColetoresAsync(1, raioKm: 5);

        Assert.Equal(2, resultado.Count);
        Assert.Equal(perto.Id, resultado[0].ColetorId);
    }

    [Fact]
    public async Task Rota_VisitaPrimeiroOPontoMaisProximo()
    {
        var db = TestDb.Criar();
        var morador = TestDb.Morador();
        var coletor = TestDb.Coletor();
        db.Usuarios.AddRange(morador, coletor);
        await db.SaveChangesAsync();
        db.Ofertas.AddRange(
            new Oferta { MoradorId = morador.Id, ColetorId = coletor.Id, Status = StatusOferta.Aceita, Descricao = "longe", Latitude = 0.10 },
            new Oferta { MoradorId = morador.Id, ColetorId = coletor.Id, Status = StatusOferta.Aceita, Descricao = "perto", Latitude = 0.01 });
        await db.SaveChangesAsync();

        var rota = await new RotaService(db, new GeoService()).CalcularRotaAsync(coletor.Id);

        Assert.Equal("perto", rota.Paradas[0].Descricao);
        Assert.Equal(2, rota.Paradas.Count);
    }
}