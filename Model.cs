namespace PSSeleraku;

public enum Kategori
{
    Makanan = 1,
    Minuman = 2,
    Camilan = 3
}

public sealed class UnitKonsol
{
    public int Id { get; set; }
    public string Nama { get; set; } = "";
    public string Jenis { get; set; } = "";
    public int TarifPerJam { get; set; }
    public bool Diarsipkan { get; set; }
}

public sealed class Produk
{
    public int Id { get; set; }
    public string Nama { get; set; } = "";
    public Kategori Kategori { get; set; }
    public int Harga { get; set; }
    public int Stok { get; set; }
}

public sealed class Transaksi
{
    public int Id { get; set; }
    public DateTime Waktu { get; set; }
    public int? UnitKonsolId { get; set; }
    public UnitKonsol? UnitKonsol { get; set; }
    public string? Pelanggan { get; set; }
    public DateTime? JadwalSelesai { get; set; }
    public DateTime? SelesaiLebihAwal { get; set; }
    public long Total { get; set; }
    public long Tunai { get; set; }
    public long Kembalian => Tunai - Total;
    public List<DetailTransaksi> Detail { get; set; } = [];
}

public sealed class DetailTransaksi
{
    public int Id { get; set; }
    public string Nama { get; set; } = "";
    public string Satuan { get; set; } = "";
    public int Jumlah { get; set; }
    public int HargaSatuan { get; set; }
    public long Subtotal => (long)Jumlah * HargaSatuan;
}
