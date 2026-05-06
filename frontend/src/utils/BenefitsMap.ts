import * as Icons from "lucide-react";
import type { LucideIcon } from "lucide-react";

export type BenefitCode =
    | "BREAKFAST"
    | "ONLINE_PAYMENT"
    | "HOTEL_PAYMENT"
    | "CANCELABLE"
    | "NON_REFUNDABLE";

export const BENEFIT_MAP: Record<
    BenefitCode,
    {
        label: string;
        icon: LucideIcon;
        color?: string;
    }
> = {
    ONLINE_PAYMENT: {
        label: "Online Payment",
        icon: Icons.CreditCard,
    },
    HOTEL_PAYMENT: {
        label: "Pay at Hotel",
        icon: Icons.Building2,
    },
    NON_REFUNDABLE: {
        label: "Non-refundable",
        icon: Icons.XCircle,
        color: "text-red-500",
    },
    CANCELABLE: {
        label: "Free Cancellation",
        icon: Icons.CheckCircle,
        color: "text-green-500",
    },
    BREAKFAST: {
        label: "Free Breakfast",
        icon: Icons.Coffee,
    },
};