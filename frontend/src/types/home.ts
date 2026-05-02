import type { PublicHotelListItem } from "@/types/hotel-search";

export type PublicBanner = {
  id: string;
  title: string;
  subtitle: string;
  imageUrl: string;
  linkUrl: string;
  sortOrder: number;
};

export type PublicDestination = {
  city: string;
  minPrice: number;
};

export type PublicBlog = {
  id: string;
  title: string;
  content: string;
  imageUrl: string;
  createdAt: string;
};

export type PublicHomeResponse = {
  heroBanners: PublicBanner[];
  popularHotels: PublicHotelListItem[];
  destinations: PublicDestination[];
  blogs: PublicBlog[];
};
