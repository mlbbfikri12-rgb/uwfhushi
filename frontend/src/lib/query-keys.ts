export const queryKeys = {
  customer: {
    me: ["customer", "me"] as const,
  },
  hotel: {
    detail: (slug: string) => ["hotel", "detail", slug] as const,
    pricing: (slug: string, checkIn: string, checkOut: string) =>
      ["hotel", "pricing", slug, checkIn, checkOut] as const,
    roomDetail: (slug: string, roomTypeId: string) =>
      ["hotel", "room-detail", slug, roomTypeId] as const,
  },
  order: {
    current: (branchCode?: string | null) =>
      ["order", "current", branchCode ?? "none"] as const,
  },
  search: {
    hotels: (params: string) => ["search", "hotels", params] as const,
  },
};
