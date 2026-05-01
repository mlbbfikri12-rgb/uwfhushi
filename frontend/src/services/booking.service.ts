import { api } from "@/lib/api";
import type {
  BookingResponse,
  CheckoutOrderPayload,
  CheckoutOrderResponse,
  CreateBookingPayload,
} from "@/types/booking";

export async function createBooking(payload: CreateBookingPayload) {
  const { data } = await api.post<BookingResponse>("/api/booking", payload);
  return data;
}

export async function checkoutFromOrder(payload: CheckoutOrderPayload) {
  const { data } = await api.post<CheckoutOrderResponse>("/api/booking/checkout-order", payload);
  return data;
}
