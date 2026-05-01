"use client";

import { useRouter } from "next/navigation";
import type { Room } from "@/types/room";
import Image from "next/image";

type SearchParams = {
  checkIn: string;
  checkOut: string;
  adultCount: number;
  childCount: number;
};

type RoomCardProps = {
  room: Room;
  branch: string;
  search: SearchParams;
  nights?: number;
};

export function RoomCard({ room, branch, search }: RoomCardProps) {
  const router = useRouter();
  const status = room.status ?? "available";

  return (
    <div className="flex flex-col gap-6 rounded-xl border bg-white p-5 shadow-sm transition hover:shadow-md lg:flex-row">
      <div className="h-48 w-full overflow-hidden rounded-lg lg:w-1/3">
        <Image
          src={room.roomType.image}
          alt={room.roomType.name}
          width={400}
          height={300}
          className="h-full w-full object-cover"
        />
      </div>

      <div className="flex-1">
        <h2 className="text-xl font-semibold text-slate-900">
          {room.roomType.name}
        </h2>
        <p className="mt-1 text-sm text-slate-500">Room #{room.roomNumber}</p>

        <div className="mt-3 flex flex-wrap gap-2 text-xs text-slate-600">
          <span>{room.roomType.capacity} Tamu</span>
          <span>{room.roomType.bedType}</span>
          <span>{room.roomType.size} m2</span>
        </div>

        <div className="mt-2 flex flex-wrap gap-2 text-xs text-slate-500">
          {room.roomType.facilities.slice(0, 3).map((facility) => (
            <span key={facility} className="rounded bg-slate-100 px-2 py-1">
              {facility}
            </span>
          ))}
        </div>

        <div className="mt-3">
          <span className="inline-block rounded bg-green-100 px-2 py-1 text-xs text-green-700">
            {status === "available" ? "Available" : status}
          </span>
        </div>
      </div>

      <div className="flex w-full flex-col justify-between lg:w-[260px]">
        <div>
          <p className="text-xl font-bold text-[#c4a661]">
            Rp {room.roomType.basePrice.toLocaleString("id-ID")}
          </p>
          <p className="text-xs text-slate-500">per malam</p>
        </div>

        <button
          onClick={() => {
            router.push(
              `/booking?roomId=${room.id}&branch=${branch}&checkIn=${search.checkIn}&checkOut=${search.checkOut}&adult=${search.adultCount}&child=${search.childCount}`,
            );
          }}
          className="mt-4 w-full rounded-lg bg-[#1a1f3c] py-2 text-sm font-semibold text-white hover:bg-[#2a2f4c]"
        >
          Book Sekarang
        </button>
      </div>
    </div>
  );
}
