# Multi-Tenant Hotel Booking System

Sistem ini adalah backend ASP.NET Core Web API untuk hotel booking multi-cabang dengan strategi **database per tenant**. Satu aplikasi melayani banyak cabang hotel, tetapi data operasional setiap cabang tetap terisolasi di database masing-masing.

## Ringkasan Arsitektur

Strategi multi-tenant:

```text
1 aplikasi API
  -> hotel_master
      branches
      customers_global
      staffs
      staff_branches

  -> hotel_sby
      customers
      rooms
      room_types
      room_images
      room_availabilities
      bookings
      payments

  -> hotel_smg
      customers
      rooms
      room_types
      room_images
      room_availabilities
      bookings
      payments
```

Tenant dipilih dari request header:

```http
X-Branch-Code: SBY
```

API akan membaca `hotel_master.Branches`, mengambil connection string tenant, lalu membuat `AppDbContext` untuk database cabang tersebut.

## Tech Stack

- .NET 8
- ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL
- Npgsql
- JWT Bearer Authentication
- BCrypt password hashing
- Bogus untuk seeder development
- xUnit untuk unit tests

## Project Structure

```text
backend/Hotel.Api
  Controllers/
  DTOs/
  Data/
  Entities/
    Master/
    Tenant/
  Migrations/
    Master/
    Tenant/
  Services/

tests/Hotel.Api.Tests
  unit tests untuk service layer dan authorization metadata
```

## Database

### Master Database

Default name:

```text
hotel_master
```

Berisi data global:

- `Branches`
- `CustomersGlobal`
- `Staffs`
- `StaffBranches`

Index penting:

- `Branches.Code` unique
- `CustomersGlobal.Email` unique
- `Staffs.Email` unique
- `StaffBranches.StaffId + StaffBranches.BranchId` unique

### Tenant Database

Contoh:

```text
hotel_sby
hotel_smg
hotel_mjk
hotel_jkt
```

Berisi data operasional cabang:

- `Customers`
- `RoomTypes`
- `Rooms`
- `RoomImages`
- `RoomAvailabilities`
- `Bookings`
- `Payments`

Index penting:

- `Rooms.RoomNumber` unique per tenant
- `RoomAvailabilities.RoomId + Date` unique
- `Bookings.BookingCode` unique

## Role dan Authorization

### SUPER_ADMIN

Scope: master/global only.

Boleh:

- manage branch
- provision tenant database
- manage staff
- assign staff ke branch

Tidak boleh:

- mencampuri management room tenant
- set harga tenant
- manage room operasional tenant

### SPV

Scope: cabang yang di-assign.

Boleh:

- manage room type
- set/update price
- manage room
- manage room image
- manage room availability

### FO

Scope: cabang yang di-assign.

Boleh:

- manage room
- update room status
- manage room image
- manage room availability

Tidak boleh:

- set/update price
- manage room type pricing

## Authentication

Staff login:

```http
POST /api/auth/staff/login
```

Body:

```json
{
  "email": "fo.sby@hotel.test",
  "password": "Password123!"
}
```

JWT berisi:

- `staff_id`
- `role`
- `allowed_branch_ids`

## Setup Development

### 1. Buat database PostgreSQL

```sql
CREATE DATABASE hotel_master;
CREATE DATABASE hotel_sby;
CREATE DATABASE hotel_smg;
CREATE DATABASE hotel_mjk;
CREATE DATABASE hotel_jkt;
```

### 2. Restore dan build

```bash
dotnet restore hotel-system.sln
dotnet build hotel-system.sln
```

### 3. Jalankan migration master

```bash
cd backend/Hotel.Api

dotnet ef database update --context MasterDbContext
```

### 4. Jalankan migration tenant default

Default tenant migration factory mengarah ke `hotel_sby`.

```bash
dotnet ef database update --context AppDbContext
```

Tenant lain:

```bash
TENANT_CONNECTION_STRING="Host=localhost;Port=5432;Database=hotel_smg;Username=postgres;Password=postgres" \
dotnet ef database update --context AppDbContext
```

### 5. Seed data development

Seed master:

```bash
dotnet run -- seed-master
```

Seed tenant default `hotel_sby`:

```bash
dotnet run -- seed-tenant
```

Seed tenant tertentu:

```bash
TENANT_CONNECTION_STRING="Host=localhost;Port=5432;Database=hotel_smg;Username=postgres;Password=postgres" \
dotnet run -- seed-tenant
```

Seeder master membuat:

- `SBY`
- `MJK`
- `JKT`
- `superadmin@hotel.test`
- `spv.sby@hotel.test`
- `fo.sby@hotel.test`

Password default seed:

```text
Password123!
```

### 6. Jalankan API

```bash
dotnet run
```

## Endpoint Utama

Semua endpoint tenant-aware harus menyertakan:

```http
X-Branch-Code: SBY
```

Endpoint protected harus menyertakan:

```http
Authorization: Bearer <TOKEN>
```

### Auth

```http
POST /api/auth/staff/login
```

### Super Admin Branch

```http
GET    /api/branches
GET    /api/branches/{id}
POST   /api/branches
PATCH  /api/branches/{id}/status
```

Create branch otomatis:

1. validasi branch code
2. create database tenant, contoh `SMG` menjadi `hotel_smg`
3. run migration tenant
4. simpan branch ke master kalau migration berhasil

Contoh:

```json
{
  "name": "Semarang",
  "code": "SMG"
}
```

Catatan: PostgreSQL user harus punya privilege `CREATEDB`.

### Super Admin Staff

```http
GET    /api/staff
GET    /api/staff/{id}
POST   /api/staff
PATCH  /api/staff/{id}/status
POST   /api/staff/{staffId}/branches/{branchId}
DELETE /api/staff/{staffId}/branches/{branchId}
```

Contoh create staff:

```json
{
  "name": "FO Semarang",
  "email": "fo.smg@hotel.test",
  "password": "Password123!",
  "role": "FO"
}
```

### SPV Room Types

```http
GET  /api/room-types
POST /api/room-types
PUT  /api/room-types/{id}
```

Write endpoint room type hanya untuk `SPV`. Harga berada di `RoomType.BasePrice`.

### SPV dan FO Rooms

```http
GET    /api/rooms
GET    /api/rooms/{id}
POST   /api/rooms
PUT    /api/rooms/{id}
PATCH  /api/rooms/{id}/status
POST   /api/rooms/{roomId}/images
DELETE /api/rooms/{roomId}/images/{imageId}
PUT    /api/rooms/{roomId}/availability
POST   /api/rooms/availability/search
```

FO boleh manage room dan availability, tetapi tidak punya endpoint untuk mengubah price.

### Booking

```http
POST /api/booking
```

Contoh:

```json
{
  "roomId": "342f1bff-4a2b-469b-a168-e94bfeba7fc2",
  "customerName": "Budi Santoso",
  "customerEmail": "budi@example.com",
  "customerPhone": "08123456789",
  "checkIn": "2026-09-01",
  "checkOut": "2026-09-03",
  "adultCount": 2,
  "childCount": 0,
  "paymentMethod": "midtrans"
}
```

Booking flow:

1. normalize date ke UTC date
2. cek/create `CustomersGlobal`
3. sync tenant `Customers`
4. cek room dan kapasitas
5. cek `RoomAvailabilities`
6. create booking dengan `BookingCode`
7. lock availability per tanggal
8. commit transaksi

Anti double booking:

- tenant transaction memakai `Serializable`
- unique index `RoomId + Date`
- race condition sudah diuji dengan 10 request bersamaan, hasilnya 1 sukses dan 9 gagal

### Payment

```http
POST /api/payments/midtrans/webhook
```

Mendukung payload Midtrans snake_case:

- `order_id`
- `transaction_id`
- `transaction_status`
- `payment_type`
- `gross_amount`

Mapping:

- `capture`, `settlement` -> paid
- `deny`, `cancel`, `expire`, `failure` -> failed

Jika payment failed, booking dicancel dan availability dibuka kembali.

## Testing

Test project:

```text
tests/Hotel.Api.Tests
```

Jalankan:

```bash
dotnet test hotel-system.sln
```

Coverage saat ini:

- booking positive dan negative cases
- customer global + tenant customer sync
- availability lock
- invalid booking date
- unavailable room
- capacity exceeded
- missing room in tenant
- room type create/update validation
- room duplicate validation
- room status validation
- availability upsert
- availability search
- room image add/delete
- payment settlement/failed webhook
- staff create/duplicate/invalid role
- staff branch assign/remove
- staff login valid/invalid
- super admin active branch handling
- tenant resolution missing/valid/forbidden branch
- authorization metadata untuk Super Admin, SPV, dan FO

Hasil terakhir:

```text
Passed: 35
Failed: 0
Skipped: 0
```

## Backup Strategy

Rekomendasi backup untuk database-per-tenant:

- backup `hotel_master` lebih sering karena menyimpan identity, staff, dan branch config
- backup tiap tenant DB secara terpisah
- gunakan `pg_dump -Fc`
- jalankan via cron atau background worker, bukan request API sinkron
- simpan ke object storage seperti S3/MinIO atau direktori server yang terenkripsi
- gunakan retention policy

Contoh strategi:

```text
hotel_master: tiap 6 jam
tenant DB: tiap malam
retention: daily 7 hari, weekly 4 minggu, monthly 6 bulan
```

Pengembangan lanjutan yang disarankan:

- tabel `backup_records` di master
- background worker untuk backup
- endpoint Super Admin untuk melihat status backup
- checksum file backup
- restore drill berkala

## Catatan Pengembangan Lanjutan

Prioritas berikutnya:

1. Booking expiry job untuk pending booking yang belum dibayar.
2. Harden Midtrans signature verification.
3. Endpoint list/detail/cancel booking untuk FO dan SPV.
4. Audit log untuk aksi Super Admin, SPV, dan FO.
5. Backup worker dan backup history.
6. Integration tests dengan PostgreSQL container.
7. Frontend Next.js untuk Super Admin, SPV, FO, dan customer booking.

## Rules Penting

- Jangan ubah menjadi single database.
- Jangan hardcode tenant DB untuk runtime request.
- Tetap gunakan `Customer`, bukan `User`.
- Pertahankan `CustomerGlobal` mapping.
- Pertahankan `RoomAvailability` sebagai sumber anti double booking.
- Pertahankan `BookingCode` unique.
- Super Admin tidak boleh mencampuri operational tenant.
- SPV bertanggung jawab untuk price.
- FO boleh menjalankan operasional room, tetapi tidak boleh mengubah price.
