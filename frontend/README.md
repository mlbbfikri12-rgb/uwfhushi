# Frontend Multi-Tenant Hotel (Next.js 14)

Frontend ini menggunakan Next.js 14 App Router + TypeScript + Tailwind, dan sudah diatur tenant-aware dengan header `X-Branch-Code` wajib.

## Struktur Domain

```text
src/
  app/
  features/
    booking/
    rooms/
    tenant/
  lib/
  services/
  store/
  types/
```

## Tenant Flow

1. User mencari/menentukan branch dari central page (`/`).
   Pencarian memakai endpoint publik `/api/public/branches`.
2. App navigate ke route dinamis `/hotel/[branch]`.
3. `branch` disimpan ke Zustand (`useBranchStore`) dan ke cookie (`js-cookie`).
4. Saat refresh, state direhydrate dari cookie.
5. Semua request API memakai `X-Branch-Code` dari Zustand.

## Konfigurasi Environment

Buat file `.env.local`:

```bash
NEXT_PUBLIC_API_URL=http://localhost:5000
```

Tidak ada hardcoded API URL di code.

## Request API

Axios instance ada di:

- [api.ts](/home/pongo_linux/hotel-system/frontend/src/lib/api.ts)

Rule:

- request akan ditolak jika branch belum ada di Zustand
- header `X-Branch-Code` selalu ditambahkan
- request credential/cookie dikirim dengan `withCredentials`
- jika API mengembalikan `401`, user diarahkan ke `/login`

## Authentication

Customer:

- `/login`
- `/register`
- service: [auth.service.ts](/home/pongo_linux/hotel-system/frontend/src/services/auth.service.ts)
- backend menyimpan session pada httpOnly cookie `customer_token`
- booking akan mengecek `/api/auth/me`; jika belum login, user redirect ke `/login?redirect=...`
- booking memakai customer session, bukan data customer dari form request

Admin/staff:

- `/admin/login`
- `/admin`
- backend menyimpan session pada httpOnly cookie `staff_token`
- halaman admin membaca `/api/auth/staff/me` dan menampilkan menu berdasarkan role:
  - `SUPER_ADMIN`: branch, banner, staff, all booking
  - `SPV`: booking cabang, laporan
  - `FO`: booking, update status booking
- `/admin` sudah memuat data backend nyata:
  - `SUPER_ADMIN`: `/api/branches`, `/api/staff`, `/api/banners/active`
  - `SPV`/`FO`: `/api/rooms` dengan `X-Branch-Code` dari `allowedBranches`

## Data Fetching

Semua fetch utama menggunakan React Query:

- daftar kamar branch untuk user publik:
  `POST /api/public/rooms/availability/search`
- query key: `["public-rooms", branch, payload]`
- punya loading state, error state, dan cache config

## Booking Form

Booking memakai:

- React Hook Form
- Zod validation
- date-fns
- react-day-picker
- Sonner notifications

Komponen:

- [BookingForm.tsx](/home/pongo_linux/hotel-system/frontend/src/features/booking/components/BookingForm.tsx)
- [BranchSearchForm.tsx](/home/pongo_linux/hotel-system/frontend/src/features/tenant/components/BranchSearchForm.tsx)

Update terbaru:

- DayPicker diperbaiki agar klik tanggal pertama (`from`) langsung terpilih secara visual.
- Nilai form tetap divalidasi hanya saat range lengkap (`from` + `to`).
- Fix diterapkan di booking form dan home search form.
- DayPicker diberi warna range yang lebih kontras (start/end/middle) agar status pilih tanggal terlihat jelas.
- Date picker sekarang tampil sebagai popover saat field tanggal diklik (bukan selalu terbuka).
- Ditambahkan info durasi inap otomatis: `X malam`.
- Layout form pencarian home dioptimalkan menjadi single-row pada desktop.
- Styling range DayPicker diperkuat dengan `modifiersStyles` agar highlight selected/range tetap terlihat konsisten di UI.
- Durasi inap dipindahkan ke samping label `Tanggal Menginap` agar layout tidak bergeser.
- Ditambahkan ikon `lucide-react` pada field kota/hotel, tanggal, tamu, dan tombol cari.

## Halaman Utama

- `/`:
  - input pencarian kota/hotel (public branch search)
  - wajib pilih check-in/check-out + jumlah tamu
  - redirect ke `/hotel/[branch]?checkIn=...&checkOut=...`
- `/hotel/[branch]`:
  - sync route param ke Zustand + cookie
  - load daftar kamar tenant dari endpoint public availability
  - submit booking tenant hanya untuk customer yang sudah login

## Menjalankan

```bash
npm run dev
```

Frontend default berjalan di `http://localhost:3000`.

## Menjalankan Dengan Docker Compose

Di root project:

```bash
docker compose up -d --build
```

Akses aplikasi:

- `http://localhost:8080`

Pada mode ini frontend akan memakai:

- `NEXT_PUBLIC_API_URL=/api`

dan request API diproxy Nginx ke backend.
