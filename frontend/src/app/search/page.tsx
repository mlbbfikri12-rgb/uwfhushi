import type { Metadata } from "next";
import SearchPageClient from "./search-page-client";

// 🔥 HARUS SERVER SERVICE (bukan client axios)
import { searchPublicHotels } from "@/services/server/branch.service";

import type { PublicHotelSearchResponse } from "@/types/hotel-search";

export const metadata: Metadata = {
  title: "Search Hotels",
  description: "Find hotels by city, date, and price.",
};

// ISR 10 menit
export const revalidate = 600;

type PageProps = {
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

export default async function SearchPage({ searchParams }: PageProps) {
  const q = asString(searchParams.q);
  const checkIn = asString(searchParams.checkIn);
  const checkOut = asString(searchParams.checkOut);
  const totalRooms = asNumber(searchParams.total_rooms, 1);

  let initialData: PublicHotelSearchResponse | undefined;

  // 🔥 ONLY FETCH IF QUERY EXISTS
  if (q) {
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

    initialData = await searchPublicHotels(params);
  }

  return (
    <SearchPageClient searchParams={searchParams} initialData={initialData} />
  );
}
