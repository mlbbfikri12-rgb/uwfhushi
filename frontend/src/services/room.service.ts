import { api } from "@/lib/api";
import type { Room } from "@/types/room";
import type { AvailabilitySearchPayload } from "@/types/search";

export async function getRooms() {
  const { data } = await api.get<Room[]>("/api/rooms");
  return data;
}

export async function searchAvailableRooms(payload: AvailabilitySearchPayload) {
  const { data } = await api.post<Room[]>("/api/public/rooms/availability/search", payload);
  return data;
}