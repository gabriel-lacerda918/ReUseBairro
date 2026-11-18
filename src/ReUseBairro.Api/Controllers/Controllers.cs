using Microsoft.AspNetCore.Mvc;
using ReUseBairro.Api.Domain;
using ReUseBairro.Api.Dtos;
using ReUseBairro.Api.Services;

namespace ReUseBairro.Api.Controllers;

[ApiController, Route("api/[controller]")]
public class UsuariosController(IUsuarioService service) : ControllerBase
{
    /// <summary>Cadastra morador ou coletor (já cria a carteira de créditos).</summary>
    [HttpPost]
    public async Task<ActionResult<UsuarioDto>> Criar(CriarUsuarioDto dto)
    {
        var usuario = await service.CriarAsync(dto);
        return Created($"/api/usuarios/{usuario.Id}", usuario);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UsuarioDto>>> Listar([FromQuery] TipoUsuario? tipo) =>
        Ok(await service.ListarAsync(tipo));

    /// <summary>Marca um coletor como verificado (necessário para aceitar ofertas).</summary>
    [HttpPatch("{id:int}/verificacao")]
    public async Task<ActionResult<UsuarioDto>> Verificar(int id) => Ok(await service.VerificarAsync(id));
}

[ApiController, Route("api/[controller]")]
public class OfertasController(IOfertaService ofertas, IMatchService match) : ControllerBase
{
    /// <summary>Cria uma oferta de descarte; o valor e os créditos são estimados automaticamente.</summary>
    [HttpPost]
    public async Task<ActionResult<OfertaDto>> Criar(CriarOfertaDto dto)
    {
        var oferta = await ofertas.CriarAsync(dto);
        return Created($"/api/ofertas/{oferta.Id}", oferta);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OfertaDto>>> Listar([FromQuery] StatusOferta? status) =>
        Ok(await ofertas.ListarAsync(status));

    /// <summary>Motor de match: coletores verificados dentro do raio, do mais próximo ao mais distante.</summary>
    [HttpGet("{id:int}/coletores-proximos")]
    public async Task<ActionResult<IReadOnlyList<ColetorProximoDto>>> ColetoresProximos(int id, [FromQuery] double raioKm = 5) =>
        Ok(await match.BuscarColetoresAsync(id, raioKm));

    [HttpPost("{id:int}/aceite")]
    public async Task<ActionResult<OfertaDto>> Aceitar(int id, AceitarOfertaDto dto) =>
        Ok(await ofertas.AceitarAsync(id, dto.ColetorId));

    /// <summary>Conclui a coleta e credita o morador na carteira comunitária.</summary>
    [HttpPost("{id:int}/conclusao")]
    public async Task<ActionResult<OfertaDto>> Concluir(int id) => Ok(await ofertas.ConcluirAsync(id));
}

[ApiController, Route("api/coletores")]
public class ColetoresController(IRotaService rotas) : ControllerBase
{
    /// <summary>Rota otimizada (vizinho mais próximo) com as ofertas aceitas pelo coletor.</summary>
    [HttpGet("{id:int}/rota")]
    public async Task<ActionResult<RotaColetaDto>> Rota(int id) => Ok(await rotas.CalcularRotaAsync(id));
}

[ApiController, Route("api/[controller]")]
public class PevsController(IPevService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<PevDto>> Criar(CriarPevDto dto)
    {
        var pev = await service.CriarAsync(dto);
        return Created($"/api/pevs/{pev.Id}", pev);
    }

    /// <summary>Lista PEVs; use apenasCheios=true para ver os que precisam de coleta (≥ 80%).</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PevDto>>> Listar([FromQuery] bool apenasCheios = false) =>
        Ok(await service.ListarAsync(apenasCheios));

    /// <summary>Registra um depósito no PEV e gera créditos para o cidadão.</summary>
    [HttpPost("{id:int}/depositos")]
    public async Task<ActionResult<DepositoRespostaDto>> Depositar(int id, DepositoDto dto) =>
        Ok(await service.DepositarAsync(id, dto));
}

[ApiController, Route("api/[controller]")]
public class CarteirasController(ICarteiraService service) : ControllerBase
{
    [HttpGet("{usuarioId:int}")]
    public async Task<ActionResult<CarteiraDto>> Obter(int usuarioId) => Ok(await service.ObterAsync(usuarioId));

    /// <summary>Troca créditos por benefícios no comércio local.</summary>
    [HttpPost("{usuarioId:int}/resgates")]
    public async Task<ActionResult<CarteiraDto>> Resgatar(int usuarioId, ResgateDto dto) =>
        Ok(await service.ResgatarAsync(usuarioId, dto));
}

[ApiController, Route("api/[controller]")]
public class ImpactoController(IImpactoService service) : ControllerBase
{
    /// <summary>Indicadores: kg desviados e renda estimada dos coletores.</summary>
    [HttpGet]
    public async Task<ActionResult<ImpactoDto>> Obter() => Ok(await service.ObterAsync());
}