# Frontend Audit

## 1. Booking draft belum punya versioning dan key stabil
- Issue: draft lama hanya menyimpan `slug/checkIn/checkOut/items` dan merge berdasarkan `roomTypeId/ratePlanId`.
- File location: `frontend/src/utils/BookingDraftUtils.ts`, `frontend/src/types/BookingDraft.ts`
- Severity: High
- Impact: multi-room lintas branch/date rentan duplicate atau stale draft.
- Recommendation: gunakan cart util versioned dengan key `branch/slug/checkIn/checkOut/roomTypeId/ratePlanId`, safe parser, merge helper, dan payload builder.

## 2. Authenticated order dan guest draft sempat tercampur
- Issue: booking page mencoba sync local draft ke backend order tanpa guard kuat.
- File location: `frontend/src/app/booking/ClientPage.tsx`
- Severity: High
- Impact: duplicate `POST /api/order/add` bisa terjadi saat rerender/query refetch.
- Recommendation: gunakan guard sync satu kali dan backend order tetap source of truth untuk authenticated user.

## 3. Query key belum konsisten
- Issue: beberapa query memakai string array manual seperti `["room-detail", slug, roomTypeId]`.
- File location: `frontend/src/app/booking/ClientPage.tsx`, `frontend/src/app/hotel/[branch]/hotel-page-client.tsx`
- Severity: Medium
- Impact: invalidation/prefetch rentan mismatch dan fetch ganda.
- Recommendation: gunakan `frontend/src/lib/query-keys.ts`.

## 4. Axios terlalu ketat meminta branch untuk semua endpoint
- Issue: interceptor sebelumnya reject request tanpa `X-Branch-Code`, termasuk endpoint public/master.
- File location: `frontend/src/lib/api.ts`
- Severity: High
- Impact: home/search/admin master bisa gagal saat branch belum dipilih.
- Recommendation: selalu kirim branch jika tersedia, tetapi hanya reject untuk endpoint tenant-bound seperti order/booking/rooms.

## 5. Console log langsung di UI
- Issue: komponen UI memakai `console.log`.
- File location: `frontend/src/app/hotel/[branch]/hotel-page-client.tsx`, `frontend/src/features/booking/components/BookingForm.tsx`
- Severity: Low
- Impact: noise di production/debugging tidak konsisten.
- Recommendation: gunakan `appLogger` development-only.

## 6. Loading/error admin belum reusable
- Issue: table admin belum punya empty/error/skeleton reusable.
- File location: `frontend/src/components/admin/*`
- Severity: Medium
- Impact: tiap halaman admin berisiko punya UX loading/error tidak konsisten.
- Recommendation: komponen `EmptyState`, `ErrorState`, dan `TableSkeleton` sudah ditambahkan untuk dipakai bertahap.

## 7. Image sizing belum merata
- Issue: beberapa `next/image` belum punya `sizes` dan LCP hero belum diberi `priority`.
- File location: `frontend/src/app/hotel/[branch]/hotel-page-client.tsx`
- Severity: Medium
- Impact: CLS/LCP kurang optimal.
- Recommendation: tambahkan `priority` untuk image utama hotel dan `sizes` pada gallery/room image.

## 8. Room detail page masih cukup besar sebagai client component
- Issue: rendering gallery, room cards, rate plans, cart sync, dan query logic berada dalam satu file.
- File location: `frontend/src/app/hotel/[branch]/hotel-page-client.tsx`
- Severity: Medium
- Impact: maintainability dan rerender sulit dilacak.
- Recommendation: split bertahap ke `RoomTypeCard`, `RatePlanRow`, dan `BookingSummaryPanel` tanpa redesign visual.
