export type AddOrderItemPayload = {
  roomTypeId: string;
  ratePlanId: string;
  checkIn: string;
  checkOut: string;
  totalRooms: number;
};

export type OrderCurrent = {
  orderDraftId: string;
  items: {
    bedType: string;
    capacity: number;
    MaxAdults?: number;
    MaxChildren?: number;
    isBreakFast: boolean;
    isRefundable: boolean;
    image: string;
    id: string;
    roomTypeId: string;
    ratePlanId: string;
    roomTypeName: string;
    ratePlanName: string;
    checkIn: string;
    checkOut: string;
    totalRooms: number;
    pricePerNight: number;
    totalPrice: number;
  }[];
  grandTotal: number;
};
