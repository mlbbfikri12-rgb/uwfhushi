import { BenefitCode } from "@/utils/BenefitsMap";

export type Hotel = {
  id: string;
  name: string;
  city: string;

  description: string;
  latitude: number;
  longitude: number;
  branchCode: string;

  images: string[];
  facilities: Facility[];

  nearby: {
    name: string;
    distanceKm: number;
  }[];

  priceFrom: number;
  priceType: "estimate" | "exact";
};

export type Facility = {
  name: string;
  icon?: string | null;
};


export type UIRatePlan = {
  id: string;
  name: string;
  benefits?: BenefitCode[]; // ✅ FIX
  terms?: string;
  price: number;
};

export type UIRoomType = {
  id: string;
  name: string;
  description?: string;
  image: string;
  capacity: number;
  bedType: string;
  size: number;
  facilities: Facility[];
  ratePlans: UIRatePlan[];
};