export type RoomType = {
  id: string;
  name: string;
  description: string;
  basePrice: number;
  maxAdults: number;
  maxChildren: number;
};

export type RoomImage = {
  id: string;
  url: string;
  format: string;
};

export type Room = {
  id: string;
  roomNumber: string;
  status: "available" | "maintenance" | "occupied";
  roomType: RoomType;
  images: RoomImage[];
};
