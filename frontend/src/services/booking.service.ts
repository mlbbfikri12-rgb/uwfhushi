import { api } from "@/lib/api";
import type { BookingResponse, CreateBookingPayload } from "@/types/booking";

export async function createBooking(payload: CreateBookingPayload) {
  const { data } = await api.post<BookingResponse>("/api/booking", payload);
  return data;
}
