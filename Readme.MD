# ReUseBairro: Logística Reversa de Resíduos Eletrônicos

# FIAP - Global Solution - Engenharia de Software

## Gabriel Lacerda - RM: 556714 - 3ESPW

Projeto CitizenTech. Backend em C#/.NET 8 que conecta moradores com pequenos
volumes de e-waste/móveis a catadores autônomos verificados, com carteira de
créditos comunitários e rotas de coleta.

## Ficha da CitizenTech
- **Nome:** ReUseBairro
- **Cidadão beneficiado:** Moradores, catadores autônomos e microempreendedores de reparo/reforma.
- **Problema:** O descarte irregular de eletrônicos, móveis e sobras de obra entope bueiros e degrada o bairro porque o cidadão não sabe como destinar pequenos volumes e os catadores não sabem onde há material.
- **Como funciona:** PEVs em comércios/escolas + app que conecta ofertas a coletores por proximidade, com estimativa de valor e créditos comunitários trocáveis no comércio local.
- **Papel de cada parte:** Sensores nos PEVs (simulados via API de depósito) · IA de estimativa (interface `IEstimadorValor`, implementação por tabela, extensível a visão computacional) · Backend C# (match, rotas, carteira) · App mobile (consumidor futuro desta API).
- **Repositório:** [URL DO GITHUB]
- **Indicador de impacto:** kg desviados e renda estimada dos coletores (`GET /api/impacto`).
- **ODS:** 11, 12 e 8.

## Arquitetura
Controllers → Services (interfaces + DI) → AppDbContext (EF Core/SQLite) → Domain (regras nas entidades).

## Como executar
Pré-requisitos: .NET 8 SDK.
```bash
git clone [URL]
cd ReUseBairro
dotnet restore
dotnet test
dotnet run --project src/ReUseBairro.Api
```
Acesse `http://localhost:<porta>/swagger`. O banco SQLite e as migrations são aplicados automaticamente.

## Endpoints principais
| Método | Rota | Descrição |
|---|---|---|
| POST/GET | /api/usuarios | Cadastro/listagem |
| PATCH | /api/usuarios/{id}/verificacao | Verifica coletor |
| POST/GET | /api/ofertas | Cria/lista ofertas (estimativa automática) |
| GET | /api/ofertas/{id}/coletores-proximos | Motor de match geográfico |
| POST | /api/ofertas/{id}/aceite · /conclusao | Fluxo da coleta |
| GET | /api/coletores/{id}/rota | Rota otimizada |
| POST/GET | /api/pevs · /api/pevs/{id}/depositos | PEVs |
| GET/POST | /api/carteiras/{usuarioId} · /resgates | Créditos comunitários |
| GET | /api/impacto | Indicadores |

## Conceitos de C# aplicados
POO · Interfaces · Injeção de Dependência · EF Core (Migrations, seed) · LINQ · API REST · async/await · testes xUnit.

## Evidências
Veja `docs/evidencias/` (Swagger, testes, banco, fluxo completo).