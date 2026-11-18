using Microsoft.EntityFrameworkCore;
using ReUseBairro.Api.Domain;
using ReUseBairro.Api.Dtos;
using ReUseBairro.Api.Infrastructure;

namespace ReUseBairro.Api.Services;

// ---------- Geo ----------
public interface IGeoService
{
    double DistanciaKm(double lat1, double lon1, double lat2, double lon2);
}

public class GeoService : IGeoService
{
    public double DistanciaKm(double lat1, double lon1, double lat2, double lon2) // Haversine
    {
        const double R = 6371;
        static double Rad(double g) => g * Math.PI / 180;
        var dLat = Rad(lat2 - lat1);
        var dLon = Rad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(Rad(lat1)) * Math.Cos(Rad(lat2)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }
}

// ---------- Estimativa de valor ----------
public record EstimativaItem(double ValorReais, int Creditos);

public interface IEstimadorValor
{
    EstimativaItem Estimar(CategoriaItem categoria, double pesoKg);
}

/// <summary>Estimador por tabela. Ponto de extensão: trocar por um serviço de IA que analisa a foto do item.</summary>
public class EstimadorValorSimples : IEstimadorValor
{
    private static readonly Dictionary<CategoriaItem, (double ReaisPorKg, int CreditosPorKg)> Tabela = new()
    {
        [CategoriaItem.Eletronico] = (4.0, 10),
        [CategoriaItem.Metal] = (2.5, 6),
        [CategoriaItem.Movel] = (0.8, 4),
        [CategoriaItem.SobraObra] = (0.2, 2),
    };

    public EstimativaItem Estimar(CategoriaItem categoria, double pesoKg)
    {
        var (reais, creditos) = Tabela[categoria];
        return new EstimativaItem(Math.Round(reais * pesoKg, 2), (int)Math.Round(creditos * pesoKg));
    }
}

// ---------- Usuários ----------
public interface IUsuarioService
{
    Task<UsuarioDto> CriarAsync(CriarUsuarioDto dto);
    Task<IReadOnlyList<UsuarioDto>> ListarAsync(TipoUsuario? tipo);
    Task<UsuarioDto> VerificarAsync(int id);
}

public class UsuarioService(AppDbContext db) : IUsuarioService
{
    public async Task<UsuarioDto> CriarAsync(CriarUsuarioDto dto)
    {
        var usuario = new Usuario
        {
            Nome = dto.Nome, Tipo = dto.Tipo, Latitude = dto.Latitude, Longitude = dto.Longitude,
            Carteira = new CarteiraCredito()
        };
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
        return UsuarioDto.De(usuario);
    }

    public async Task<IReadOnlyList<UsuarioDto>> ListarAsync(TipoUsuario? tipo)
    {
        var query = db.Usuarios.AsNoTracking();
        if (tipo.HasValue) query = query.Where(u => u.Tipo == tipo);
        var lista = await query.OrderBy(u => u.Nome).ToListAsync();
        return lista.Select(UsuarioDto.De).ToList();
    }

    public async Task<UsuarioDto> VerificarAsync(int id)
    {
        var usuario = await db.Usuarios.FindAsync(id)
            ?? throw new NaoEncontradoException("Usuário não encontrado.");
        if (usuario.Tipo != TipoUsuario.Coletor)
            throw new RegraNegocioException("Apenas coletores passam por verificação.");
        usuario.Verificado = true;
        await db.SaveChangesAsync();
        return UsuarioDto.De(usuario);
    }
}

// ---------- Ofertas ----------
public interface IOfertaService
{
    Task<OfertaDto> CriarAsync(CriarOfertaDto dto);
    Task<IReadOnlyList<OfertaDto>> ListarAsync(StatusOferta? status);
    Task<OfertaDto> AceitarAsync(int ofertaId, int coletorId);
    Task<OfertaDto> ConcluirAsync(int ofertaId);
}

public class OfertaService(AppDbContext db, IEstimadorValor estimador) : IOfertaService
{
    public async Task<OfertaDto> CriarAsync(CriarOfertaDto dto)
    {
        var morador = await db.Usuarios.FindAsync(dto.MoradorId)
            ?? throw new NaoEncontradoException("Morador não encontrado.");
        if (morador.Tipo != TipoUsuario.Morador)
            throw new RegraNegocioException("Apenas moradores podem criar ofertas.");

        var estimativa = estimador.Estimar(dto.Categoria, dto.PesoKg);
        var oferta = new Oferta
        {
            MoradorId = morador.Id, Descricao = dto.Descricao, Categoria = dto.Categoria,
            PesoKg = dto.PesoKg, Latitude = dto.Latitude, Longitude = dto.Longitude,
            ValorEstimado = estimativa.ValorReais, CreditosEstimados = estimativa.Creditos
        };
        db.Ofertas.Add(oferta);
        await db.SaveChangesAsync();
        return OfertaDto.De(oferta);
    }

    public async Task<IReadOnlyList<OfertaDto>> ListarAsync(StatusOferta? status)
    {
        var query = db.Ofertas.AsNoTracking();
        if (status.HasValue) query = query.Where(o => o.Status == status);
        var lista = await query.OrderByDescending(o => o.Id).ToListAsync();
        return lista.Select(OfertaDto.De).ToList();
    }

    public async Task<OfertaDto> AceitarAsync(int ofertaId, int coletorId)
    {
        var oferta = await db.Ofertas.FindAsync(ofertaId)
            ?? throw new NaoEncontradoException("Oferta não encontrada.");
        var coletor = await db.Usuarios.FindAsync(coletorId)
            ?? throw new NaoEncontradoException("Coletor não encontrado.");
        oferta.Aceitar(coletor);
        await db.SaveChangesAsync();
        return OfertaDto.De(oferta);
    }

    public async Task<OfertaDto> ConcluirAsync(int ofertaId)
    {
        var oferta = await db.Ofertas
            .Include(o => o.Morador).ThenInclude(m => m!.Carteira)
            .FirstOrDefaultAsync(o => o.Id == ofertaId)
            ?? throw new NaoEncontradoException("Oferta não encontrada.");

        oferta.Concluir();
        oferta.Morador!.Carteira!.Creditar(oferta.CreditosEstimados, $"Doação concluída (oferta #{oferta.Id})");
        await db.SaveChangesAsync();
        return OfertaDto.De(oferta);
    }
}

// ---------- Match geográfico ----------
public interface IMatchService
{
    Task<IReadOnlyList<ColetorProximoDto>> BuscarColetoresAsync(int ofertaId, double raioKm);
}

public class MatchService(AppDbContext db, IGeoService geo) : IMatchService
{
    public async Task<IReadOnlyList<ColetorProximoDto>> BuscarColetoresAsync(int ofertaId, double raioKm)
    {
        var oferta = await db.Ofertas.AsNoTracking().FirstOrDefaultAsync(o => o.Id == ofertaId)
            ?? throw new NaoEncontradoException("Oferta não encontrada.");

        var coletores = await db.Usuarios.AsNoTracking()
            .Where(u => u.Tipo == TipoUsuario.Coletor && u.Verificado)
            .ToListAsync();

        return coletores
            .Select(c => new ColetorProximoDto(c.Id, c.Nome,
                Math.Round(geo.DistanciaKm(oferta.Latitude, oferta.Longitude, c.Latitude, c.Longitude), 2)))
            .Where(x => x.DistanciaKm <= raioKm)
            .OrderBy(x => x.DistanciaKm)
            .ToList();
    }
}

// ---------- Rota de coleta (vizinho mais próximo) ----------
public interface IRotaService
{
    Task<RotaColetaDto> CalcularRotaAsync(int coletorId);
}

public class RotaService(AppDbContext db, IGeoService geo) : IRotaService
{
    public async Task<RotaColetaDto> CalcularRotaAsync(int coletorId)
    {
        var coletor = await db.Usuarios.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == coletorId && u.Tipo == TipoUsuario.Coletor)
            ?? throw new NaoEncontradoException("Coletor não encontrado.");

        var pendentes = await db.Ofertas.AsNoTracking()
            .Where(o => o.ColetorId == coletorId && o.Status == StatusOferta.Aceita)
            .ToListAsync();

        var paradas = new List<ParadaRotaDto>();
        double lat = coletor.Latitude, lon = coletor.Longitude, total = 0;

        while (pendentes.Count > 0)
        {
            var proxima = pendentes.MinBy(o => geo.DistanciaKm(lat, lon, o.Latitude, o.Longitude))!;
            var d = geo.DistanciaKm(lat, lon, proxima.Latitude, proxima.Longitude);
            total += d;
            paradas.Add(new ParadaRotaDto(proxima.Id, proxima.Descricao, proxima.Latitude, proxima.Longitude, Math.Round(d, 2)));
            lat = proxima.Latitude;
            lon = proxima.Longitude;
            pendentes.Remove(proxima);
        }
        return new RotaColetaDto(coletorId, Math.Round(total, 2), paradas);
    }
}

// ---------- PEVs ----------
public interface IPevService
{
    Task<PevDto> CriarAsync(CriarPevDto dto);
    Task<IReadOnlyList<PevDto>> ListarAsync(bool apenasCheios);
    Task<DepositoRespostaDto> DepositarAsync(int pevId, DepositoDto dto);
}

public class PevService(AppDbContext db) : IPevService
{
    private const int CreditosPorKg = 5;

    public async Task<PevDto> CriarAsync(CriarPevDto dto)
    {
        var pev = new Pev { Nome = dto.Nome, Latitude = dto.Latitude, Longitude = dto.Longitude, CapacidadeKg = dto.CapacidadeKg };
        db.Pevs.Add(pev);
        await db.SaveChangesAsync();
        return PevDto.De(pev);
    }

    public async Task<IReadOnlyList<PevDto>> ListarAsync(bool apenasCheios)
    {
        var pevs = await db.Pevs.AsNoTracking().ToListAsync();
        return pevs.Where(p => !apenasCheios || p.PrecisaColeta)
                   .OrderByDescending(p => p.PercentualOcupacao)
                   .Select(PevDto.De).ToList();
    }

    public async Task<DepositoRespostaDto> DepositarAsync(int pevId, DepositoDto dto)
    {
        var pev = await db.Pevs.FindAsync(pevId) ?? throw new NaoEncontradoException("PEV não encontrado.");
        var usuario = await db.Usuarios.Include(u => u.Carteira).FirstOrDefaultAsync(u => u.Id == dto.UsuarioId)
            ?? throw new NaoEncontradoException("Usuário não encontrado.");

        pev.Receber(dto.PesoKg);
        var creditos = (int)Math.Round(dto.PesoKg * CreditosPorKg);
        if (creditos > 0) usuario.Carteira!.Creditar(creditos, $"Depósito no PEV {pev.Nome}");
        db.DepositosPev.Add(new DepositoPev { PevId = pev.Id, UsuarioId = usuario.Id, PesoKg = dto.PesoKg });

        await db.SaveChangesAsync();
        return new DepositoRespostaDto(creditos, pev.PercentualOcupacao, pev.PrecisaColeta);
    }
}

// ---------- Carteira de créditos comunitários ----------
public interface ICarteiraService
{
    Task<CarteiraDto> ObterAsync(int usuarioId);
    Task<CarteiraDto> ResgatarAsync(int usuarioId, ResgateDto dto);
}

public class CarteiraService(AppDbContext db) : ICarteiraService
{
    public async Task<CarteiraDto> ObterAsync(int usuarioId) => ParaDto(await CarregarAsync(usuarioId));

    public async Task<CarteiraDto> ResgatarAsync(int usuarioId, ResgateDto dto)
    {
        var carteira = await CarregarAsync(usuarioId);
        carteira.Debitar(dto.Valor, $"Resgate no comércio: {dto.Comercio}");
        await db.SaveChangesAsync();
        return ParaDto(carteira);
    }

    private async Task<CarteiraCredito> CarregarAsync(int usuarioId) =>
        await db.Carteiras.Include(c => c.Transacoes).FirstOrDefaultAsync(c => c.UsuarioId == usuarioId)
        ?? throw new NaoEncontradoException("Carteira não encontrada.");

    private static CarteiraDto ParaDto(CarteiraCredito c) => new(c.UsuarioId, c.Saldo,
        c.Transacoes.OrderByDescending(t => t.Data).Select(t => new TransacaoDto(t.Tipo, t.Valor, t.Descricao, t.Data)).ToList());
}

// ---------- Indicador de impacto ----------
public interface IImpactoService
{
    Task<ImpactoDto> ObterAsync();
}

public class ImpactoService(AppDbContext db) : IImpactoService
{
    public async Task<ImpactoDto> ObterAsync()
    {
        var concluidas = db.Ofertas.Where(o => o.Status == StatusOferta.Concluida);

        var kgColetas = await concluidas.SumAsync(o => o.PesoKg);
        var qtdColetas = await concluidas.CountAsync();
        var coletoresAtivos = await concluidas.Select(o => o.ColetorId).Distinct().CountAsync();
        var renda = await concluidas.SumAsync(o => o.ValorEstimado);
        var kgPevs = await db.DepositosPev.SumAsync(d => d.PesoKg);
        var creditos = await db.Transacoes.Where(t => t.Tipo == TipoTransacao.Credito).SumAsync(t => t.Valor);

        return new ImpactoDto(
            Math.Round(kgColetas, 2), Math.Round(kgPevs, 2), Math.Round(kgColetas + kgPevs, 2),
            qtdColetas, coletoresAtivos, creditos,
            Math.Round(renda, 2), coletoresAtivos == 0 ? 0 : Math.Round(renda / coletoresAtivos, 2));
    }
}