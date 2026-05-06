export type BookingDraftItem = {
    roomTypeId: string;
    ratePlanId: string;
    roomId?: string;

    roomTypeName?: string;
    ratePlanName?: string;

    slug: string;

    checkIn: string;
    checkOut: string;
    imageUrl?: string;

    totalRooms: number;
    price?: number;
};

export type BookingDraft = {
    items: BookingDraftItem[];
};