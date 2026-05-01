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

export type CheckoutOrderPayload = {
  adultCount: number;
  childCount: number;
  paymentMethod?: string;
  notes?: string;
};

export type CheckoutOrderResponse = {
  message: string;
  orderDraftId: string;
  grandTotal: number;
  bookings: {
    bookingId: string;
    bookingCode: string;
    roomId: string;
    roomNumber: string;
    roomTypeName: string;
    ratePlanName: string;
    checkIn: string;
    checkOut: string;
    totalPrice: number;
  }[];
};
