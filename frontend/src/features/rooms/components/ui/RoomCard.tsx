"use client";

import { useRouter } from "next/navigation";

export function RoomCard({ room, branch, search }: any) {
  const router = useRouter();

  return (
    <div className="flex flex-col lg:flex-row gap-6 rounded-xl border bg-white p-5 shadow-sm hover:shadow-md transition">
      {/* IMAGE */}
      <div className="w-full lg:w-1/3 h-48 rounded-lg overflow-hidden">
        <img src={room.roomType.image} className="w-full h-full object-cover" />
      </div>

      {/* INFO */}
      <div className="flex-1">
        <h2 className="text-xl font-semibold text-slate-900">
          {room.roomType.name}
        </h2>

        <p className="text-sm text-slate-500 mt-1">Room #{room.roomNumber}</p>

        <div className="flex flex-wrap gap-2 mt-3 text-xs text-slate-600">
          <span>👤 {room.roomType.capacity} Tamu</span>
          <span>🛏 {room.roomType.bedType}</span>
          <span>📐 {room.roomType.size} m²</span>
        </div>

        <div className="flex flex-wrap gap-2 mt-2 text-xs text-slate-500">
          {room.roomType.facilities.slice(0, 3).map((f: string) => (
            <span key={f} className="rounded bg-slate-100 px-2 py-1">
              {f}
            </span>
          ))}
        </div>

        <div className="mt-3">
          <span className="inline-block rounded bg-green-100 px-2 py-1 text-xs text-green-700">
            Best Deal
          </span>
        </div>
      </div>

      {/* BOOK */}
      <div className="w-full lg:w-[260px] flex flex-col justify-between">
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
