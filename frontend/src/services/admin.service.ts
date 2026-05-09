import { adminApi } from "@/lib/admin-api";
import { useBranchStore } from "@/store/useBranchStore";
import { BannerRow, BrandRow, CityRow, FacilityRow, HotelRow, RatePlanAdminRow, StaffRow } from "@/types/admin-rateplan";
import { AdminRoom } from "@/types/admin-room";
import type { PublicBranch } from "@/types/branch";


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

export async function getRoomsForBranch(branchCode: string): Promise<AdminRoom[]> {
  const apiUrl = process.env.NEXT_PUBLIC_API_URL
  const res = await fetch(`${apiUrl}/api/rooms`, {
    credentials: "include",
    headers: {
      "X-Branch-Code": branchCode,
    },
  });

  if (!res.ok) throw new Error("Failed to fetch rooms");

  return res.json();
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

export async function getRatePlansByRoomType(roomTypeId: string) {
  const { data } = await adminApi.get<RatePlanAdminRow[]>(`/api/admin/room-types/${roomTypeId}/rate-plans`);
  return data;
}

export async function createRatePlan(roomTypeId: string, payload: Omit<RatePlanAdminRow, "id" | "roomTypeId">) {
  const { data } = await adminApi.post<RatePlanAdminRow>(`/api/admin/room-types/${roomTypeId}/rate-plans`, payload);
  return data;
}

export async function updateRatePlan(id: string, payload: Omit<RatePlanAdminRow, "id" | "roomTypeId">) {
  const { data } = await adminApi.put<RatePlanAdminRow>(`/api/admin/rate-plans/${id}`, payload);
  return data;
}

export async function deleteRatePlan(id: string) {
  await adminApi.delete(`/api/admin/rate-plans/${id}`);
}
