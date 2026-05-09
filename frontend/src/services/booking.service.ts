import { api } from "@/lib/api";
import type {
  BookingResponse,
  CheckoutOrderPayload,
  CheckoutOrderResponse,
  CreateBookingPayload,
  GuestCheckoutPayload,
  GuestCheckoutResponse,
} from "@/types/booking";

export async function createBooking(payload: CreateBookingPayload) {
  const { data } = await api.post<BookingResponse>("/api/booking", payload);
  return data;
}

export async function checkoutFromOrder(payload: CheckoutOrderPayload) {
  const { data } = await api.post<CheckoutOrderResponse>("/api/booking/checkout-order", payload);
  return data;
}

export async function guestCheckout(payload: GuestCheckoutPayload) {
  const { data } = await api.post<GuestCheckoutResponse>("/api/guest/checkout", payload);
  return data;
}
