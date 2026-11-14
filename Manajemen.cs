using Microsoft.EntityFrameworkCore;

namespace PSSeleraku;

internal static partial class Program
{
    private static void KelolaProduk(AppDbContext db)
    {
        while (true)
        {
            Judul("MANAJEMEN PRODUK & STOK");
            TampilkanProduk(db.Produk.AsNoTracking().OrderBy(p => p.Kategori).ThenBy(p => p.Id).ToList());
            Console.WriteLine("\n1. Tambah produk");
            Console.WriteLine("2. Edit produk atau stok");
            Console.WriteLine("3. Hapus produk");
            Console.WriteLine("0. Kembali");
            var pilihan = Baca("Pilih");
            switch (pilihan)
            {
                case null or "0": return;
                case "1": SimpanProduk(db, null); break;
                case "2" or "3": PilihProduk(db, hapus: pilihan == "3"); break;
                default: Jeda("Pilihan tidak valid."); break;
            }
            db.ChangeTracker.Clear();
        }
    }

    private static void PilihProduk(AppDbContext db, bool hapus)
    {
        var id = BacaAngka("ID produk (0 = kembali)");
        if (id is null or 0)
            return;
        var produk = db.Produk.Find(id.Value);
        if (produk is null)
        {
            Jeda("Produk tidak ditemukan.");
            return;
        }

        if (!hapus)
        {
            SimpanProduk(db, produk);
            return;
        }

        if (!Konfirmasi($"Hapus {produk.Nama} dari menu?"))
            return;

        db.Produk.Remove(produk);
        db.SaveChanges();
        Jeda("Produk dihapus. Riwayat penjualannya masih tersedia.");
    }

    private static void SimpanProduk(AppDbContext db, Produk? produk)
    {
        var nama = BacaNama("Nama produk", 60, produk?.Nama);
        if (nama is null)
            return;
        int? kategoriAwal = produk is null ? null : (int)produk.Kategori;
        var kategori = BacaAngka("Kategori (1 Makanan, 2 Minuman, 3 Camilan)", 1, 3, kategoriAwal);
        if (kategori is null)
            return;
        var harga = BacaAngka("Harga dalam rupiah", minimum: 1, nilaiAwal: produk?.Harga);
        if (harga is null)
            return;
        var stok = BacaAngka("Jumlah stok", nilaiAwal: produk?.Stok);
        if (stok is null)
            return;

        if (produk is null)
        {
            produk = new Produk();
            db.Produk.Add(produk);
        }
        produk.Nama = nama;
        produk.Kategori = (Kategori)kategori.Value;
        produk.Harga = harga.Value;
        produk.Stok = stok.Value;
        db.SaveChanges();
        Jeda($"Produk #{produk.Id} tersimpan.");
    }

    private static void KelolaUnit(AppDbContext db)
    {
        while (true)
        {
            Judul("MANAJEMEN UNIT KONSOL");
            var dipakai = RentalAktif(db, DateTime.Now).Select(t => t.UnitKonsolId).ToHashSet();
            var unit = db.UnitKonsol.AsNoTracking().OrderBy(u => u.Id).ToList();
            foreach (var konsol in unit)
            {
                string status = konsol.Diarsipkan ? "Arsip" : dipakai.Contains(konsol.Id) ? "Dipakai" : "Tersedia";
                Console.WriteLine($"{konsol.Id,2}. {konsol.Nama} | {konsol.Jenis} | {Rupiah(konsol.TarifPerJam)}/jam | {status}");
            }
            Console.WriteLine("\n1. Tambah unit");
            Console.WriteLine("2. Edit kode, jenis, atau tarif");
            Console.WriteLine("3. Arsipkan unit");
            Console.WriteLine("4. Aktifkan kembali unit");
            Console.WriteLine("0. Kembali");

            var pilihan = Baca("Pilih");
            switch (pilihan)
            {
                case null or "0": return;
                case "1": SimpanUnit(db, null); break;
                case "2" or "3" or "4": PilihUnit(db, pilihan); break;
                default: Jeda("Pilihan tidak valid."); break;
            }
            db.ChangeTracker.Clear();
        }
    }

    private static void PilihUnit(AppDbContext db, string pilihan)
    {
        var id = BacaAngka("ID unit (0 = kembali)");
        if (id is null or 0)
            return;
        var unit = db.UnitKonsol.Find(id.Value);
        if (unit is null)
        {
            Jeda("Unit tidak ditemukan.");
            return;
        }

        if (pilihan == "2")
        {
            SimpanUnit(db, unit);
            return;
        }

        bool arsipkan = pilihan == "3";
        if (unit.Diarsipkan == arsipkan)
        {
            Jeda(arsipkan ? "Unit sudah diarsipkan." : "Unit sudah aktif.");
            return;
        }
        if (!Konfirmasi(arsipkan ? $"Arsipkan {unit.Nama}?" : $"Aktifkan kembali {unit.Nama}?"))
            return;

        using var proses = db.Database.BeginTransaction();
        if (arsipkan && RentalAktif(db, DateTime.Now).Any(t => t.UnitKonsolId == unit.Id))
        {
            proses.Rollback();
            Jeda("Unit masih dipakai. Akhiri sesi atau tunggu hingga selesai sebelum mengarsipkan.");
            return;
        }
        unit.Diarsipkan = arsipkan;
        db.SaveChanges();
        proses.Commit();
        Jeda(arsipkan ? "Unit diarsipkan dari daftar rental." : "Unit kembali tersedia untuk rental.");
    }

    private static void SimpanUnit(AppDbContext db, UnitKonsol? unit)
    {
        var nama = BacaNama("Kode unit, contoh PS5-02", 24, unit?.Nama);
        if (nama is null)
            return;
        var jenis = BacaNama("Jenis konsol", 40, unit?.Jenis);
        if (jenis is null)
            return;
        var tarif = BacaAngka("Tarif rupiah per jam", minimum: 1, nilaiAwal: unit?.TarifPerJam);
        if (tarif is null)
            return;

        int id = unit?.Id ?? 0;
        using var proses = db.Database.BeginTransaction();
        if (db.UnitKonsol.Any(u => u.Id != id && EF.Functions.Collate(u.Nama, "NOCASE") == nama))
        {
            proses.Rollback();
            Jeda("Kode unit sudah digunakan.");
            return;
        }
        if (unit is null)
        {
            unit = new UnitKonsol();
            db.UnitKonsol.Add(unit);
        }
        unit.Nama = nama;
        unit.Jenis = jenis;
        unit.TarifPerJam = tarif.Value;
        db.SaveChanges();
        proses.Commit();
        Jeda($"Unit #{unit.Id} tersimpan.");
    }

    private static string? BacaNama(string label, int maksimum, string? nilaiAwal)
    {
        while (true)
        {
            var nama = Baca(nilaiAwal is null ? label : $"{label} ({nilaiAwal}, Enter = tetap)");
            if (nama is null)
                return null;
            if (nama.Length == 0)
            {
                if (nilaiAwal is not null)
                    return nilaiAwal;
                Console.WriteLine("Kolom ini wajib diisi.");
                continue;
            }
            if (nama.Length <= maksimum)
                return nama;
            Console.WriteLine($"Panjang teks maksimal {maksimum} karakter.");
        }
    }

    private static bool Konfirmasi(string pesan)
    {
        return string.Equals(Baca($"{pesan} (y/t)"), "y", StringComparison.OrdinalIgnoreCase);
    }
}
