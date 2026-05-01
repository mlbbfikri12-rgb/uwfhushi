export type RatePlan = {
  id: string;
  name: string;
  price: number;

  benefits?: string;
  terms?: string;

  isRefundable?: boolean;
  includesBreakfast?: boolean;
};

export type RoomType = {
  id: string;
  name: string;
  description?: string;

  image: string;

  capacity: number;
  bedType: string;
  size: number;

  facilities: string[];

  basePrice: number;

  ratePlans: RatePlan[];
};

export type RoomImage = {
  id: string;
  url: string;
  format: string;
};

export type Room = {
  id: string;
  roomNumber: string;
  status?: "available" | "occupied" | "maintenance";
  roomType: RoomType;
};

export type BookingPayload = {
  roomTypeId: string;
  ratePlanId: string;
  checkIn: string;
  checkOut: string;
  totalRooms: number;
};
