// hooks/useHotelSearch.ts
"use client";

import { useRouter, useSearchParams } from "next/navigation";

export function useHotelSearch() {
    const router = useRouter();
    const params = useSearchParams();

    const checkIn = params.get("checkIn");
    const checkOut = params.get("checkOut");
    const rooms = Number(params.get("rooms") || 1);

    const setSearch = (next: {
        checkIn?: string;
        checkOut?: string;
        rooms?: number;
    }) => {
        const newParams = new URLSearchParams(params.toString());

        if (next.checkIn) newParams.set("checkIn", next.checkIn);
        if (next.checkOut) newParams.set("checkOut", next.checkOut);
        if (next.rooms) newParams.set("rooms", String(next.rooms));

        router.push(`?${newParams.toString()}`);
    };

    return {
        checkIn,
        checkOut,
        rooms,
        setSearch,
    };
}