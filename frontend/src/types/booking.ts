export type CreateBookingPayload = {
  roomTypeId: string;
  ratePlanId: string;
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
  bookingGroupCode?: string;
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
  bookingGroupCode: string;
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

export type GuestCheckoutItemPayload = {
  roomTypeId: string;
  ratePlanId: string;
  checkIn: string;
  checkOut: string;
  totalRooms: number;
};

export type GuestCheckoutPayload = {
  customerName: string;
  customerEmail: string;
  customerPhone: string;
  adultCount: number;
  childCount: number;
  paymentMethod?: string;
  notes?: string;
  items: GuestCheckoutItemPayload[];
};

export type GuestCheckoutResponse = {
  message: string;
  bookingGroupCode: string;
  grandTotal: number;
  bookings: {
    bookingId: string;
    bookingCode: string;
    roomId?: string | null;
    roomNumber?: string | null;
    roomTypeId: string;
    roomTypeName: string;
    ratePlanName: string;
    checkIn: string;
    checkOut: string;
    totalPrice: number;
  }[];
};
