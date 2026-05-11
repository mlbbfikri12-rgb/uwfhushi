# Frontend Performance Review

## 1. Query defaults terlalu minimal
- Issue: React Query hanya menonaktifkan `refetchOnWindowFocus`.
- File location: `frontend/src/app/providers.tsx`
- Severity: Medium
- Impact: data hotel/order bisa refetch terlalu sering dan memicu loading flash.
- Recommendation: default `staleTime`, `gcTime`, dan retry ringan sudah ditambahkan.

## 2. Prefetch booking dependency belum konsisten
- Issue: saat lanjut booking hanya prefetch satu room detail dengan query key manual.
- File location: `frontend/src/app/hotel/[branch]/hotel-page-client.tsx`
- Severity: Medium
- Impact: booking page masih bisa terasa blank jika banyak item.
- Recommendation: gunakan query key factory; langkah berikutnya prefetch semua selected room detail sebelum navigation.

## 3. LocalStorage access perlu dibatasi ke utility client-safe
- Issue: akses storage tersebar berpotensi hydration mismatch.
- File location: `frontend/src/utils/BookingDraftUtils.ts`, `frontend/src/app/booking/ClientPage.tsx`
- Severity: Medium
- Impact: malformed storage atau SSR access bisa membuat crash.
- Recommendation: storage access sudah dipusatkan di util dengan guard `window/localStorage` dan parser aman.

## 4. Booking page memakai beberapa derived state besar
- Issue: `orderItems` dibentuk dari backend order atau local draft + room queries dalam satu memo besar.
- File location: `frontend/src/app/booking/ClientPage.tsx`
- Severity: Medium
- Impact: rerender bisa mahal saat item banyak.
- Recommendation: split helper `mapOrderItems` dan `mapGuestDraftItems` ke util typed pada batch berikutnya.

## 5. Hotel detail rate plan rendering nested
- Issue: room type dan rate plan list render nested dalam satu component.
- File location: `frontend/src/app/hotel/[branch]/hotel-page-client.tsx`
- Severity: Medium
- Impact: perubahan qty bisa rerender banyak DOM.
- Recommendation: memoize/split `RatePlanRow` dan pass lightweight props.

## 6. Admin table states belum dipakai menyeluruh
- Issue: reusable state sudah tersedia tetapi belum diadopsi semua halaman admin.
- File location: `frontend/src/components/admin/EmptyState.tsx`, `frontend/src/components/admin/ErrorState.tsx`, `frontend/src/components/admin/TableSkeleton.tsx`
- Severity: Low
- Impact: UX loading/error masih mungkin tidak konsisten.
- Recommendation: adopsi bertahap saat masing-masing halaman admin disentuh.

## 7. Bundle risk dari icon import namespace
- Issue: hotel detail memakai `import * as Icons from "lucide-react"`.
- File location: `frontend/src/app/hotel/[branch]/hotel-page-client.tsx`
- Severity: Low
- Impact: bundle dapat membesar jika tree-shaking tidak optimal.
- Recommendation: map icon string ke explicit imports pada refactor komponen fasilitas berikutnya.
