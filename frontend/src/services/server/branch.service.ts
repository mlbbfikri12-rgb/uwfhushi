import type { PublicBanner, PublicBlog, PublicHomeResponse } from "@/types/home";


const API_URL = process.env.NEXT_PUBLIC_API_URL;

async function fetcher<T>(
  endpoint: string,
  options?: RequestInit
): Promise<T> {
  const res = await fetch(`${API_URL}${endpoint}`, {
    ...options,

    next: {
      revalidate: 60 * 10,
    },
  });

  if (!res.ok) {
    throw new Error(`Failed to fetch: ${endpoint}`);
  }

  return res.json();
}

export async function getPublicHome() {
  return fetcher<PublicHomeResponse>("/api/public/home");
}

export async function getPublicBanners() {
  return fetcher<PublicBanner[]>("/api/public/banners");
}

export async function getPublicBlogs() {
  return fetcher<PublicBlog[]>("/api/public/blogs");
}

import { PublicHotelSearchResponse } from "@/types/hotel-search";

export async function searchPublicHotels(params: {
  q?: string;
  checkIn?: string;
  checkOut?: string;
  totalRooms?: number;
  cityId?: string;
  minPrice?: number;
  maxPrice?: number;
  stars?: number[];
  brands?: string[];
  brandNames?: string[];
}): Promise<PublicHotelSearchResponse> {
  const query = new URLSearchParams();

  // =========================
  // BUILD QUERY CLEAN
  // =========================

  Object.entries(params).forEach(([key, value]) => {
    if (value === undefined || value === null) return;

    if (Array.isArray(value)) {
      if (value.length === 0) return;
      query.set(key, value.join(","));
    } else {
      query.set(key, String(value));
    }
  });

  const url = `${process.env.NEXT_PUBLIC_API_URL}/api/public/hotels/search?${query.toString()}`;

  const res = await fetch(url, {
    // 🔥 ISR SUPPORT
    next: {
      revalidate: 600, // 10 menit
    },
  });

  if (!res.ok) {
    throw new Error("Failed to fetch hotels");
  }

  return res.json();
}