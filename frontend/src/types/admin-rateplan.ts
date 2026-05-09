import { BenefitCode } from "@/utils/BenefitsMap";

export type Branch = {
    id: string;
    code: string;
    name: string;
};
export type StaffRole = "SPV" | "SUPER_ADMIN" | "FO";

export type Staff = {
    staffId: string;
    role: StaffRole;
    allowedBranchIds: string[];
    allowedBranches: Branch[];
};

export type Room = {
    id: string;
    roomType: {
        id: string;
        name: string;
    } | null;
};

export type RoomType = {
    id: string;
    name: string;
};

export type PaymentType = "online" | "pay_at_hotel";

export type RatePlan = {
    id: string;

    name: string;
    price: number;

    paymentType: PaymentType;

    includesBreakfast: boolean;
    isRefundable: boolean;

    termsConditions: string;

    isActive: boolean;
};

export type CreateRatePlanPayload = {
    name: string;
    price: number;
    paymentType: string;
    terms?: string;
};

export type UpdateRatePlanPayload = CreateRatePlanPayload;

export type StaffRow = {
    id: string;
    name: string;
    email: string;
    role: "SUPER_ADMIN" | "SPV" | "FO";
    isActive: boolean;
};

export type CityRow = {
    id: string;
    name: string;
    isActive: boolean;
};

export type BrandRow = {
    id: string;
    name: string;
    logoUrl?: string | null;
    isActive: boolean;
};

export type FacilityRow = {
    id: string;
    name: string;
    icon?: string | null;
    isActive: boolean;
};

export type HotelRow = {
    id: string;
    name: string;
    slug: string;
    branchCode: string;
    cityId: string;
    cityName: string;
    brandId?: string | null;
    brandName?: string | null;
    address: string;
    description: string;
    starRating: number;
    latitude: number;
    longitude: number;
    isActive: boolean;
    images: { id: string; url: string; isPrimary: boolean; sortOrder: number }[];
    facilities: { facilityId: string; name: string; icon?: string | null }[];
    nearbyPlaces: { id: string; name: string; distance: string }[];
};

export type RatePlanAdminRow = {
    id: string;
    roomTypeId: string;
    name: string;
    price: number;
    includesBreakfast: boolean;
    isRefundable: boolean;
    paymentType: PaymentType;
    termsConditions: string;
    isActive: boolean;
};

export type BannerRow = {
    id: string;
    title: string;
    subtitle: string;
    imageUrl: string;
    linkUrl: string;
    sortOrder: number;
};

export type PricingRatePlan = {
    id: string;
    name: string;
    price: number;
    benefits?: BenefitCode[]; // ✅ FIX
    termsPreview: string;

    isBreakFast: boolean;
    isRefundable: boolean;
};

export type PricingRoom = {
    roomTypeId: string;
    name: string;
    image?: string;
    description?: string;
    capacity: number;
    bedType: string;
    size: number;
    maxAdults?: number;
    maxChildren?: number;
    facilities?: string[];
    ratePlans: PricingRatePlan[];
};

export type RatePlanForm = {
    name: string;
    price: number;
    includesBreakfast: boolean;
    isRefundable: boolean;
    paymentType: "online" | "pay_at_hotel";
    termsConditions: string;
    isActive: boolean;
};

export type RatePlanAdmin = RatePlanForm & {
    id: string;
};