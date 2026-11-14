using System.Runtime.InteropServices;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PSSeleraku;

internal static class StrukPdf
{
    private static readonly Guid FolderDownloads = new("374DE290-123F-4565-9164-39C4925E467B");

    public static string Cetak(Transaksi transaksi)
    {
        var folder = LokasiUnduhan();
        Directory.CreateDirectory(folder);
        var file = Path.Combine(folder, $"PS-Seleraku_Struk_{transaksi.Waktu:yyyyMMdd_HHmmss}_{transaksi.Id:D6}.pdf");

        Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A7);
                page.Margin(12);
                page.DefaultTextStyle(style => style.FontSize(9));
                page.Header().Text("PS Seleraku").AlignCenter().Bold().FontSize(14);
                page.Content().PaddingTop(6).Column(col =>
                {
                    col.Spacing(5);
                    col.Item().Text($"Transaksi #{transaksi.Id:D6} • {transaksi.Waktu:dd/MM/yyyy HH:mm}");
                    if (transaksi.JadwalSelesai is DateTime selesai)
                    {
                        col.Item().Text($"Pelanggan: {transaksi.Pelanggan}");
                        col.Item().Text($"Main sampai {selesai:dd/MM/yyyy HH:mm}");
                    }

                    foreach (var item in transaksi.Detail)
                    {
                        col.Item().Text($"{item.Nama}\n{item.Jumlah} {item.Satuan} × {Program.Rupiah(item.HargaSatuan)} = {Program.Rupiah(item.Subtotal)}");
                    }

                    col.Item().Text($"Total: {Program.Rupiah(transaksi.Total)}").Bold();
                    col.Item().Text($"Tunai: {Program.Rupiah(transaksi.Tunai)}");
                    col.Item().Text($"Kembalian: {Program.Rupiah(transaksi.Kembalian)}");
                });
            });
        }).GeneratePdf(file);

        return file;
    }

    private static string LokasiUnduhan()
    {
        if (OperatingSystem.IsWindows())
        {
            const uint janganPeriksaKeberadaan = 0x4000;
            int hasil = SHGetKnownFolderPath(in FolderDownloads, janganPeriksaKeberadaan, IntPtr.Zero, out var lokasi);
            try
            {
                if (hasil == 0 && Marshal.PtrToStringUni(lokasi) is { Length: > 0 } unduhan)
                    return unduhan;
            }
            finally
            {
                Marshal.FreeCoTaskMem(lokasi);
            }
        }

        var profil = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrEmpty(profil))
            throw new IOException("Folder Downloads tidak ditemukan.");
        return Path.Combine(profil, "Downloads");
    }

    [DllImport("shell32.dll", ExactSpelling = true)]
    private static extern int SHGetKnownFolderPath(in Guid folder, uint flags, IntPtr token, out IntPtr lokasi);
}
