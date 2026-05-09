"use client";

import { Search } from "lucide-react";

type Props = {
  hideSearchButton?: boolean;
};

export function SearchButton({ hideSearchButton = false }: Props) {
  if (hideSearchButton) {
    return null;
  }

  return (
    <div className="px-4 py-4 flex items-center justify-center lg:justify-end shrink-0">
      <button
        type="submit"
        className="flex items-center gap-2 bg-[#1a1f3c] hover:bg-[#252c52] active:scale-95 text-white text-sm font-bold px-7 py-4 rounded-xl transition-all whitespace-nowrap w-full lg:w-auto justify-center"
      >
        <Search size={16} />
        Search Hotels
      </button>
    </div>
  );
}
