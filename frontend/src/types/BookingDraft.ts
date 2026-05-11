// export type BookingDraftItem = {
//     roomTypeId: string;
//     ratePlanId: string;
//     roomId?: string;

//     roomTypeName?: string;
//     ratePlanName?: string;

//     slug: string;

//     checkIn: string;
//     checkOut: string;
//     imageUrl?: string;

//     totalRooms: number;
//     price?: number;
// };

// export type BookingDraft = {
//     items: BookingDraftItem[];
// };

export type BookingDraft = {
    version: 1;
    slug: string;
    branchCode?: string;
    checkIn: string;
    checkOut: string;
    items: BookingDraftItem[];
};

export type BookingDraftItem = {
    key: string;
    roomTypeId: string;
    ratePlanId: string;

    roomTypeName?: string;
    ratePlanName?: string;

    imageUrl?: string;

    qty: number;
    price?: number;
    MaxAdults?: number;
    MaxChildren?: number;

    isBreakFast?: boolean;
    isRefundable?: boolean;
};

export type SelectedItem = {
    roomTypeId: string;
    ratePlanId: string;
    qty: number;
};
