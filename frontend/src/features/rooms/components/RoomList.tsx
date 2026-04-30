"use client";

import { useMemo } from "react";
import { useRoomsQuery } from "@/features/rooms/hooks/useRoomsQuery";
import { useBranchStore } from "@/store/useBranchStore";
import { Spinner } from "./ui/Spinner";
import { enrichRoom } from "@/utils/enrichRooom";
import { RoomCard } from "./ui/RoomCard";

type Props = {
  branch: string;
  search: any;
};

export function RoomList({ branch, search }: Props) {
  const activeBranch = useBranchStore((state) => state.activeBranch);
  const isReady = activeBranch === branch.toUpperCase();

  const stableSearch = useMemo(
    () => ({
      checkIn: search.checkIn,
      checkOut: search.checkOut,
      adultCount: search.adultCount,
      childCount: search.childCount,
    }),
    [search.checkIn, search.checkOut, search.adultCount, search.childCount],
  );

  const { data, isLoading, isError, error } = useRoomsQuery(
    branch,
    isReady,
    stableSearch,
  );

  if (!isReady) return <Spinner />;
  if (isLoading) return <Spinner />;

  if (isError) {
    return (
      <div className="rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-700">
        {(error as Error)?.message ?? "Gagal mengambil data kamar"}
      </div>
    );
  }

  if (!data?.length) {
    return (
      <div className="rounded-lg border p-4 text-sm">
        Tidak ada kamar tersedia.
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {data.map((room) => (
        <RoomCard
          key={room.id}
          room={enrichRoom(room)}
          branch={branch}
          search={search}
        />
      ))}
    </div>
  );
}
