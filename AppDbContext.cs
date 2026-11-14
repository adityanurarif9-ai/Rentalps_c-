using Microsoft.EntityFrameworkCore;

namespace PSSeleraku;

public sealed class AppDbContext : DbContext
{
    public DbSet<UnitKonsol> UnitKonsol => Set<UnitKonsol>();
    public DbSet<Produk> Produk => Set<Produk>();
    public DbSet<Transaksi> Transaksi => Set<Transaksi>();

    public void SiapkanDatabase()
    {
        if (Database.EnsureCreated())
            return;

        // Database versi pertama dibuat dengan EnsureCreated dan belum memiliki status arsip.
        using var perubahan = Database.BeginTransaction();
        var adaKolomArsip = Database.SqlQueryRaw<int>(
            "SELECT COUNT(*) AS Value FROM pragma_table_info('UnitKonsol') WHERE name = 'Diarsipkan'").Single() > 0;
        if (!adaKolomArsip)
        {
            Database.ExecuteSqlRaw(
                "ALTER TABLE UnitKonsol ADD COLUMN Diarsipkan INTEGER NOT NULL DEFAULT 0");
        }
        perubahan.Commit();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseSqlite("Data Source=ps_seleraku.db");
    }

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Transaksi>().HasIndex(t => new { t.UnitKonsolId, t.JadwalSelesai });
        model.Entity<Produk>().Property(p => p.Stok).IsConcurrencyToken();
        model.Entity<Produk>().Property(p => p.Harga).IsConcurrencyToken();
        model.Entity<Produk>().Property(p => p.Nama).IsConcurrencyToken();
        model.Entity<Produk>().Property(p => p.Kategori).IsConcurrencyToken();
        model.Entity<UnitKonsol>().Property(u => u.Nama).IsConcurrencyToken();
        model.Entity<UnitKonsol>().Property(u => u.Jenis).IsConcurrencyToken();
        model.Entity<UnitKonsol>().Property(u => u.TarifPerJam).IsConcurrencyToken();
        model.Entity<UnitKonsol>().Property(u => u.Diarsipkan).IsConcurrencyToken();

        model.Entity<UnitKonsol>().HasData(
            new UnitKonsol { Id = 1, Nama = "PS3-01", Jenis = "PlayStation 3", TarifPerJam = 6000 },
            new UnitKonsol { Id = 2, Nama = "PS4-01", Jenis = "PlayStation 4", TarifPerJam = 10000 },
            new UnitKonsol { Id = 3, Nama = "PS5-01", Jenis = "PlayStation 5", TarifPerJam = 15000 },
            new UnitKonsol { Id = 4, Nama = "NS-01", Jenis = "Nintendo Switch", TarifPerJam = 12000 });

        model.Entity<Produk>().HasData(
            new Produk { Id = 1, Nama = "Mie Goreng", Kategori = Kategori.Makanan, Harga = 10000, Stok = 20 },
            new Produk { Id = 2, Nama = "Nasi Goreng", Kategori = Kategori.Makanan, Harga = 15000, Stok = 15 },
            new Produk { Id = 3, Nama = "Es Teh", Kategori = Kategori.Minuman, Harga = 4000, Stok = 30 },
            new Produk { Id = 4, Nama = "Air Mineral", Kategori = Kategori.Minuman, Harga = 5000, Stok = 25 },
            new Produk { Id = 5, Nama = "Kentang Goreng", Kategori = Kategori.Camilan, Harga = 8000, Stok = 20 },
            new Produk { Id = 6, Nama = "Keripik", Kategori = Kategori.Camilan, Harga = 5000, Stok = 20 });
    }
}
