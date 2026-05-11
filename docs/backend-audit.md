# Backend Audit

## 1. Admin booking visibility masih kurang lengkap
- Masalah: flow checkout sudah menggunakan `BookingGroup`, tetapi admin sebelumnya belum punya endpoint read khusus untuk list/detail group dan payment events.
- Lokasi file: `backend/Hotel.Api/Services/BookingService.cs`, `backend/Hotel.Api/Services/PaymentService.cs`, `backend/Hotel.Api/Controllers/AdminBookingsController.cs`
- Impact: FO/SPV sulit melihat satu checkout OTA sebagai satu order utuh, terutama multi-room checkout dan webhook retry.
- Severity: High
- Recommendation: gunakan endpoint read-only `GET /api/admin/bookings/groups`, `GET /api/admin/bookings/groups/{id}`, dan `GET /api/admin/bookings/payment-events` sebagai sumber dashboard operasional.

## 2. Payment webhook belum punya audit trail historis
- Masalah: status payment tersimpan di `Payments` dan `Bookings`, tetapi raw webhook/result processing tidak historis.
- Lokasi file: `backend/Hotel.Api/Services/PaymentService.cs`, `backend/Hotel.Api/Entities/Tenant/PaymentEvent.cs`
- Impact: sulit debug mismatch Midtrans, duplicate webhook, invalid order id, dan dispute payment.
- Severity: High
- Recommendation: `PaymentEvents` sudah ditambahkan secara additive. Pertahankan event ini append-only dan jangan dipakai sebagai source of truth booking.

## 3. Order draft authenticated belum auto-create tenant customer
- Masalah: user login yang belum pernah booking di branch tertentu bisa gagal `Customer not found in this branch` saat order/cart.
- Lokasi file: `backend/Hotel.Api/Services/OrderService.cs`
- Impact: UX hotel detail/booking terasa rusak untuk first-time customer lintas cabang.
- Severity: High
- Recommendation: `OrderService.AddAsync` sekarang auto-create tenant customer dari `CustomerGlobal`; `GetCurrentAsync` mengembalikan empty cart jika belum ada tenant customer.

## 4. Branch authorization staff masih role-only
- Masalah: beberapa controller admin memakai `[Authorize(Roles = ...)]`, tetapi belum semua service memvalidasi branch assignment staff.
- Lokasi file: `backend/Hotel.Api/Controllers/AdminRatePlansController.cs`, `backend/Hotel.Api/Controllers/AdminBookingsController.cs`, `backend/Hotel.Api/Services/*AdminService.cs`
- Impact: SPV/FO yang valid role-nya berpotensi mengakses branch lain jika token/header tidak difilter di service.
- Severity: High
- Recommendation: tambahkan service `IStaffBranchAccessGuard` yang membaca staff claim dan `staff_branches`, lalu panggil di semua service tenant admin.

## 5. Admin list endpoint masih campur detail payload
- Masalah: sebagian endpoint admin lama mengembalikan DTO detail penuh untuk list table.
- Lokasi file: `backend/Hotel.Api/Services/HotelAdminService.cs`, `backend/Hotel.Api/Services/StaffAdminService.cs`
- Impact: payload table membesar, query lebih berat, dan frontend table menjadi chatty.
- Severity: Medium
- Recommendation: pisahkan list DTO dan detail DTO bertahap. Booking group admin baru sudah memakai list/detail DTO terpisah.

## 6. Upload pipeline belum terstandarisasi penuh
- Masalah: storage abstraction sudah ada, tetapi audit metadata upload dan image transform lifecycle belum konsisten di semua domain.
- Lokasi file: `backend/Hotel.Api/Services/ObjectStorageService.cs`, `backend/Hotel.Api/Controllers/UploadsController.cs`
- Impact: sulit menelusuri asal image hotel/banner/room dan potensi ukuran file tidak seragam.
- Severity: Medium
- Recommendation: tambah logging structured untuk upload success/failure dan simpan full URL saja di DB.

## 7. Price summary masih bergantung command manual
- Masalah: `dotnet seed-price` sebelumnya diperlukan sebagai operational step untuk sinkronisasi `HotelPriceSummaries`.
- Lokasi file: `backend/Hotel.Api/Services/HotelPriceSummaryService.cs`, `backend/Hotel.Api/Program.cs`
- Impact: search/home dapat menampilkan harga stale setelah mutation rate plan/room type/hotel.
- Severity: High
- Recommendation: gunakan `IHotelPriceSummaryUpdater` queue untuk auto-update setelah mutation; command manual tetap hanya untuk repair/backfill.

## 8. Service validation masih memakai `Exception` generik
- Masalah: banyak service melempar `Exception` langsung untuk business validation.
- Lokasi file: `backend/Hotel.Api/Services/BookingService.cs`, `backend/Hotel.Api/Services/OrderService.cs`, `backend/Hotel.Api/Services/*AdminService.cs`
- Impact: response error tidak selalu konsisten dan sulit dipetakan ke status HTTP.
- Severity: Medium
- Recommendation: tambah exception typed bertahap (`ValidationException`, `NotFoundException`, `ForbiddenException`) tanpa rewrite controller sekaligus.

## 9. Observability belum merata
- Masalah: logging sudah ada di payment/order/assignment/price summary, tetapi belum semua auth/upload/admin mutation memakai structured log.
- Lokasi file: `backend/Hotel.Api/Services/ClientAuthService.cs`, `backend/Hotel.Api/Services/StaffAuthService.cs`, `backend/Hotel.Api/Services/ObjectStorageService.cs`
- Impact: incident login/upload sulit dilacak tanpa correlation id dan field domain.
- Severity: Medium
- Recommendation: gunakan `CorrelationIdMiddleware` dan tambahkan log field `branchCode`, `customerId`, `staffId`, `transactionId` sesuai konteks.
