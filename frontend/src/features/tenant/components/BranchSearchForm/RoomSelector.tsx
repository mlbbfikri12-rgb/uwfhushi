"use client";

import { BedDouble } from "lucide-react";

type Props = {
  totalRooms: number;

  setTotalRooms: React.Dispatch<React.SetStateAction<number>>;
};

export function RoomSelector({ totalRooms, setTotalRooms }: Props) {
  return (
    <div className="flex-1 px-5 py-4 border-b lg:border-b-0 lg:border-r border-slate-200 flex items-start gap-3">
      <BedDouble className="shrink-0 text-[#c4a661] mt-0.5" size={18} />

      <div>
        <p className="text-[10px] font-bold uppercase tracking-widest text-slate-400 mb-1.5">
          Room
        </p>

        <div className="flex items-center gap-2.5 mt-1">
          <button
            type="button"
            onClick={() => setTotalRooms((prev) => Math.max(1, prev - 1))}
            className="w-7 h-7 rounded-full border border-slate-300 flex items-center justify-center text-slate-600 hover:border-[#1a1f3c] hover:text-[#1a1f3c] transition-colors text-lg leading-none"
          >
            −
          </button>

          <span className="text-base font-bold text-slate-800 min-w-[20px] text-center">
            {totalRooms}
          </span>

          <button
            type="button"
            onClick={() => setTotalRooms((prev) => Math.min(10, prev + 1))}
            className="w-7 h-7 rounded-full border border-slate-300 flex items-center justify-center text-slate-600 hover:border-[#1a1f3c] hover:text-[#1a1f3c] transition-colors text-lg leading-none"
          >
            +
          </button>
        </div>

        <p className="text-xs text-slate-400 mt-1.5">
          {totalRooms === 1 ? "Room" : "Rooms"}
        </p>
      </div>
    </div>
  );
}
