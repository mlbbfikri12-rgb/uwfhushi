// "use client";

// import { useMemo } from "react";
// import { useQuery } from "@tanstack/react-query";
// import Image from "next/image";
// import Link from "next/link";
// import { usePathname, useRouter, useSearchParams } from "next/navigation";
// import { MapPin, Star, Phone, Mail, Share2, Search } from "lucide-react";
// import { searchPublicHotels } from "@/services/client/public.client";
// import { Navbar } from "@/components/layout/navbar";
// import BranchSearchForm from "@/features/tenant/components/BranchSearchForm";
// import type { PublicHotelListItem } from "@/types/hotel-search";

// function toInt(value: string | null, fallback = 0) {
//   const parsed = Number(value ?? "");
//   return Number.isFinite(parsed) ? parsed : fallback;
// }

// function parseCsvNumbers(value: string | null) {
//   if (!value) return [];
//   return value
//     .split(",")
//     .map((x) => Number(x.trim()))
//     .filter((x) => Number.isFinite(x) && x > 0);
// }

// function parseCsvStrings(value: string | null) {
//   if (!value) return [];
//   return value
//     .split(",")
//     .map((x) => x.trim())
//     .filter(Boolean);
// }

// function toImageUrl(url: string) {
//   return /^https?:\/\//i.test(url)
//     ? url
//     : "https://images.unsplash.com/photo-1566665797739-1674de7a421a?w=1200&q=80";
// }

// type FilterSidebarProps = {
//   minPrice: number;
//   maxPrice: number;
//   selectedStars: number[];
//   selectedBrands: string[];
//   availableBrands: string[];

//   onMinPrice: (value?: string) => void;
//   onMaxPrice: (value?: string) => void;

//   onToggleStar: (star: number) => void;
//   onToggleBrand: (brand: string) => void;
// };

// // ─── SKELETON ────────────────────────────────────────────────────────────────

// function HotelCardSkeleton() {
//   return (
//     <div className="flex gap-5 rounded-2xl border border-slate-200 bg-white p-4 animate-pulse">
//       <div className="w-52 h-40 rounded-xl bg-slate-200 shrink-0" />
//       <div className="flex-1 space-y-3 py-1">
//         <div className="h-5 bg-slate-200 rounded w-2/3" />
//         <div className="h-4 bg-slate-100 rounded w-1/3" />
//         <div className="h-4 bg-slate-100 rounded w-1/4" />
//       </div>
//       <div className="w-40 space-y-3 py-1">
//         <div className="h-4 bg-slate-100 rounded w-1/2 ml-auto" />
//         <div className="h-7 bg-slate-200 rounded w-3/4 ml-auto" />
//         <div className="h-10 bg-slate-200 rounded mt-6" />
//       </div>
//     </div>
//   );
// }

// // ─── HOTEL CARD ──────────────────────────────────────────────────────────────

// function HotelCard({
//   hotel,
//   checkIn,
//   checkOut,
//   totalRooms,
// }: {
//   hotel: PublicHotelListItem;
//   checkIn: string;
//   checkOut: string;
//   totalRooms: number;
// }) {
//   return (
//     <div className="flex flex-col md:flex-row rounded-2xl border border-slate-200 bg-white overflow-hidden hover:shadow-lg transition-shadow">
//       {/* Foto */}
//       <div className="relative w-full md:w-56 h-48 md:h-auto shrink-0">
//         {hotel.image ? (
//           <Image
//             src={toImageUrl(hotel.image)}
//             alt={hotel.name}
//             fill
//             className="object-cover"
//           />
//         ) : (
//           <div className="w-full h-full bg-slate-100 flex items-center justify-center">
//             <span className="text-xs text-slate-400">No Image</span>
//           </div>
//         )}
//       </div>

//       {/* Info tengah */}
//       <div className="flex-1 p-5">
//         <div className="flex items-start justify-between gap-3">
//           <div>
//             <h2 className="text-base font-bold text-slate-900 leading-snug">
//               {hotel.name}
//             </h2>
//             <div className="flex items-center gap-1.5 mt-1.5 text-sm text-slate-500">
//               <MapPin size={13} className="text-[#c4a661] shrink-0" />
//               {hotel.city}
//             </div>
//           </div>

//           {/* Rating badge */}
//           <div className="shrink-0 flex items-center gap-1 bg-[#1a1f3c] text-white text-xs font-bold px-2.5 py-1.5 rounded-lg">
//             <Star size={11} className="fill-[#c4a661] text-[#c4a661]" />
//             {hotel.rating?.toFixed(1) ?? "—"}
//           </div>
//         </div>

//         {/* Brand */}
//         {hotel.brand && (
//           <span className="inline-block mt-3 text-xs bg-slate-100 text-slate-500 px-2.5 py-1 rounded-full font-medium">
//             {hotel.brand}
//           </span>
//         )}

//         {/* Review */}
//         <p className="text-xs text-slate-400 mt-3">Belum ada ulasan</p>

//         {/* Action buttons */}
//         <div className="flex items-center gap-2 mt-4">
//           <button className="flex items-center gap-1.5 text-xs text-slate-500 border border-slate-200 px-3 py-1.5 rounded-lg hover:bg-slate-50 transition-colors">
//             <Phone size={12} /> Call
//           </button>
//           <button className="flex items-center gap-1.5 text-xs text-slate-500 border border-slate-200 px-3 py-1.5 rounded-lg hover:bg-slate-50 transition-colors">
//             <Mail size={12} /> Email
//           </button>
//           <button className="flex items-center gap-1.5 text-xs text-slate-500 border border-slate-200 px-3 py-1.5 rounded-lg hover:bg-slate-50 transition-colors">
//             <Share2 size={12} /> Share
//           </button>
//         </div>
//       </div>

//       {/* Harga + book */}
//       <div className="flex flex-row md:flex-col items-center md:items-end justify-between md:justify-between p-5 border-t md:border-t-0 md:border-l border-slate-100 md:w-48 shrink-0">
//         <div className="text-left md:text-right">
//           <p className="text-[10px] text-slate-400 uppercase tracking-wide font-semibold">
//             Starting from
//           </p>
//           <p className="text-xl font-bold text-slate-900 mt-0.5">
//             Rp {hotel.priceFrom?.toLocaleString("id-ID") ?? "—"}
//           </p>
//           <p className="text-[10px] text-slate-400">/room/night</p>
//         </div>

//         <Link
//           href={`/hotel/${hotel.branchCode}?checkIn=${checkIn}&checkOut=${checkOut}&total_rooms=${totalRooms}`}
//           className="mt-0 md:mt-auto bg-[#1a1f3c] hover:bg-[#252c52] active:scale-95 text-white text-sm font-semibold px-5 py-2.5 rounded-xl transition-all text-center whitespace-nowrap"
//         >
//           Book Now
//         </Link>
//       </div>
//     </div>
//   );
// }

// // ─── FILTER SIDEBAR ───────────────────────────────────────────────────────────

// function FilterSidebar({
//   minPrice,
//   maxPrice,
//   selectedStars,
//   selectedBrands,
//   availableBrands,
//   onMinPrice,
//   onMaxPrice,
//   onToggleStar,
//   onToggleBrand,
// }: FilterSidebarProps) {
//   return (
//     <aside className="w-64 shrink-0 space-y-5">
//       {/* Price Range */}
//       <div className="bg-white rounded-2xl border border-slate-200 p-5">
//         <h3 className="text-sm font-bold text-slate-800 mb-4">Price Range</h3>
//         <div className="space-y-2">
//           <div className="relative">
//             <span className="absolute left-3 top-1/2 -translate-y-1/2 text-xs text-slate-400">
//               Rp
//             </span>
//             <input
//               type="number"
//               defaultValue={minPrice || ""}
//               placeholder="Min"
//               className="w-full pl-8 pr-3 py-2 text-sm border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-[#1a1f3c]/20"
//               onBlur={(e) => onMinPrice(e.target.value || undefined)}
//             />
//           </div>
//           <div className="relative">
//             <span className="absolute left-3 top-1/2 -translate-y-1/2 text-xs text-slate-400">
//               Rp
//             </span>
//             <input
//               type="number"
//               defaultValue={maxPrice || ""}
//               placeholder="Max"
//               className="w-full pl-8 pr-3 py-2 text-sm border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-[#1a1f3c]/20"
//               onBlur={(e) => onMaxPrice(e.target.value || undefined)}
//             />
//           </div>
//         </div>
//       </div>

//       {/* Brand */}
//       <div className="bg-white rounded-2xl border border-slate-200 p-5">
//         <h3 className="text-sm font-bold text-slate-800 mb-4">Brand</h3>
//         {availableBrands.length === 0 ? (
//           <p className="text-xs text-slate-400">
//             Tidak ada brand pada hasil ini.
//           </p>
//         ) : (
//           <div className="space-y-2.5">
//             {availableBrands.map((brand: string) => (
//               <label
//                 key={brand}
//                 className="flex items-center gap-2.5 cursor-pointer group"
//               >
//                 <input
//                   type="checkbox"
//                   checked={selectedBrands.includes(brand)}
//                   onChange={() => onToggleBrand(brand)}
//                   className="w-4 h-4 accent-[#1a1f3c] rounded"
//                 />
//                 <span className="text-sm text-slate-600 group-hover:text-slate-900 transition-colors">
//                   {brand}
//                 </span>
//               </label>
//             ))}
//           </div>
//         )}
//       </div>

//       {/* Star Rating */}
//       <div className="bg-white rounded-2xl border border-slate-200 p-5">
//         <h3 className="text-sm font-bold text-slate-800 mb-4">
//           Hotel Star Rating
//         </h3>
//         <div className="space-y-2.5">
//           {[5, 4, 3, 2].map((star) => (
//             <label
//               key={star}
//               className="flex items-center gap-2.5 cursor-pointer group"
//             >
//               <input
//                 type="checkbox"
//                 checked={selectedStars.includes(star)}
//                 onChange={() => onToggleStar(star)}
//                 className="w-4 h-4 accent-[#1a1f3c] rounded"
//               />
//               <span className="flex items-center gap-1 text-sm text-slate-600 group-hover:text-slate-900 transition-colors">
//                 {Array.from({ length: star }).map((_, i) => (
//                   <Star
//                     key={i}
//                     size={12}
//                     className="fill-amber-400 text-amber-400"
//                   />
//                 ))}
//                 {Array.from({ length: 5 - star }).map((_, i) => (
//                   <Star
//                     key={i}
//                     size={12}
//                     className="fill-slate-200 text-slate-200"
//                   />
//                 ))}
//               </span>
//             </label>
//           ))}
//         </div>
//       </div>
//     </aside>
//   );
// }

// // ─── PAGE ─────────────────────────────────────────────────────────────────────

// export default function SearchPage() {
//   const router = useRouter();
//   const pathname = usePathname();
//   const params = useSearchParams();

//   const q = params.get("q") ?? "";
//   const checkIn = params.get("checkIn") ?? "";
//   const checkOut = params.get("checkOut") ?? "";
//   const totalRooms = toInt(params.get("total_rooms"), 1);
//   const minPrice = toInt(params.get("minPrice"), 0);
//   const maxPrice = toInt(params.get("maxPrice"), 0);
//   const selectedStars = parseCsvNumbers(params.get("stars"));
//   const selectedBrands = parseCsvStrings(params.get("brands"));

//   const hotelsQuery = useQuery({
//     queryKey: [
//       "hotel-search",
//       q,
//       checkIn,
//       checkOut,
//       totalRooms,
//       minPrice,
//       maxPrice,
//       selectedStars,
//       selectedBrands,
//     ],
//     queryFn: () =>
//       searchPublicHotels({
//         q,
//         checkIn,
//         checkOut,
//         totalRooms,
//         minPrice: minPrice > 0 ? minPrice : undefined,
//         maxPrice: maxPrice > 0 ? maxPrice : undefined,
//         stars: selectedStars.length > 0 ? selectedStars : undefined,
//         brandNames: selectedBrands.length > 0 ? selectedBrands : undefined,
//       }),
//     enabled: q.length > 0 && checkIn.length > 0 && checkOut.length > 0,
//   });

//   const hotels = useMemo<PublicHotelListItem[]>(
//     () => hotelsQuery.data?.hotels ?? [],
//     [hotelsQuery.data],
//   );

//   const updateParam = (key: string, value?: string) => {
//     const next = new URLSearchParams(params.toString());
//     if (!value) next.delete(key);
//     else next.set(key, value);
//     router.replace(`${pathname}?${next.toString()}`);
//   };

//   const toggleStar = (star: number) => {
//     const next = selectedStars.includes(star)
//       ? selectedStars.filter((x) => x !== star)
//       : [...selectedStars, star];
//     updateParam(
//       "stars",
//       next.length > 0 ? next.sort((a, b) => a - b).join(",") : undefined,
//     );
//   };

//   const toggleBrand = (brand: string) => {
//     const next = selectedBrands.includes(brand)
//       ? selectedBrands.filter((x) => x !== brand)
//       : [...selectedBrands, brand];
//     updateParam(
//       "brands",
//       next.length > 0
//         ? next.sort((a, b) => a.localeCompare(b)).join(",")
//         : undefined,
//     );
//   };

//   const availableBrands = useMemo(
//     () =>
//       Array.from(
//         new Set(
//           hotels.map((h) => h.brand).filter((b): b is string => Boolean(b)),
//         ),
//       ).sort((a, b) => a.localeCompare(b)),
//     [hotels],
//   );

//   const isLoading = hotelsQuery.isLoading;
//   const isError = hotelsQuery.isError;

//   return (
//     <main className="min-h-screen bg-slate-50">
//       <Navbar />

//       {/* ── SEARCH BAR ── */}
//       <div className="bg-white border-b border-slate-100 shadow-sm pt-16">
//         <div className="max-w-7xl mx-auto px-6 py-4">
//           <BranchSearchForm />
//         </div>
//       </div>

//       <div className="max-w-7xl mx-auto px-6 py-8">
//         {/* Header */}
//         <div className="flex items-center justify-between mb-6">
//           <div>
//             <h1 className="text-xl font-bold text-slate-900">
//               {q ? `Hotel di "${q}"` : "Semua Hotel"}
//             </h1>
//             {!isLoading && (
//               <p className="text-sm text-slate-500 mt-0.5">
//                 {hotels.length} hotel ditemukan
//                 {checkIn && checkOut && (
//                   <span className="ml-1">
//                     · {checkIn} → {checkOut} · {totalRooms} room
//                   </span>
//                 )}
//               </p>
//             )}
//           </div>
//         </div>

//         <div className="flex gap-7">
//           {/* ── FILTER ── */}
//           <div className="hidden lg:block">
//             <FilterSidebar
//               minPrice={minPrice}
//               maxPrice={maxPrice}
//               selectedStars={selectedStars}
//               selectedBrands={selectedBrands}
//               availableBrands={availableBrands}
//               onMinPrice={(v: string | undefined) => updateParam("minPrice", v)}
//               onMaxPrice={(v: string | undefined) => updateParam("maxPrice", v)}
//               onToggleStar={toggleStar}
//               onToggleBrand={toggleBrand}
//             />
//           </div>

//           {/* ── RESULTS ── */}
//           <div className="flex-1 min-w-0 space-y-4">
//             {/* Loading */}
//             {isLoading && (
//               <>
//                 <HotelCardSkeleton />
//                 <HotelCardSkeleton />
//                 <HotelCardSkeleton />
//               </>
//             )}

//             {/* Error */}
//             {isError && (
//               <div className="rounded-2xl border border-red-200 bg-red-50 p-5 text-sm text-red-700">
//                 Gagal memuat hasil pencarian. Silakan coba lagi.
//               </div>
//             )}

//             {/* Kosong */}
//             {!isLoading && !isError && hotels.length === 0 && (
//               <div className="rounded-2xl border border-slate-200 bg-white p-10 text-center">
//                 <Search size={36} className="text-slate-300 mx-auto mb-3" />
//                 <p className="text-slate-600 font-medium">
//                   Hotel tidak ditemukan
//                 </p>
//                 <p className="text-slate-400 text-sm mt-1">
//                   Coba ubah keyword atau tanggal pencarian.
//                 </p>
//               </div>
//             )}

//             {/* Belum search */}
//             {!isLoading && !isError && !q && (
//               <div className="rounded-2xl border border-slate-200 bg-white p-10 text-center">
//                 <Search size={36} className="text-slate-300 mx-auto mb-3" />
//                 <p className="text-slate-600 font-medium">Mulai cari hotel</p>
//                 <p className="text-slate-400 text-sm mt-1">
//                   Masukkan nama kota atau hotel di atas.
//                 </p>
//               </div>
//             )}

//             {/* Hotel cards */}
//             {!isLoading &&
//               hotels.map((hotel: PublicHotelListItem) => (
//                 <HotelCard
//                   key={hotel.hotelId}
//                   hotel={hotel}
//                   checkIn={checkIn}
//                   checkOut={checkOut}
//                   totalRooms={totalRooms}
//                 />
//               ))}
//           </div>
//         </div>
//       </div>
//     </main>
//   );
// }
