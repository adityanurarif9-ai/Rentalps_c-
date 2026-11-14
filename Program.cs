using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;

namespace PSSeleraku;

internal static partial class Program
{
    private static readonly CultureInfo Indonesia = CultureInfo.GetCultureInfo("id-ID");

    [STAThread]
    private static void Main()
    {
        QuestPDF.Settings.License = LicenseType.Community;

        try
        {
            using var db = new AppDbContext();
            db.SiapkanDatabase();
        }
        catch (SqliteException ex)
        {
            Console.WriteLine($"Database tidak bisa dibuka. {ex.Message}");
            return;
        }

        while (true)
        {
            Judul("PS SELERAKU");
            Console.WriteLine($"{DateTime.Today.ToString("dddd, dd MMMM yyyy", Indonesia)}\n");
            Console.WriteLine("1. Rental PS3, PS4, PS5 & Nintendo Switch");
            Console.WriteLine("2. Kasir Makanan, Minuman & Camilan");
            Console.WriteLine("3. Manajemen Produk & Stok");
            Console.WriteLine("4. Manajemen Unit Konsol");
            Console.WriteLine("5. Laporan & Cetak Ulang Struk");
            Console.WriteLine("0. Keluar");
            var pilihan = Baca("Pilih menu");
            if (pilihan is null or "0")
            {
                Console.WriteLine("Terima kasih telah menggunakan PS Seleraku.");
                return;
            }

            using var db = new AppDbContext();
            try
            {
                switch (pilihan)
                {
                    case "1": MenuRental(db); break;
                    case "2": Kasir(db); break;
                    case "3": KelolaProduk(db); break;
                    case "4": KelolaUnit(db); break;
                    case "5": MenuLaporan(db); break;
                    default: Jeda("Pilihan tidak valid."); break;
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                Jeda("Data baru saja berubah. Buka kembali menu untuk melihat data terbaru.");
            }
            catch (Exception ex) when (ex is SqliteException or DbUpdateException)
            {
                Jeda($"Operasi database gagal. {ex.GetBaseException().Message}");
            }
        }
    }

    private static void MenuRental(AppDbContext db)
    {
        Judul("RENTAL KONSOL");
        var sekarang = DateTime.Now;
        var sesi = RentalAktif(db, sekarang).AsNoTracking().ToList()
            .ToDictionary(t => t.UnitKonsolId!.Value);
        var unit = db.UnitKonsol.AsNoTracking().Where(u => !u.Diarsipkan).OrderBy(u => u.Id).ToList();
        if (unit.Count == 0)
        {
            Jeda("Belum ada unit konsol yang aktif.");
            return;
        }
        foreach (var konsol in unit)
        {
            string status = sesi.TryGetValue(konsol.Id, out var aktif)
                ? $"Dipakai {aktif.Pelanggan} sampai {aktif.JadwalSelesai:dd/MM HH:mm}"
                : "Tersedia";
            Console.WriteLine($"{konsol.Id}. {konsol.Nama} | {konsol.Jenis} | {Rupiah(konsol.TarifPerJam)}/jam | {status}");
        }

        Console.WriteLine("\n1. Mulai rental");
        Console.WriteLine("2. Akhiri sesi lebih awal");
        Console.WriteLine("0. Kembali");
        var pilihan = Baca("Pilih");
        if (pilihan is null or "0")
            return;
        if (pilihan is not ("1" or "2"))
        {
            Jeda("Pilihan tidak valid.");
            return;
        }

        var id = BacaAngka("ID unit (0 = kembali)");
        if (id is null or 0)
            return;
        var dipilih = unit.Find(u => u.Id == id);
        if (dipilih is null)
        {
            Jeda("Unit tidak ditemukan.");
            return;
        }

        if (pilihan == "2")
        {
            AkhiriRental(db, dipilih, sesi.GetValueOrDefault(dipilih.Id));
            return;
        }

        if (sesi.ContainsKey(dipilih.Id))
        {
            Jeda("Unit masih dipakai.");
            return;
        }

        var pelanggan = BacaNama("Nama pelanggan", 60, null);
        if (pelanggan is null)
            return;
        var jam = BacaAngka("Durasi dalam jam (1–24, 0 = batal)", 0, 24);
        if (jam is null or 0)
            return;

        var detail = new DetailTransaksi
        {
            Nama = $"{dipilih.Nama} ({dipilih.Jenis})",
            Jumlah = jam.Value,
            HargaSatuan = dipilih.TarifPerJam,
            Satuan = "jam"
        };
        var tunai = BacaTunai(detail.Subtotal);
        if (tunai is null)
            return;

        Transaksi transaksi;
        using (var proses = db.Database.BeginTransaction())
        {
            var mulai = DateTime.Now;
            var unitTerbaru = db.UnitKonsol.AsNoTracking().SingleOrDefault(u => u.Id == dipilih.Id);
            if (unitTerbaru is null || unitTerbaru.Diarsipkan || unitTerbaru.TarifPerJam != dipilih.TarifPerJam
                || unitTerbaru.Nama != dipilih.Nama || unitTerbaru.Jenis != dipilih.Jenis)
            {
                proses.Rollback();
                Jeda("Data unit berubah. Buka kembali menu rental untuk melihat data terbaru.");
                return;
            }

            if (RentalAktif(db, mulai).Any(t => t.UnitKonsolId == dipilih.Id))
            {
                proses.Rollback();
                Jeda("Unit sudah dipakai. Silakan pilih unit lain.");
                return;
            }

            transaksi = new Transaksi
            {
                Waktu = mulai,
                UnitKonsolId = dipilih.Id,
                Pelanggan = pelanggan,
                JadwalSelesai = mulai.AddHours(jam.Value),
                Detail = [detail],
                Total = detail.Subtotal,
                Tunai = tunai.Value
            };
            db.Transaksi.Add(transaksi);
            db.SaveChanges();
            proses.Commit();
        }

        Console.WriteLine($"Sesi dimulai. Main sampai {transaksi.JadwalSelesai:dd/MM/yyyy HH:mm}.");
        TampilkanStruk(transaksi);
    }

    private static IQueryable<Transaksi> RentalAktif(AppDbContext db, DateTime sekarang)
    {
        return db.Transaksi.Where(t => t.UnitKonsolId != null
            && t.JadwalSelesai > sekarang && t.SelesaiLebihAwal == null);
    }

    private static void AkhiriRental(AppDbContext db, UnitKonsol unit, Transaksi? sesi)
    {
        if (sesi is null)
        {
            Jeda("Tidak ada sesi aktif untuk unit ini.");
            return;
        }

        if (!string.Equals(Baca($"Akhiri sesi {unit.Nama}? Pembayaran tetap. (y/t)"), "y",
                StringComparison.OrdinalIgnoreCase))
            return;

        var sekarang = DateTime.Now;
        int diakhiri = RentalAktif(db, sekarang).Where(t => t.Id == sesi.Id)
            .ExecuteUpdate(perubahan => perubahan.SetProperty(t => t.SelesaiLebihAwal, sekarang));
        Jeda(diakhiri == 1 ? $"{unit.Nama} sudah tersedia." : "Sesi tersebut sudah berakhir.");
    }

    private static void Kasir(AppDbContext db)
    {
        var katalog = db.Produk.AsNoTracking().OrderBy(p => p.Id).ToList();
        var keranjang = new Dictionary<int, DetailTransaksi>();

        while (true)
        {
            Judul("KASIR MAKANAN, MINUMAN & CAMILAN");
            foreach (var item in keranjang.Values)
                Console.WriteLine($"{item.Jumlah} × {item.Nama} = {Rupiah(item.Subtotal)}");
            Console.WriteLine($"Total keranjang: {Rupiah(keranjang.Values.Sum(item => item.Subtotal))}\n");
            Console.WriteLine("1. Makanan");
            Console.WriteLine("2. Minuman");
            Console.WriteLine("3. Camilan");
            Console.WriteLine("4. Pembayaran");
            Console.WriteLine("0. Kembali (kosongkan keranjang)");
            var pilihan = Baca("Pilih");
            if (pilihan is null or "0")
                return;
            if (pilihan == "4")
            {
                if (keranjang.Count == 0)
                {
                    Jeda("Keranjang masih kosong.");
                    continue;
                }

                var total = keranjang.Values.Sum(item => item.Subtotal);
                var tunai = BacaTunai(total);
                if (tunai is null)
                    continue;
                SimpanPenjualan(db, keranjang, total, tunai.Value);
                return;
            }

            if (pilihan is not ("1" or "2" or "3"))
            {
                Jeda("Pilihan tidak valid.");
                continue;
            }

            var kategori = (Kategori)int.Parse(pilihan);
            var daftar = katalog.Where(p => p.Kategori == kategori).ToList();
            TampilkanProduk(daftar);
            var id = BacaAngka("ID produk (0 = kembali)");
            if (id is null or 0)
                continue;
            var produk = daftar.Find(p => p.Id == id);
            if (produk is null)
            {
                Jeda("Produk tidak ditemukan di kategori ini.");
                continue;
            }

            int jumlahSaatIni = keranjang.GetValueOrDefault(produk.Id)?.Jumlah ?? 0;
            var jumlah = BacaAngka($"Jumlah di keranjang (saat ini {jumlahSaatIni}, maks. {produk.Stok}, 0 = hapus)",
                0, produk.Stok);
            if (jumlah is null)
                continue;
            if (jumlah == 0)
            {
                keranjang.Remove(produk.Id);
                continue;
            }

            keranjang[produk.Id] = new DetailTransaksi
            {
                Nama = produk.Nama, Jumlah = jumlah.Value, HargaSatuan = produk.Harga, Satuan = "item"
            };
        }
    }

    private static void SimpanPenjualan(AppDbContext db, Dictionary<int, DetailTransaksi> keranjang, long total, long tunai)
    {
        var transaksi = new Transaksi
        {
            Waktu = DateTime.Now, Total = total, Tunai = tunai, Detail = keranjang.Values.ToList()
        };
        using (var proses = db.Database.BeginTransaction())
        {
            foreach (var (id, item) in keranjang)
            {
                var produk = db.Produk.Find(id);
                if (produk is null || produk.Stok < item.Jumlah || produk.Harga != item.HargaSatuan
                    || produk.Nama != item.Nama)
                {
                    proses.Rollback();
                    Jeda("Data produk berubah. Penjualan dibatalkan. Buka kasir lagi untuk memuat data terbaru.");
                    return;
                }

                produk.Stok -= item.Jumlah;
            }

            db.Transaksi.Add(transaksi);
            db.SaveChanges();
            proses.Commit();
        }

        TampilkanStruk(transaksi);
    }

    private static void TampilkanProduk(IReadOnlyList<Produk> daftar)
    {
        if (daftar.Count == 0)
        {
            Console.WriteLine("Belum ada produk.");
            return;
        }

        foreach (var produk in daftar)
        {
            var stok = produk.Stok == 0 ? "Stok habis" : $"Stok {produk.Stok}";
            Console.WriteLine($"{produk.Id,2}. {produk.Nama} | {produk.Kategori} | {Rupiah(produk.Harga)} | {stok}");
        }
    }

    private static long? BacaTunai(long total)
    {
        Console.WriteLine($"Total pembayaran: {Rupiah(total)}");
        while (true)
        {
            var input = Baca("Tunai dalam rupiah (0 = batal)");
            if (input is null or "0")
                return null;
            if (long.TryParse(input, out long tunai) && tunai >= total)
                return tunai;
            Console.WriteLine("Masukkan angka rupiah tanpa titik. Uang tunai harus mencukupi.");
        }
    }

    private static void TampilkanStruk(Transaksi transaksi)
    {
        Console.WriteLine($"Transaksi #{transaksi.Id:D6} tersimpan. Kembalian {Rupiah(transaksi.Kembalian)}.");
        SimpanStrukPdf(transaksi);
    }

    private static void SimpanStrukPdf(Transaksi transaksi)
    {
        try
        {
            var file = StrukPdf.Cetak(transaksi);
            Jeda($"Unduh struk selesai. Buka File Explorer → Downloads.\nNama file: {Path.GetFileName(file)}");
        }
        catch (Exception ex)
        {
            Jeda($"Unduh struk gagal. Transaksi tersimpan. Coba lagi dari menu Laporan.\n{ex.Message}");
        }
    }

    internal static string Rupiah(long nilai) => $"Rp {nilai.ToString("N0", Indonesia)}";

    private static string? Baca(string label)
    {
        Console.Write($"{label}: ");
        return Console.ReadLine()?.Trim();
    }

    private static int? BacaAngka(string label, int minimum = 0, int maksimum = int.MaxValue,
        int? nilaiAwal = null)
    {
        var teks = Baca(nilaiAwal is null ? label : $"{label} ({nilaiAwal}, Enter = tetap)");
        if (teks is null)
            return null;
        if (teks.Length == 0 && nilaiAwal is not null)
            return nilaiAwal;
        if (int.TryParse(teks, out int angka) && angka >= minimum && angka <= maksimum)
            return angka;
        Jeda("Angka di luar batas atau format tidak valid.");
        return null;
    }

    private static void Judul(string teks)
    {
        if (!Console.IsOutputRedirected)
        {
            Console.Clear();
            var warna = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"=== {teks} ===");
            Console.ForegroundColor = warna;
            return;
        }
        Console.WriteLine($"=== {teks} ===");
    }

    private static void Jeda(string pesan)
    {
        Console.WriteLine(pesan);
        Console.WriteLine("Tekan Enter untuk melanjutkan.");
        Console.ReadLine();
    }
}
