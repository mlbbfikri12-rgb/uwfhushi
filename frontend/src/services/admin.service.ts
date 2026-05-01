import { adminApi } from "@/lib/admin-api";
import { useBranchStore } from "@/store/useBranchStore";
import type { PublicBranch } from "@/types/branch";
import type { Room } from "@/types/room";

export type StaffRow = {
  id: string;
  name: string;
  email: string;
  role: "SUPER_ADMIN" | "SPV" | "FO";
  isActive: boolean;
};

export type CityRow = {
  id: string;
  name: string;
  isActive: boolean;
};

export type BrandRow = {
  id: string;
  name: string;
  logoUrl?: string | null;
  isActive: boolean;
};

export type FacilityRow = {
  id: string;
  name: string;
  icon?: string | null;
  isActive: boolean;
};

export type HotelRow = {
  id: string;
  name: string;
  slug: string;
  branchCode: string;
  cityId: string;
  cityName: string;
  brandId?: string | null;
  brandName?: string | null;
  address: string;
  description: string;
  starRating: number;
  latitude: number;
  longitude: number;
  isActive: boolean;
  images: { id: string; url: string; isPrimary: boolean; sortOrder: number }[];
  facilities: { facilityId: string; name: string; icon?: string | null }[];
  nearbyPlaces: { id: string; name: string; distance: string }[];
};

export type BannerRow = {
  id: string;
  title: string;
  subtitle: string;
  imageUrl: string;
  linkUrl: string;
  sortOrder: number;
};

export async function getBranches() {
  const { data } = await adminApi.get<PublicBranch[]>("/api/branches");
  return data;
}

export async function getStaffRows() {
  const { data } = await adminApi.get<StaffRow[]>("/api/staff");
  return data;
}

export async function getActiveBanners() {
  const { data } = await adminApi.get<BannerRow[]>("/api/banners/active");
  return data;
}

export async function getRoomsForBranch(branchCode: string) {
  const { data } = await adminApi.get<Room[]>("/api/rooms", {
    headers: {
      "X-Branch-Code": branchCode,
    },
  });
  return data;
}

export async function updateRoomStatus(
  roomId: string,
  status: string
) {
  const branch = useBranchStore.getState().activeBranch;
  const { data } = await adminApi.patch(
    `/api/rooms/${roomId}/status`,
    { status },
    {
      headers: {
        "X-Branch-Code": branch || "",
      },
    }
  );

  return data;
}

export async function getCities(q?: string) {
  const { data } = await adminApi.get<CityRow[]>("/api/admin/cities", { params: { q } });
  return data;
}

export async function createCity(payload: { name: string }) {
  const { data } = await adminApi.post<CityRow>("/api/admin/cities", payload);
  return data;
}

export async function updateCity(id: string, payload: { name: string }) {
  const { data } = await adminApi.put<CityRow>(`/api/admin/cities/${id}`, payload);
  return data;
}

export async function deleteCity(id: string) {
  await adminApi.delete(`/api/admin/cities/${id}`);
}

export async function getBrands(q?: string) {
  const { data } = await adminApi.get<BrandRow[]>("/api/admin/brands", { params: { q } });
  return data;
}

export async function createBrand(payload: { name: string; logoUrl?: string | null }) {
  const { data } = await adminApi.post<BrandRow>("/api/admin/brands", payload);
  return data;
}

export async function updateBrand(id: string, payload: { name: string; logoUrl?: string | null }) {
  const { data } = await adminApi.put<BrandRow>(`/api/admin/brands/${id}`, payload);
  return data;
}

export async function deleteBrand(id: string) {
  await adminApi.delete(`/api/admin/brands/${id}`);
}

export async function getFacilities(q?: string) {
  const { data } = await adminApi.get<FacilityRow[]>("/api/admin/facilities", { params: { q } });
  return data;
}

export async function createFacility(payload: { name: string; icon?: string | null }) {
  const { data } = await adminApi.post<FacilityRow>("/api/admin/facilities", payload);
  return data;
}

export async function updateFacility(id: string, payload: { name: string; icon?: string | null }) {
  const { data } = await adminApi.put<FacilityRow>(`/api/admin/facilities/${id}`, payload);
  return data;
}

export async function deleteFacility(id: string) {
  await adminApi.delete(`/api/admin/facilities/${id}`);
}

export async function getHotels(q?: string) {
  const { data } = await adminApi.get<HotelRow[]>("/api/admin/hotels", { params: { q } });
  return data;
}

export async function getHotelById(id: string) {
  const { data } = await adminApi.get<HotelRow>(`/api/admin/hotels/${id}`);
  return data;
}

export async function createHotel(payload: {
  name: string;
  slug: string;
  branchCode: string;
  cityId: string;
  brandId?: string | null;
  address: string;
  description: string;
  starRating: number;
  latitude: number;
  longitude: number;
  isActive: boolean;
}) {
  const { data } = await adminApi.post<HotelRow>("/api/admin/hotels", payload);
  return data;
}

export async function updateHotel(
  id: string,
  payload: {
    name: string;
    slug: string;
    branchCode: string;
    cityId: string;
    brandId?: string | null;
    address: string;
    description: string;
    starRating: number;
    latitude: number;
    longitude: number;
    isActive: boolean;
  }
) {
  const { data } = await adminApi.put<HotelRow>(`/api/admin/hotels/${id}`, payload);
  return data;
}

export async function deleteHotel(id: string) {
  await adminApi.delete(`/api/admin/hotels/${id}`);
}

export async function addHotelImage(
  hotelId: string,
  payload: { url: string; isPrimary: boolean; sortOrder: number }
) {
  const { data } = await adminApi.post(`/api/admin/hotels/${hotelId}/images`, payload);
  return data;
}

export async function deleteHotelImage(hotelId: string, imageId: string) {
  await adminApi.delete(`/api/admin/hotels/${hotelId}/images/${imageId}`);
}

export async function setHotelFacilities(hotelId: string, facilityIds: string[]) {
  const { data } = await adminApi.post(`/api/admin/hotels/${hotelId}/facilities`, {
    facilityIds,
  });
  return data;
}

export async function addNearbyPlace(hotelId: string, payload: { name: string; distance: string }) {
  const { data } = await adminApi.post(`/api/admin/hotels/${hotelId}/nearby-places`, payload);
  return data;
}

export async function deleteNearbyPlace(hotelId: string, nearbyPlaceId: string) {
  await adminApi.delete(`/api/admin/hotels/${hotelId}/nearby-places/${nearbyPlaceId}`);
}

export async function uploadImage(file: File, folder: string) {
  const formData = new FormData();
  formData.append("file", file);
  formData.append("folder", folder);
  const { data } = await adminApi.post<{ url: string }>("/api/uploads/images", formData, {
    headers: { "Content-Type": "multipart/form-data" },
  });
  return data;
}
