# PS Seleraku

Aplikasi konsol C# untuk rental konsol per jam dan penjualan makanan, minuman, serta camilan. Data transaksi dan stok tersimpan di SQLite. Setiap pembayaran menghasilkan struk PDF.

## Menjalankan aplikasi

Gunakan .NET 10 SDK. Buka `PSSeleraku.csproj` di Visual Studio lalu tekan **Ctrl+F5**, atau jalankan perintah berikut di folder proyek.

```powershell
dotnet run
```

Aplikasi membuat `ps_seleraku.db` di direktori kerja. Pertahankan direktori kerja yang sama untuk membuka data sebelumnya. Setelah pembayaran, PDF otomatis tersimpan di **Downloads** dan aplikasi menampilkan nama filenya.

## Menu

- **Rental konsol** menampilkan unit yang tersedia. Pilih unit, masukkan nama pelanggan dan durasi, lalu terima pembayaran tunai. Unit kembali tersedia setelah waktunya habis atau sesi diakhiri lebih awal. Pembayaran tetap berlaku jika sesi diakhiri lebih awal.
- **Kasir** punya pilihan makanan, minuman, dan camilan. Semua kategori bisa masuk ke keranjang yang sama. Stok berkurang setelah transaksi tersimpan.
- **Manajemen produk & stok** bisa menambah, melihat, mengedit, dan menghapus produk. Kategori, harga, dan stok bisa diubah. Detail transaksi lama tetap tersedia setelah produk dihapus.
- **Manajemen unit konsol** bisa menambah unit dan mengubah kode, jenis, serta tarif. Arsipkan unit yang sudah tidak digunakan atau aktifkan kembali dari menu yang sama. Sesi harus berakhir sebelum unit bisa diarsipkan.
- **Laporan & cetak ulang struk** menampilkan omzet hari ini, rentang tanggal pilihan, atau seluruh riwayat. Pilih **Unduh ulang struk dari nomor transaksi** untuk menyimpan kembali PDF ke Downloads.

Masukkan nominal rupiah tanpa pemisah ribuan, misalnya `20000`.

## Data contoh

Saat database pertama kali dibuat, aplikasi menyediakan satu unit untuk setiap jenis konsol. Tarif dan jumlah unit dapat disesuaikan melalui **Manajemen Unit Konsol**.

| Konsol | Tarif per jam |
| --- | ---: |
| PlayStation 3 | Rp 6.000 |
| PlayStation 4 | Rp 10.000 |
| PlayStation 5 | Rp 15.000 |
| Nintendo Switch | Rp 12.000 |

## Skenario demo

1. Buka **Manajemen Produk & Stok**. Tambahkan Roti Bakar sebagai camilan, dengan harga Rp 9.000 dan stok 8. Ubah harga menjadi Rp 10.000, lalu periksa daftarnya.
2. Buka **Rental**. Sewakan PS5-01 ke pelanggan selama 2 jam. Masukkan `40000` untuk pembayaran. Aplikasi menampilkan kembalian Rp 10.000 dan menandai unit sedang dipakai.
3. Buka **Kasir**. Masukkan satu Mie Goreng, satu Es Teh, dan satu Roti Bakar ke keranjang. Bayar `25000`. Totalnya Rp 24.000 dan stok Roti Bakar turun menjadi 7.
4. Hapus Roti Bakar dari menu produk, lalu buka **Laporan & Cetak Ulang Struk**. Omzet hari itu bertambah Rp 54.000 dari dua transaksi tadi. Roti Bakar masih tercatat pada riwayat dan struk.

## Menggunakan data dari versi pertama

Jalankan versi baru dari direktori yang memuat `ps_seleraku.db` lama. Aplikasi menambahkan kolom status arsip saat pertama dibuka dan tetap menggunakan stok serta transaksi yang sudah tersimpan.

## Catatan awal

### Rentalps_c-
semoga berhasil ya tuhan
