namespace ReUseBairro.Api.Domain;

public enum TipoUsuario { Morador, Coletor }
public enum CategoriaItem { Eletronico, Movel, SobraObra, Metal }
public enum StatusOferta { Aberta, Aceita, Concluida, Cancelada }
public enum TipoTransacao { Credito, Debito }

public class RegraNegocioException(string mensagem) : Exception(mensagem);
public class NaoEncontradoException(string mensagem) : Exception(mensagem);

public class Usuario
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public TipoUsuario Tipo { get; set; }
    public bool Verificado { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public CarteiraCredito? Carteira { get; set; }
}

public class CarteiraCredito
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public int Saldo { get; private set; }
    public List<TransacaoCredito> Transacoes { get; } = new();

    public void Creditar(int valor, string descricao)
    {
        if (valor <= 0) throw new RegraNegocioException("O valor deve ser positivo.");
        Saldo += valor;
        Transacoes.Add(new TransacaoCredito { Tipo = TipoTransacao.Credito, Valor = valor, Descricao = descricao });
    }

    public void Debitar(int valor, string descricao)
    {
        if (valor <= 0) throw new RegraNegocioException("O valor deve ser positivo.");
        if (valor > Saldo) throw new RegraNegocioException("Saldo insuficiente.");
        Saldo -= valor;
        Transacoes.Add(new TransacaoCredito { Tipo = TipoTransacao.Debito, Valor = valor, Descricao = descricao });
    }
}

public class TransacaoCredito
{
    public int Id { get; set; }
    public int CarteiraCreditoId { get; set; }
    public TipoTransacao Tipo { get; set; }
    public int Valor { get; set; }
    public string Descricao { get; set; } = "";
    public DateTime Data { get; set; } = DateTime.UtcNow;
}

public class Oferta
{
    public int Id { get; set; }
    public int MoradorId { get; set; }
    public Usuario? Morador { get; set; }
    public int? ColetorId { get; set; }
    public Usuario? Coletor { get; set; }
    public string Descricao { get; set; } = "";
    public CategoriaItem Categoria { get; set; }
    public double PesoKg { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double ValorEstimado { get; set; }
    public int CreditosEstimados { get; set; }
    public StatusOferta Status { get; set; } = StatusOferta.Aberta;
    public DateTime CriadaEm { get; set; } = DateTime.UtcNow;
    public DateTime? ConcluidaEm { get; set; }

    public void Aceitar(Usuario coletor)
    {
        if (Status != StatusOferta.Aberta)
            throw new RegraNegocioException("A oferta não está aberta.");
        if (coletor.Tipo != TipoUsuario.Coletor || !coletor.Verificado)
            throw new RegraNegocioException("Apenas coletores verificados podem aceitar ofertas.");
        ColetorId = coletor.Id;
        Status = StatusOferta.Aceita;
    }

    public void Concluir()
    {
        if (Status != StatusOferta.Aceita)
            throw new RegraNegocioException("Só é possível concluir ofertas aceitas.");
        Status = StatusOferta.Concluida;
        ConcluidaEm = DateTime.UtcNow;
    }
}

public class Pev
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double CapacidadeKg { get; set; }
    public double OcupacaoKg { get; set; }

    public double PercentualOcupacao =>
        CapacidadeKg <= 0 ? 0 : Math.Round(OcupacaoKg / CapacidadeKg * 100, 1);
    public bool PrecisaColeta => PercentualOcupacao >= 80;

    public void Receber(double kg)
    {
        if (kg <= 0) throw new RegraNegocioException("O peso deve ser positivo.");
        if (OcupacaoKg + kg > CapacidadeKg)
            throw new RegraNegocioException("O PEV não tem capacidade para este volume.");
        OcupacaoKg += kg;
    }
}

public class DepositoPev
{
    public int Id { get; set; }
    public int PevId { get; set; }
    public int UsuarioId { get; set; }
    public double PesoKg { get; set; }
    public DateTime Data { get; set; } = DateTime.UtcNow;
}