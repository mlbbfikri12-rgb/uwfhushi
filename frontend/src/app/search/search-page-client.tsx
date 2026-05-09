"use client";

import { useMemo } from "react";
import { useQuery } from "@tanstack/react-query";
import Image from "next/image";
import Link from "next/link";
// import { usePathname, useRouter } from "next/navigation";
import { MapPin, Star } from "lucide-react";

import { Navbar } from "@/components/layout/navbar";
import BranchSearchForm from "@/features/tenant/components/BranchSearchForm";
import { searchPublicHotels } from "@/services/server/branch.service";

import type {
  PublicHotelListItem,
  PublicHotelSearchResponse,
} from "@/types/hotel-search";

import Footer from "../components/Footer";

type SearchPageClientProps = {
  searchParams: Record<string, string | string[] | undefined>;
  initialData?: PublicHotelSearchResponse;
};

function asString(value: string | string[] | undefined) {
  if (Array.isArray(value)) return value[0] ?? "";
  return value ?? "";
}

function asNumber(value: string | string[] | undefined, fallback: number) {
  const parsed = Number(asString(value));
  return Number.isFinite(parsed) ? parsed : fallback;
}

function toImageUrl(url: string) {
  return /^https?:\/\//i.test(url)
    ? url
    : "https://images.unsplash.com/photo-1566073771259-6a8506099945?w=1200&q=80";
}

function SkeletonGrid() {
  return (
    <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
      {Array.from({ length: 6 }).map((_, i) => (
        <div
          key={i}
          className="animate-pulse rounded-xl border bg-white overflow-hidden"
        >
          <div className="h-48 bg-slate-200" />
          <div className="p-4 space-y-3">
            <div className="h-4 bg-slate-200 rounded w-3/4" />
            <div className="h-3 bg-slate-200 rounded w-1/2" />
            <div className="h-4 bg-slate-200 rounded w-1/3" />
          </div>
        </div>
      ))}
    </div>
  );
}

export default function SearchPageClient({
  searchParams,
  initialData,
}: SearchPageClientProps) {
  // const router = useRouter();
  // const pathname = usePathname();

  // =========================
  // PARAMS
  // =========================

  const q = asString(searchParams.q);
  const checkIn = asString(searchParams.checkIn);
  const checkOut = asString(searchParams.checkOut);
  const totalRooms = asNumber(searchParams.total_rooms, 1);

  // =========================
  // QUERY
  // =========================

  const hotelsQuery = useQuery({
    queryKey: ["hotel-search", q, checkIn, checkOut, totalRooms],

    queryFn: () => {
      const params: {
        q?: string;
        checkIn?: string;
        checkOut?: string;
        totalRooms?: number;
      } = {
        q,
        totalRooms,
      };
      if (checkIn && checkOut) {
        params.checkIn = checkIn;
        params.checkOut = checkOut;
      }

      return searchPublicHotels(params);
    },

    enabled: q.length > 0,
    initialData,
    staleTime: 1000 * 60 * 5,
  });

  const { data, isLoading, isFetching } = hotelsQuery;

  // =========================
  // DATA
  // =========================

  const hotels = useMemo<PublicHotelListItem[]>(
    () => data?.hotels ?? [],
    [data],
  );

  // =========================
  // BOOKING QUERY
  // =========================

  const bookingQuery = (() => {
    const params = new URLSearchParams();

    if (checkIn && checkOut) {
      params.set("checkIn", checkIn);
      params.set("checkOut", checkOut);
      params.set("total_rooms", String(totalRooms));
    }

    return params.toString();
  })();

  // =========================
  // UI
  // =========================

  return (
    <>
      <Navbar />

      <main className="min-h-screen bg-slate-50">
        {/* SEARCH */}
        <div className="bg-white border-b border-slate-100 pt-16">
          <div className="mx-auto max-w-7xl px-6 py-4">
            <BranchSearchForm />
          </div>
        </div>

        <div className="mx-auto max-w-7xl px-6 py-10">
          {/* HEADER */}
          <div className="mb-6 flex items-center justify-between">
            <div>
              <h1 className="text-xl font-bold text-slate-900">
                {q ? `Hotels in ${q}` : "All Hotels"}
              </h1>
              <p className="text-sm text-slate-500">
                {hotels.length} properties found
              </p>
            </div>

            <div className="text-sm text-slate-400 hidden md:block">
              Explore best stays for your trip
            </div>
          </div>

          {/* PROMO */}
          <div className="mb-6 rounded-xl bg-[#1a1f3c] text-white p-4 flex justify-between items-center">
            <div>
              <p className="text-sm opacity-80">Special Offer</p>
              <p className="font-semibold">
                Get best price guarantee for your stay
              </p>
            </div>
          </div>

          {/* INITIAL LOADING (no SSR data) */}
          {isLoading && !initialData && <SkeletonGrid />}

          {/* GRID */}
          {!isLoading && (
            <div className="relative">
              {/* REFETCH OVERLAY */}
              {isFetching && (
                <div className="absolute inset-0 bg-white/60 backdrop-blur-sm z-10 flex items-center justify-center text-sm text-slate-500">
                  Updating results...
                </div>
              )}

              <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
                {hotels.map((hotel) => (
                  <div
                    key={hotel.hotelId}
                    className="group rounded-xl border bg-white overflow-hidden hover:shadow-xl transition-all duration-300 hover:-translate-y-1"
                  >
                    {/* IMAGE */}
                    <div className="relative h-48 w-full overflow-hidden">
                      <Image
                        src={toImageUrl(hotel.image)}
                        alt={hotel.name}
                        fill
                        sizes="(max-width: 768px) 100vw, 33vw"
                        className="object-cover transition-transform duration-500 group-hover:scale-105"
                      />

                      <div className="absolute top-3 left-3 bg-white/90 text-xs px-2 py-1 rounded-md font-semibold">
                        {hotel.brand || "Hotel"}
                      </div>
                    </div>

                    {/* CONTENT */}
                    <div className="p-4">
                      <h2 className="font-semibold text-slate-900 line-clamp-1">
                        {hotel.name}
                      </h2>

                      <div className="mt-1 flex items-center gap-2 text-sm text-slate-500">
                        <MapPin size={14} /> {hotel.city}
                      </div>

                      <div className="mt-2 flex items-center gap-2">
                        <div className="flex items-center gap-1 bg-amber-50 px-2 py-1 rounded">
                          <Star
                            size={12}
                            className="fill-amber-400 text-amber-400"
                          />
                          <span className="text-xs font-semibold text-amber-600">
                            {hotel.rating.toFixed(1)}
                          </span>
                        </div>

                        <span className="text-xs text-slate-400">
                          Excellent stay
                        </span>
                      </div>

                      <div className="mt-4 flex items-end justify-between">
                        <div>
                          <p className="text-xs text-slate-400">Per night</p>
                          <p className="text-lg font-bold text-[#c4a661]">
                            Rp {hotel.priceFrom.toLocaleString("id-ID")}
                          </p>
                        </div>

                        <Link
                          href={`/hotel/${hotel.slug}${
                            bookingQuery ? `?${bookingQuery}` : ""
                          }`}
                        >
                          <button className="bg-[#1a1f3c] text-white px-4 py-2 rounded-lg text-sm hover:bg-[#2a3160] transition">
                            View
                          </button>
                        </Link>
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* EMPTY */}
          {!isLoading && hotels.length === 0 && (
            <div className="text-center text-slate-500 py-20">
              No hotels found
            </div>
          )}
        </div>
      </main>

      <Footer />
    </>
  );
}
