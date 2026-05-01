import { publicApi } from "@/lib/public-api";
import type { HotelFullResponse } from "@/types/hotel";

export async function getHotelFull(params: {
  branch: string;
  checkIn: string;
  checkOut: string;
  adult: number;
  child: number;
}) {
  const { data } = await publicApi.get<HotelFullResponse>(`/api/hotel/${params.branch}/full`, {
    params: {
      checkIn: params.checkIn,
      checkOut: params.checkOut,
      adult: params.adult,
      child: params.child,
    },
  });

  return data;
}
