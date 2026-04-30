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
- Redis cache via `IDistributedCache`
- ASP.NET Core Rate Limiter
- SMTP email notification
- Cloudflare R2-compatible image upload abstraction
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
- `HeroBanners`
- `CustomersGlobal`
- `Staffs`
- `StaffBranches`

Index penting:

- `Branches.Code` unique
- `HeroBanners.IsActive + HeroBanners.SortOrder`
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

Customer auth:

```http
POST /api/auth/register
POST /api/auth/login
GET  /api/auth/me
```

Alias tanpa prefix `/api` juga tersedia:

```http
POST /auth/register
POST /auth/login
GET  /auth/me
```

Customer auth memakai `CustomerGlobal` dan tidak menambah tabel baru. Password disimpan sebagai bcrypt hash di `CustomerGlobal.PasswordHash`.
Response tidak mengekspos `PasswordHash`. Token customer berisi claim:

- `customer_id`
- `auth_type=customer`
- `role=CUSTOMER`

Backend juga mengirim token ke httpOnly cookie `customer_token`. Staff login mengirim cookie `staff_token`.

Authorization customer dan staff dipisahkan dengan claim `auth_type`:

- customer token: `auth_type=customer`
- staff token: `auth_type=staff`

Booking memakai policy `CustomerOnly`, sehingga tidak bergantung pada role staff. Endpoint staff/admin memakai staff token dan role `SUPER_ADMIN`, `SPV`, atau `FO`.
Backend memilih cookie token berdasarkan path endpoint agar customer dan staff session tidak saling tertukar saat berada di browser yang sama.

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

### Public Branch Search (Central Website)

```http
GET /api/public/branches?q=smg&limit=10
```

Endpoint ini dibuat untuk flow pencarian kota/hotel sebelum user memilih tenant.
Response endpoint ini dicache Redis dengan key `branches:search:{keyword}:limit:{limit}`.

### Public Hotel Full

```http
GET /api/hotel/{branch}/full?checkIn=2026-09-01&checkOut=2026-09-03&adult=2&child=0
```

Endpoint ini menggabungkan data master (`Branches`) dan data tenant (`Rooms`, `RoomTypes`, `RoomImages`, `RoomAvailabilities`) tanpa menduplikasi data hotel ke tenant.
Endpoint ini memakai Redis cache dengan key berbasis `branch`, `checkIn`, `checkOut`, jumlah adult, dan child.

### Dynamic Banners

```http
GET /api/banners/active
```

Data banner berasal dari tabel master `HeroBanners`. Gambar banner disimpan sebagai URL, sehingga cocok dipakai dengan Cloudflare R2 atau object storage lain.

### Public Room Availability (Central Website)

```http
POST /api/public/rooms/availability/search
```

Body:

```json
{
  "checkIn": "2026-09-01",
  "checkOut": "2026-09-03",
  "adultCount": 2,
  "childCount": 0
}
```

Endpoint ini tidak memerlukan auth staff, tetapi tetap tenant-aware melalui `X-Branch-Code`.
Availability dicache Redis dengan key berbasis `branch`, `checkIn`, `checkOut`, adult, dan child.
Cache availability dan `/hotel/{branch}/full` dihapus otomatis saat booking dibuat atau availability berubah.

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
  "checkIn": "2026-09-01",
  "checkOut": "2026-09-03",
  "adultCount": 2,
  "childCount": 0,
  "paymentMethod": "midtrans"
}
```

Booking sekarang wajib customer login (`role=CUSTOMER`). Backend mengambil customer dari token `customer_id`, lalu membaca data dari `CustomerGlobal`. Identitas customer dari request body tidak dipakai untuk booking authenticated.

Booking flow:

1. normalize date ke UTC date
2. cek/create `CustomersGlobal`
3. sync tenant `Customers`
4. cek room dan kapasitas
5. cek `RoomAvailabilities`
6. create booking dengan `BookingCode`
7. lock availability per tanggal
8. commit transaksi
9. invalidate cache availability dan hotel full untuk branch terkait
10. kirim email booking ke customer dan internal jika `Email:Enabled=true`

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

### Upload Image

```http
POST /api/uploads/images
Content-Type: multipart/form-data
```

Form fields:

- `file`: image `jpeg`, `png`, `webp`, atau `avif`
- `folder`: contoh `banners`, `hotels`, atau `rooms`

Endpoint mengembalikan URL dan object key. Database hanya menyimpan URL, bukan binary file.
Konfigurasi upload memakai section `Storage` di appsettings.

## Cache, Rate Limit, Email, Storage

Semua konfigurasi berada di `appsettings*.json` atau environment variable.

Redis:

```json
"Cache": {
  "RedisConnection": "localhost:6379",
  "DefaultTtlMinutes": 5,
  "HotelFullTtlMinutes": 10,
  "BranchSearchTtlMinutes": 5,
  "AvailabilityTtlMinutes": 5
}
```

Rate limit:

```json
"RateLimit": {
  "GlobalPermitLimit": 100,
  "GlobalWindowSeconds": 60,
    "BookingPermitLimit": 5,
    "BookingWindowSeconds": 60,
    "AuthLoginPermitLimit": 5,
    "AuthRegisterPermitLimit": 3,
    "AuthWindowSeconds": 60,
    "UseRedisForBookingLimit": true
  }
```

Global rate limit memakai middleware bawaan ASP.NET Core. Booking endpoint juga memakai policy `booking`, dan ada Redis-backed booking limiter agar konsisten saat backend berjalan multi-instance.
Auth endpoint juga dilimit:

- client login: 5 request/menit
- register: 3 request/menit
- staff login: 5 request/menit

Jika limit terlampaui, API mengembalikan HTTP `429`.

Email:

```json
"Email": {
  "Enabled": false,
  "Host": "",
  "Port": 587,
  "EnableSsl": true,
  "Username": "",
  "Password": "",
  "FromEmail": "noreply@example.com",
  "FromName": "Hotel System",
  "InternalEmail": "",
  "RetryCount": 3
}
```

Email booking memakai HTML template berisi booking code, hotel, tanggal, tamu, kamar, dan total harga. Ada retry sederhana jika SMTP gagal.

Storage:

```json
"Storage": {
  "Provider": "Local",
  "UploadEndpoint": "",
  "PublicBaseUrl": "/uploads",
  "Bucket": "",
  "AccessToken": "",
  "MaxUploadBytes": 5242880,
  "LocalRootPath": "wwwroot/uploads",
  "MaxImageWidth": 1920,
  "WebpQuality": 82
}
```

Abstraction storage berada di service layer (`IObjectStorageService`). Pada development gunakan `Provider=Local`, file akan diproses dan disimpan ke `wwwroot/uploads` lalu diserve sebagai static file.
Pada production gunakan R2-compatible gateway/service dengan `Provider=R2`, `UploadEndpoint`, `Bucket`, `AccessToken`, dan `PublicBaseUrl`.

Pipeline image:

- validasi content type `jpeg`, `png`, `webp`, `avif`
- resize otomatis jika lebar melebihi `MaxImageWidth`
- konversi output menjadi `.webp`
- quality dikontrol lewat `WebpQuality`
- database tetap menyimpan URL saja

Validasi booking/availability:

```json
"BookingValidation": {
  "MaxStayNights": 30,
  "MaxAdvanceBookingDays": 365
}
```

Validasi tambahan yang aktif:

- `checkIn` tidak boleh di masa lalu
- `checkOut` harus setelah `checkIn`
- durasi menginap tidak boleh melewati `MaxStayNights`
- branch wajib valid
- adult minimal 1 dan child tidak boleh negatif

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

## Frontend Status (Next.js 14)

Frontend sudah diimplementasikan awal dengan fokus multi-tenant awareness.

Lokasi:

- [frontend/README.md](/home/pongo_linux/hotel-system/frontend/README.md)
- [page.tsx](/home/pongo_linux/hotel-system/frontend/src/app/page.tsx)
- [branch page](/home/pongo_linux/hotel-system/frontend/src/app/hotel/[branch]/page.tsx)

Poin yang sudah ada:

1. Next.js 14 App Router + TypeScript + Tailwind.
2. Struktur domain-based: `app`, `features`, `lib`, `services`, `store`, `types`.
3. Routing dinamis `/hotel/[branch]`.
4. Zustand `useBranchStore` sebagai source utama branch.
5. Persist branch via cookie (`js-cookie`) + rehydrate saat app load.
6. Axios interceptor wajib `X-Branch-Code`, request ditolak jika branch belum ada.
7. React Query untuk fetch daftar kamar (`/api/rooms`) dengan loading/error/cache.
8. Booking form dengan React Hook Form + Zod + date-fns + react-day-picker.
9. Notifikasi user dengan Sonner.
10. Semua endpoint frontend memakai `NEXT_PUBLIC_API_URL`.
11. Halaman `/login` dan `/register` untuk customer.
12. Booking redirect ke `/login` jika customer belum authenticated.
13. Axios mengirim httpOnly cookie dengan `withCredentials` dan redirect saat response `401`.
14. Halaman `/admin/login` dan `/admin` membaca staff role untuk menampilkan menu dan data sesuai RBAC.
15. Admin panel sudah mengonsumsi endpoint nyata:
    - `SUPER_ADMIN`: branch, staff, banner aktif
    - `SPV` dan `FO`: room tenant berdasarkan allowed branch

### Flow Central Website (tanpa subdomain)

Flow ini didukung penuh oleh arsitektur saat ini:

1. User melakukan pencarian kota/hotel dari central website.
2. User memilih branch/hotel.
3. Frontend pindah ke route `/hotel/[branch]`.
4. Nilai `branch` disimpan ke Zustand + cookie.
5. Semua request API otomatis mengirim `X-Branch-Code` dari Zustand.

Catatan:

- Request pencarian branch (`/api/public/branches`) memang non-tenant dan tidak memerlukan `X-Branch-Code`.
- Setelah branch dipilih, request tenant/public-room (`/api/public/rooms/availability/search`, `/api/booking`, dst) wajib membawa `X-Branch-Code`.

Ini berarti `X-Branch-Code` bisa sepenuhnya dikendalikan dari input/pilihan user, tidak perlu subdomain.

## Docker & Nginx

File deployment:

- [docker-compose.yml](/home/pongo_linux/hotel-system/docker-compose.yml)
- [backend Dockerfile](/home/pongo_linux/hotel-system/backend/Hotel.Api/Dockerfile)
- [frontend Dockerfile](/home/pongo_linux/hotel-system/frontend/Dockerfile)
- [nginx config](/home/pongo_linux/hotel-system/nginx/default.conf)

Service stack:

1. `postgres` (single PostgreSQL instance, multi database)
2. `redis` untuk cache dan distributed booking rate limit
3. `backend` ASP.NET Core (`:5000`)
4. `frontend` Next.js (`:3000`)
5. `nginx` reverse proxy (`:8080`)

Routing:

- `http://localhost:8080/api/*` -> `backend:5000`
- `http://localhost:8080/*` -> `frontend:3000`

Frontend build arg sudah di-set:

- `NEXT_PUBLIC_API_URL=/api`

Sehingga browser request selalu lewat origin Nginx yang sama, lalu Nginx meneruskan ke backend.

### Menjalankan via Docker

```bash
docker compose up -d --build
```

Setelah container hidup:

1. Jalankan migration dari host (lihat bagian Setup Development), karena image runtime backend tidak menyertakan `dotnet-ef`.
2. Seed data via host atau jalankan command seed sebelum packaging.
3. Setelah migration + seed selesai, aplikasi siap diakses dari `http://localhost:8080`.

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
