using Microsoft.EntityFrameworkCore;
using ReUseBairro.Api.Domain;

namespace ReUseBairro.Api.Infrastructure;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<CarteiraCredito> Carteiras => Set<CarteiraCredito>();
    public DbSet<TransacaoCredito> Transacoes => Set<TransacaoCredito>();
    public DbSet<Oferta> Ofertas => Set<Oferta>();
    public DbSet<Pev> Pevs => Set<Pev>();
    public DbSet<DepositoPev> DepositosPev => Set<DepositoPev>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Usuario>(e =>
        {
            e.Property(x => x.Nome).HasMaxLength(120).IsRequired();
            e.Property(x => x.Tipo).HasConversion<string>();
            e.HasOne(x => x.Carteira).WithOne().HasForeignKey<CarteiraCredito>(c => c.UsuarioId);
        });

        b.Entity<TransacaoCredito>().Property(t => t.Tipo).HasConversion<string>();

        b.Entity<Oferta>(e =>
        {
            e.Property(o => o.Status).HasConversion<string>();
            e.Property(o => o.Categoria).HasConversion<string>();
            e.Property(o => o.Descricao).HasMaxLength(200).IsRequired();
            e.HasOne(o => o.Morador).WithMany().HasForeignKey(o => o.MoradorId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(o => o.Coletor).WithMany().HasForeignKey(o => o.ColetorId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(o => o.Status);
        });

        // Seed: PEVs de exemplo (ajuste as coordenadas para o seu bairro)
        b.Entity<Pev>().HasData(
            new Pev { Id = 1, Nome = "Mercadinho do Bairro", Latitude = -23.5500, Longitude = -46.6300, CapacidadeKg = 200 },
            new Pev { Id = 2, Nome = "Escola Municipal", Latitude = -23.5560, Longitude = -46.6400, CapacidadeKg = 300 });
    }
}