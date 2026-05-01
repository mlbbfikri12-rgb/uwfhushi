import type { RoomType } from "@/types/room";

export type HotelFullResponse = {
  hotel: {
    hotelId: string;
    branchCode: string;
    name: string;
    address: string;
    city: string;
    brand: string;
    rating: number;
    reviewCount: number;
    description: string;
    latitude: number;
    longitude: number;
  };
  images: { url: string; type: string; sortOrder: number }[];
  facilities: { name: string; icon: string }[];
  nearby: { name: string; distanceKm: number }[];
  roomTypes: RoomType[];
};
