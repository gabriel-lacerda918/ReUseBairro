using System.ComponentModel.DataAnnotations;
using ReUseBairro.Api.Domain;

namespace ReUseBairro.Api.Dtos;

public record CriarUsuarioDto
{
    [Required, StringLength(120)] public string Nome { get; init; } = "";
    public TipoUsuario Tipo { get; init; }
    [Range(-90, 90)] public double Latitude { get; init; }
    [Range(-180, 180)] public double Longitude { get; init; }
}

public record UsuarioDto(int Id, string Nome, TipoUsuario Tipo, bool Verificado, double Latitude, double Longitude)
{
    public static UsuarioDto De(Usuario u) => new(u.Id, u.Nome, u.Tipo, u.Verificado, u.Latitude, u.Longitude);
}

public record CriarOfertaDto
{
    [Range(1, int.MaxValue)] public int MoradorId { get; init; }
    [Required, StringLength(200)] public string Descricao { get; init; } = "";
    public CategoriaItem Categoria { get; init; }
    [Range(0.1, 1000)] public double PesoKg { get; init; }
    [Range(-90, 90)] public double Latitude { get; init; }
    [Range(-180, 180)] public double Longitude { get; init; }
}

public record OfertaDto(int Id, int MoradorId, int? ColetorId, string Descricao, CategoriaItem Categoria,
    double PesoKg, double ValorEstimado, int CreditosEstimados, StatusOferta Status, DateTime CriadaEm)
{
    public static OfertaDto De(Oferta o) => new(o.Id, o.MoradorId, o.ColetorId, o.Descricao, o.Categoria,
        o.PesoKg, o.ValorEstimado, o.CreditosEstimados, o.Status, o.CriadaEm);
}

public record AceitarOfertaDto { [Range(1, int.MaxValue)] public int ColetorId { get; init; } }

public record ColetorProximoDto(int ColetorId, string Nome, double DistanciaKm);
public record ParadaRotaDto(int OfertaId, string Descricao, double Latitude, double Longitude, double DistanciaDoPontoAnteriorKm);
public record RotaColetaDto(int ColetorId, double DistanciaTotalKm, IReadOnlyList<ParadaRotaDto> Paradas);

public record CriarPevDto
{
    [Required, StringLength(120)] public string Nome { get; init; } = "";
    [Range(-90, 90)] public double Latitude { get; init; }
    [Range(-180, 180)] public double Longitude { get; init; }
    [Range(1, 100000)] public double CapacidadeKg { get; init; }
}

public record PevDto(int Id, string Nome, double Latitude, double Longitude, double CapacidadeKg,
    double OcupacaoKg, double PercentualOcupacao, bool PrecisaColeta)
{
    public static PevDto De(Pev p) => new(p.Id, p.Nome, p.Latitude, p.Longitude, p.CapacidadeKg,
        p.OcupacaoKg, p.PercentualOcupacao, p.PrecisaColeta);
}

public record DepositoDto
{
    [Range(1, int.MaxValue)] public int UsuarioId { get; init; }
    [Range(0.1, 1000)] public double PesoKg { get; init; }
}

public record DepositoRespostaDto(int CreditosGerados, double PercentualOcupacao, bool PrecisaColeta);

public record ResgateDto
{
    [Range(1, int.MaxValue)] public int Valor { get; init; }
    [Required, StringLength(120)] public string Comercio { get; init; } = "";
}

public record TransacaoDto(TipoTransacao Tipo, int Valor, string Descricao, DateTime Data);
public record CarteiraDto(int UsuarioId, int Saldo, IReadOnlyList<TransacaoDto> Transacoes);

public record ImpactoDto(double KgDesviadosPorColetas, double KgEntreguesEmPevs, double KgTotalDesviado,
    int ColetasConcluidas, int ColetoresAtivos, int CreditosEmitidos,
    double RendaEstimadaColetoresReais, double RendaMediaPorColetorReais);