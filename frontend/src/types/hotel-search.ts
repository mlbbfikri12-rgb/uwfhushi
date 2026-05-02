export type PublicHotelListItem = {
  hotelId: string;
  slug: string;
  branchCode: string;
  name: string;
  city: string;
  rating: number;
  priceFrom: number;
  image: string;
  brand: string;
  isCityMatch: boolean;
};

export type PublicHotelSearchResponse = {
  type: "city" | "hotel";
  hotels: PublicHotelListItem[];
};
