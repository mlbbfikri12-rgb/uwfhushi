"use client";

import { Building2, MapPin } from "lucide-react";
import { useState } from "react";

import { useBranchSearchQuery } from "@/features/tenant/hooks/useBranchSearchQuery";
import type { PublicBranch } from "@/types/branch";

type Props = {
  keyword: string;
  setKeyword: React.Dispatch<React.SetStateAction<string>>;
};

export function DestinationInput({ keyword, setKeyword }: Props) {
  const [selectedBranch, setSelectedBranch] = useState<PublicBranch | null>(
    null,
  );

  const [isUserTyping, setIsUserTyping] = useState(false);

  const { data, isFetching } = useBranchSearchQuery(
    isUserTyping ? keyword : "",
  );

  const getIcon = (type: string) => {
    switch (type) {
      case "city":
        return <MapPin size={14} className="text-[#c4a661] shrink-0" />;

      case "hotel":
        return <Building2 size={14} className="text-slate-500 shrink-0" />;

      default:
        return <MapPin size={14} className="text-slate-400 shrink-0" />;
    }
  };

  return (
    <div className="relative flex-[2.5] min-w-0 px-5 py-4 border-b lg:border-b-0 lg:border-r border-slate-200">
      <div className="flex items-start gap-3">
        <MapPin className="shrink-0 text-[#c4a661] mt-0.5" size={18} />

        <div className="flex-1 min-w-0">
          <p className="text-[10px] font-bold uppercase tracking-widest text-slate-400 mb-1.5">
            Discover Hotel or Location
          </p>

          <input
            value={keyword}
            onChange={(e) => {
              setKeyword(e.target.value);

              setSelectedBranch(null);

              setIsUserTyping(true);
            }}
            placeholder="City, hotel name..."
            className="w-full text-sm font-medium text-slate-800 placeholder:text-slate-400 bg-transparent outline-none"
          />
        </div>
      </div>

      {isUserTyping &&
        !isFetching &&
        data &&
        data.length > 0 &&
        !selectedBranch &&
        keyword.length > 1 && (
          <ul className="absolute left-0 top-full mt-2 z-50 w-full bg-white border border-slate-200 rounded-xl shadow-2xl overflow-hidden">
            {data.map((b, index) => (
              <li key={index}>
                <button
                  type="button"
                  onClick={() => {
                    setSelectedBranch(b);

                    setKeyword(b.name);

                    setIsUserTyping(false);
                  }}
                  className="w-full flex items-center gap-3 px-4 py-3 text-left text-sm hover:bg-slate-50 transition-colors border-b border-slate-100 last:border-0"
                >
                  {getIcon(b.type)}

                  <div className="flex flex-col">
                    <span className="text-slate-700">{b.name}</span>

                    <span className="text-xs text-slate-400 capitalize">
                      {b.type}
                    </span>
                  </div>
                </button>
              </li>
            ))}
          </ul>
        )}
    </div>
  );
}
