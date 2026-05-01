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
