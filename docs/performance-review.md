# Backend Performance Review

## 1. Admin list queries perlu projection ringan
- Masalah: endpoint list lama masih berpotensi memakai navigation payload detail.
- Lokasi file: `backend/Hotel.Api/Services/HotelAdminService.cs`, `backend/Hotel.Api/Services/StaffAdminService.cs`
- Impact: payload dan query cost meningkat saat data hotel/staff besar.
- Severity: Medium
- Recommendation: gunakan `AsNoTracking()` + `Select` DTO untuk list. Endpoint booking group baru mengikuti pola ini.

## 2. `OrderService.GetCurrentAsync` memakai `Include` berulang
- Masalah: query awal memuat `OrderDraft -> Items -> RoomType/RatePlan` memakai include chain.
- Lokasi file: `backend/Hotel.Api/Services/OrderService.cs`
- Impact: query lebih berat dan membawa entity tracking yang tidak diperlukan.
- Severity: Medium
- Recommendation: sudah dipindahkan ke projection DTO read-only dengan `AsNoTracking()`.

## 3. Payment detail group membutuhkan index event
- Masalah: payment webhook/admin event lookup membaca `PaymentEvents` berdasarkan `OrderId` dan `TransactionId`.
- Lokasi file: `backend/Hotel.Api/Data/AppDbContext.cs`, `backend/Hotel.Api/Migrations/Tenant/20260511055212_AddPaymentEvents.cs`
- Impact: webhook history akan melambat jika event bertambah banyak.
- Severity: Medium
- Recommendation: index `(OrderId, TransactionId)` dan `CreatedAt` sudah ditambahkan.

## 4. Room assignment melakukan loop candidate room
- Masalah: assignment mencari candidate rooms lalu cek overlap/block per room.
- Lokasi file: `backend/Hotel.Api/Services/RoomAssignmentService.cs`
- Impact: bisa mahal pada hotel dengan ribuan kamar dalam satu room type.
- Severity: Medium
- Recommendation: untuk fase sekarang aman; optimasi berikutnya bisa memakai query anti-join sekali untuk memilih kandidat pertama.

## 5. Branch connection string dibuat per request
- Masalah: `TenantDbFactory` membaca branch dari master lalu membangun options baru.
- Lokasi file: `backend/Hotel.Api/Factory/TenantDbFactory.cs`
- Impact: request tenant tinggi akan berulang lookup master dan create options.
- Severity: Medium
- Recommendation: cache tenant connection metadata dengan TTL pendek dan invalidasi saat branch mutation.

## 6. Search dan home bergantung price summary
- Masalah: search/home perlu harga minimum cepat lintas tenant.
- Lokasi file: `backend/Hotel.Api/Services/PublicHotelSearchService.cs`, `backend/Hotel.Api/Services/PublicHomeService.cs`, `backend/Hotel.Api/Services/HotelPriceSummaryService.cs`
- Impact: tanpa summary, query lintas tenant akan mahal dan tidak scale.
- Severity: High
- Recommendation: `HotelPriceSummaries` harus tetap jadi read model master; update otomatis via queue setelah mutation harga/visibility.

## 7. Inventory query harus tetap range-based
- Masalah: query date availability yang memakai materialized date list bisa berat.
- Lokasi file: `backend/Hotel.Api/Services/BookingService.cs`
- Impact: booking high concurrency rawan lambat dan lock lebih lama.
- Severity: High
- Recommendation: pertahankan pattern `Date >= checkIn && Date < checkOut`, composite index `RoomId, Date`, dan serializable transaction/lock yang sudah ada.

## 8. Index recommendation lanjutan
- Masalah: beberapa filter admin belum punya index khusus.
- Lokasi file: `backend/Hotel.Api/Data/AppDbContext.cs`, `backend/Hotel.Api/Data/MasterDbContext.cs`
- Impact: table scan saat data besar.
- Severity: Low
- Recommendation: pertimbangkan index tenant `BookingGroups(Status, CreatedAt)`, `Bookings(BookingGroupId, CreatedAt)`, `PaymentEvents(MappedStatus, CreatedAt)`, dan master `Hotels(IsActive, StarRating)`.
