"use client";

import { useQuery } from "@tanstack/react-query";
import { searchAvailableRooms } from "@/services/room.service";
import type { AvailabilitySearchPayload } from "@/types/search";

export function useRoomsQuery(
  branch: string,
  enabled: boolean,
  payload: AvailabilitySearchPayload
) {
  return useQuery({
    queryKey: ["public-rooms", branch, payload],
    queryFn: () => searchAvailableRooms(payload),
    enabled: Boolean(branch) && enabled,
    staleTime: 60_000,
    gcTime: 5 * 60_000,
    retry: 1,
  });
}
