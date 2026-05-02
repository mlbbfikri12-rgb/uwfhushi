"use client";

import { useMemo } from "react";
import { useQuery } from "@tanstack/react-query";
import Image from "next/image";
import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { MapPin, Star } from "lucide-react";
import { Navbar } from "@/components/layout/navbar";
import { BranchSearchForm } from "@/features/tenant/components/BranchSearchForm";
import { searchPublicHotels } from "@/services/branch.service";
import type { PublicHotelListItem } from "@/types/hotel-search";

type SearchPageClientProps = {
  searchParams: Record<string, string | string[] | undefined>;
};

function asString(value: string | string[] | undefined) {
  if (Array.isArray(value)) return value[0] ?? "";
  return value ?? "";
}

function asNumber(value: string | string[] | undefined, fallback: number) {
  const parsed = Number(asString(value));
  return Number.isFinite(parsed) ? parsed : fallback;
}

function parseCsvNumbers(value: string) {
  if (!value) return [];
  return value
    .split(",")
    .map((x) => Number(x.trim()))
    .filter((x) => Number.isFinite(x));
}

function parseCsvStrings(value: string) {
  if (!value) return [];
  return value
    .split(",")
    .map((x) => x.trim())
    .filter(Boolean);
}

function toImageUrl(url: string) {
  return /^https?:\/\//i.test(url)
    ? url
    : "https://images.unsplash.com/photo-1566073771259-6a8506099945?w=1200&q=80";
}

export default function SearchPageClient({ searchParams }: SearchPageClientProps) {
  const router = useRouter();
  const pathname = usePathname();

  const q = asString(searchParams.q);
  const checkIn = asString(searchParams.checkIn);
  const checkOut = asString(searchParams.checkOut);
  const totalRooms = asNumber(searchParams.total_rooms, 1);
  const minPrice = asNumber(searchParams.minPrice, 0);
  const maxPrice = asNumber(searchParams.maxPrice, 0);
  const selectedStars = parseCsvNumbers(asString(searchParams.stars));
  const selectedBrands = parseCsvStrings(asString(searchParams.brands));

  const hotelsQuery = useQuery({
    queryKey: ["hotel-search", q, checkIn, checkOut, totalRooms, minPrice, maxPrice, selectedStars, selectedBrands],
    queryFn: () =>
      searchPublicHotels({
        q,
        checkIn,
        checkOut,
        totalRooms,
        minPrice: minPrice > 0 ? minPrice : undefined,
        maxPrice: maxPrice > 0 ? maxPrice : undefined,
        stars: selectedStars.length > 0 ? selectedStars : undefined,
        brandNames: selectedBrands.length > 0 ? selectedBrands : undefined,
      }),
    enabled: q.length > 0 && checkIn.length > 0 && checkOut.length > 0,
  });

  const hotels = useMemo<PublicHotelListItem[]>(
    () => hotelsQuery.data?.hotels ?? [],
    [hotelsQuery.data],
  );

  const availableBrands = useMemo(
    () =>
      Array.from(new Set(hotels.map((x) => x.brand).filter((x): x is string => x.length > 0))).sort((a, b) =>
        a.localeCompare(b),
      ),
    [hotels],
  );

  const updateParam = (key: string, value?: string) => {
    const next = new URLSearchParams();
    Object.entries(searchParams).forEach(([k, v]) => {
      const raw = asString(v);
      if (raw) next.set(k, raw);
    });
    if (!value) next.delete(key);
    else next.set(key, value);
    router.replace(`${pathname}?${next.toString()}`);
  };

  return (
    <main className="min-h-screen bg-slate-50">
      <Navbar />
      <div className="bg-white border-b border-slate-100 pt-16">
        <div className="mx-auto max-w-7xl px-6 py-4">
          <BranchSearchForm />
        </div>
      </div>

      <div className="mx-auto grid max-w-7xl grid-cols-12 gap-6 px-6 py-8">
        <aside className="col-span-12 space-y-4 rounded-xl border bg-white p-4 lg:col-span-3">
          <p className="font-semibold text-slate-900">Filter</p>
          <div className="grid grid-cols-2 gap-2">
            <input defaultValue={minPrice || ""} onBlur={(e) => updateParam("minPrice", e.target.value || undefined)} placeholder="Min price" className="rounded border px-2 py-1 text-sm" />
            <input defaultValue={maxPrice || ""} onBlur={(e) => updateParam("maxPrice", e.target.value || undefined)} placeholder="Max price" className="rounded border px-2 py-1 text-sm" />
          </div>
          <div className="space-y-1">
            {[5, 4, 3, 2].map((star) => (
              <label key={star} className="flex items-center gap-2 text-sm">
                <input
                  type="checkbox"
                  checked={selectedStars.includes(star)}
                  onChange={() => {
                    const next = selectedStars.includes(star) ? selectedStars.filter((x) => x !== star) : [...selectedStars, star];
                    updateParam("stars", next.length > 0 ? next.sort((a, b) => a - b).join(",") : undefined);
                  }}
                />
                {star} Star
              </label>
            ))}
          </div>
          <div className="space-y-1">
            {availableBrands.map((brand) => (
              <label key={brand} className="flex items-center gap-2 text-sm">
                <input
                  type="checkbox"
                  checked={selectedBrands.includes(brand)}
                  onChange={() => {
                    const next = selectedBrands.includes(brand) ? selectedBrands.filter((x) => x !== brand) : [...selectedBrands, brand];
                    updateParam("brands", next.length > 0 ? next.sort((a, b) => a.localeCompare(b)).join(",") : undefined);
                  }}
                />
                {brand}
              </label>
            ))}
          </div>
        </aside>

        <section className="col-span-12 space-y-4 lg:col-span-9">
          {hotels.map((hotel) => (
            <div key={hotel.hotelId} className="flex flex-col gap-4 rounded-xl border bg-white p-4 md:flex-row">
              <div className="relative h-40 w-full overflow-hidden rounded-lg md:w-56">
                <Image src={toImageUrl(hotel.image)} alt={hotel.name} fill className="object-cover" unoptimized />
              </div>
              <div className="flex-1">
                <h2 className="text-lg font-semibold">{hotel.name}</h2>
                <div className="mt-1 flex items-center gap-2 text-sm text-slate-500">
                  <MapPin size={14} /> {hotel.city}
                </div>
                <div className="mt-1 flex items-center gap-1 text-sm">
                  <Star size={14} className="fill-amber-400 text-amber-400" />
                  {hotel.rating.toFixed(1)}
                </div>
              </div>
              <div className="text-right">
                <p className="text-xs text-slate-400">Starting from</p>
                <p className="text-lg font-bold text-[#c4a661]">Rp {hotel.priceFrom.toLocaleString("id-ID")}</p>
                <Link href={`/hotel/${hotel.slug}?checkIn=${checkIn}&checkOut=${checkOut}&total_rooms=${totalRooms}`} className="mt-2 inline-block rounded-lg bg-[#1a1f3c] px-4 py-2 text-sm text-white">
                  Book Now
                </Link>
              </div>
            </div>
          ))}
        </section>
      </div>
    </main>
  );
}
