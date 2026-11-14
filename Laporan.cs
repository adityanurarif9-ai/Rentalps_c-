using System.Globalization;
using Microsoft.EntityFrameworkCore;

namespace PSSeleraku;

internal static partial class Program
{
    private static void MenuLaporan(AppDbContext db)
    {
        while (true)
        {
            Judul("LAPORAN & CETAK ULANG STRUK");
            Console.WriteLine("1. Omzet hari ini");
            Console.WriteLine("2. Omzet berdasarkan rentang tanggal");
            Console.WriteLine("3. Seluruh riwayat dan omzet");
            Console.WriteLine("4. Unduh ulang struk dari nomor transaksi");
            Console.WriteLine("0. Kembali");
            switch (Baca("Pilih"))
            {
                case null or "0":
                    return;
                case "1":
                    var hariIni = DateOnly.FromDateTime(DateTime.Today);
                    LihatRekap(db, hariIni, hariIni);
                    break;
                case "2":
                    PilihTanggalLaporan(db);
                    break;
                case "3":
                    LihatRekap(db, null, null);
                    break;
                case "4":
                    CetakUlangStruk(db);
                    break;
                default:
                    Jeda("Pilihan tidak valid.");
                    break;
            }
        }
    }

    private static void PilihTanggalLaporan(AppDbContext db)
    {
        var awal = BacaTanggal("Tanggal awal");
        if (awal is null)
            return;
        var akhir = BacaTanggal("Tanggal akhir");
        if (akhir is null)
            return;
        if (akhir < awal)
        {
            Jeda("Tanggal akhir harus sama dengan atau setelah tanggal awal.");
            return;
        }

        LihatRekap(db, awal, akhir);
    }

    private static DateOnly? BacaTanggal(string label)
    {
        while (true)
        {
            var teks = Baca($"{label} (DD/MM/YYYY)");
            if (teks is null)
                return null;
            if (DateOnly.TryParseExact(teks, "dd/MM/yyyy", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var tanggal))
                return tanggal;
            Console.WriteLine("Masukkan tanggal dengan format DD/MM/YYYY.");
        }
    }

    private static void LihatRekap(AppDbContext db, DateOnly? awal, DateOnly? akhir)
    {
        Judul("RIWAYAT TRANSAKSI & OMZET");
        var transaksi = db.Transaksi.AsNoTracking().Include(t => t.Detail).AsQueryable();
        if (awal is DateOnly tanggalAwal && akhir is DateOnly tanggalAkhir)
        {
            Console.WriteLine($"Periode {tanggalAwal:dd/MM/yyyy} s.d. {tanggalAkhir:dd/MM/yyyy}\n");
            var dari = tanggalAwal.ToDateTime(TimeOnly.MinValue);
            var sampai = tanggalAkhir.ToDateTime(TimeOnly.MaxValue);
            transaksi = transaksi.Where(t => t.Waktu >= dari && t.Waktu <= sampai);
        }

        var daftar = transaksi.OrderByDescending(t => t.Waktu).ThenByDescending(t => t.Id).ToList();
        if (daftar.Count == 0)
            Console.WriteLine("Belum ada transaksi pada periode ini.");

        foreach (var item in daftar)
        {
            string jenis = item.UnitKonsolId is null ? "Kasir" : $"Rental • {item.Pelanggan}";
            var ringkasan = string.Join(", ", item.Detail.Select(d => $"{d.Nama} ({d.Jumlah} {d.Satuan})"));
            Console.WriteLine($"#{item.Id:D6} | {item.Waktu:dd/MM/yyyy HH:mm} | {jenis} | {Rupiah(item.Total)}");
            Console.WriteLine($"  {ringkasan}");
        }

        var rental = daftar.Where(t => t.UnitKonsolId != null).Sum(t => t.Total);
        var penjualan = daftar.Where(t => t.UnitKonsolId == null).Sum(t => t.Total);
        Console.WriteLine($"\nJumlah transaksi: {daftar.Count}");
        Console.WriteLine($"Omzet rental: {Rupiah(rental)}");
        Console.WriteLine($"Omzet kasir:  {Rupiah(penjualan)}");
        Jeda($"Total omzet:  {Rupiah(rental + penjualan)}");
    }

    private static void CetakUlangStruk(AppDbContext db)
    {
        var id = BacaAngka("Nomor transaksi (0 = kembali)");
        if (id is null or 0)
            return;
        var transaksi = db.Transaksi.AsNoTracking().Include(t => t.Detail).SingleOrDefault(t => t.Id == id);
        if (transaksi is null)
        {
            Jeda("Transaksi tidak ditemukan.");
            return;
        }

        Console.WriteLine($"Transaksi #{transaksi.Id:D6} | {transaksi.Waktu:dd/MM/yyyy HH:mm} | {Rupiah(transaksi.Total)}");
        SimpanStrukPdf(transaksi);
    }
}
