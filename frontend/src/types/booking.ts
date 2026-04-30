export type CreateBookingPayload = {
  roomId: string;
  customerName?: string;
  customerEmail?: string;
  customerPhone?: string;
  checkIn: string;
  checkOut: string;
  adultCount: number;
  childCount: number;
  paymentMethod?: string;
  notes?: string;
};

export type BookingResponse = {
  message: string;
  bookingId: string;
  bookingCode: string;
  totalPrice: number;
};
