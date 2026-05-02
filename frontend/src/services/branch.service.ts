import { publicApi } from "@/lib/public-api";
import type { PublicBranch } from "@/types/branch";
import type { PublicBanner, PublicBlog, PublicHomeResponse } from "@/types/home";
import type { PublicHotelSearchResponse } from "@/types/hotel-search";

export async function searchBranches(keyword: string) {
  const { data } = await publicApi.get<PublicBranch[]>("/api/public/branches", {
    params: {
      q: keyword,
      limit: 10,
    },
  });
  return data;
}

export async function searchPublicHotels(params: {
  q: string;
  checkIn: string;
  checkOut: string;
  totalRooms: number;
  cityId?: string;
  minPrice?: number;
  maxPrice?: number;
  stars?: number[];
  brands?: string[];
  brandNames?: string[];
}) {
  const { data } = await publicApi.get<PublicHotelSearchResponse>("/api/public/hotels/search", {
    params,
  });

  return data;
}

export async function getPublicBanners() {
  const { data } = await publicApi.get<PublicBanner[]>("/api/public/banners");
  return data;
}

export async function getPublicHome() {
  const { data } = await publicApi.get<PublicHomeResponse>("/api/public/home");
  return data;
}

export async function getPublicBlogs() {
  const { data } = await publicApi.get<PublicBlog[]>("/api/public/blogs");
  return data;
}
