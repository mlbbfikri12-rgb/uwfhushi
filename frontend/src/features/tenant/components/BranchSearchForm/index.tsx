"use client";

import { addDays, format } from "date-fns";
import { useRouter, useSearchParams } from "next/navigation";
import { useEffect, useState } from "react";
import { toast } from "sonner";

import { searchPublicHotels } from "@/services/client/public.client";

import { DestinationInput } from "./DestinationInput";
import { CalendarPicker } from "./CalendarPicker";
import { RoomSelector } from "./RoomSelector";
import { SearchButton } from "./SearchButton";

type Props = {
  branch?: string;
  initialKeyword?: string;
  hideSearchButton?: boolean;
};

export default function BranchSearchForm({
  branch,
  initialKeyword,
  hideSearchButton = false,
}: Props) {
  const router = useRouter();
  const searchParams = useSearchParams();

  const [keyword, setKeyword] = useState("");

  const [dateRange, setDateRange] = useState({
    from: new Date(),
    to: addDays(new Date(), 1),
  });

  const [totalRooms, setTotalRooms] = useState(1);

  // sync dari URL params
  useEffect(() => {
    const checkIn =
      searchParams.get("checkIn") ?? searchParams.get("checkin_date");

    const checkOut = searchParams.get("checkOut");

    const rooms = searchParams.get("total_rooms") ?? searchParams.get("rooms");

    if (checkIn && checkOut) {
      const from = new Date(checkIn);
      const to = new Date(checkOut);

      if (!isNaN(from.getTime()) && !isNaN(to.getTime())) {
        setDateRange({
          from,
          to,
        });
      }
    }

    if (rooms) {
      setTotalRooms(Number(rooms));
    }
  }, [searchParams]);

  // sync initial props
  useEffect(() => {
    if (branch) {
      setKeyword(branch);
    }

    if (initialKeyword) {
      setKeyword(initialKeyword);
    }
  }, [branch, initialKeyword]);

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!keyword.trim()) {
      toast.error("Masukkan kota atau hotel");

      return;
    }

    const checkIn = format(dateRange.from, "yyyy-MM-dd");

    const checkOut = format(dateRange.to, "yyyy-MM-dd");

    const result = await searchPublicHotels({
      q: keyword.trim(),
      checkIn,
      checkOut,
      totalRooms,
    });

    if (result.type === "hotel" && result.hotels.length > 0) {
      router.push(
        `/hotel/${result.hotels[0].slug}?checkIn=${checkIn}&checkOut=${checkOut}&total_rooms=${totalRooms}`,
      );

      return;
    }

    router.push(
      `/search?q=${encodeURIComponent(
        keyword.trim(),
      )}&checkIn=${checkIn}&checkOut=${checkOut}&total_rooms=${totalRooms}`,
    );
  };

  return (
    <form onSubmit={onSubmit} className="w-full">
      <div className="flex flex-col lg:flex-row rounded-2xl overflow-visible shadow-lg border border-slate-200 bg-white">
        <DestinationInput keyword={keyword} setKeyword={setKeyword} />

        <CalendarPicker dateRange={dateRange} setDateRange={setDateRange} />

        <RoomSelector totalRooms={totalRooms} setTotalRooms={setTotalRooms} />

        <SearchButton hideSearchButton={hideSearchButton} />
      </div>
    </form>
  );
}
