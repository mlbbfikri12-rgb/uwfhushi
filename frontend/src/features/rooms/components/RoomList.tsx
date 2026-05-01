"use client";

import { useMemo, useState } from "react";
import { useRoomsQuery } from "@/features/rooms/hooks/useRoomsQuery";
import { useBranchStore } from "@/store/useBranchStore";
import { enrichRoom } from "@/utils/enrichRooom";
import { RoomCard } from "./ui/RoomCard";
import { Spinner } from "./ui/Spinner";
import { SlidersHorizontal } from "lucide-react";

type Props = {
  branch: string;
  search: {
    checkIn: string;
    checkOut: string;
    adultCount: number;
    childCount: number;
  };
  nights?: number;
};

const BRANDS = [
  "MyLynn Premier",
  "MyLynn Express",
  "MyLynn Boutique",
  "MyLynn Suites",
];
const STAR_RATINGS = [5, 4, 3, 2];

export function RoomList({ branch, search, nights = 1 }: Props) {
  const activeBranch = useBranchStore((state) => state.activeBranch);
  const isReady = activeBranch === branch.toUpperCase();

  const [priceRange, setPriceRange] = useState(2000000);
  const [selectedBrands, setSelectedBrands] = useState<string[]>([]);
  const [selectedStars, setSelectedStars] = useState<number[]>([]);
  const [showFilter, setShowFilter] = useState(false);

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

  const enriched = useMemo(() => {
    if (!data) return [];
    return data
      .map(enrichRoom)
      .filter((room) => room.roomType.basePrice <= priceRange);
  }, [data, priceRange]);

  const toggleBrand = (b: string) =>
    setSelectedBrands((prev) =>
      prev.includes(b) ? prev.filter((x) => x !== b) : [...prev, b],
    );

  const toggleStar = (s: number) =>
    setSelectedStars((prev) =>
      prev.includes(s) ? prev.filter((x) => x !== s) : [...prev, s],
    );

  if (!isReady || isLoading) return <Spinner />;

  if (isError) {
    return (
      <div className="rounded-xl border border-red-200 bg-red-50 p-5 text-sm text-red-700">
        {(error as Error)?.message ?? "Gagal mengambil data kamar"}
      </div>
    );
  }

  if (!data?.length) {
    return (
      <div className="rounded-xl border border-slate-200 bg-white p-8 text-center">
        <p className="text-slate-500 text-sm">
          Tidak ada kamar tersedia untuk tanggal yang dipilih.
        </p>
      </div>
    );
  }

  return (
    <div className="flex gap-8">
      {/* ── FILTER SIDEBAR ── */}
      <aside className="hidden lg:block w-64 shrink-0 space-y-6">
        {/* Price range */}
        <div className="bg-white rounded-xl border border-slate-200 p-5">
          <h3 className="text-sm font-bold text-slate-800 mb-4">Price Range</h3>
          <input
            type="range"
            min={100000}
            max={5000000}
            step={50000}
            value={priceRange}
            onChange={(e) => setPriceRange(Number(e.target.value))}
            className="w-full accent-[#1a1f3c]"
          />
          <div className="flex justify-between mt-2 text-xs text-slate-500">
            <span>Rp 100.000</span>
            <span className="font-semibold text-[#1a1f3c]">
              Rp {priceRange.toLocaleString("id-ID")}
            </span>
          </div>
        </div>

        {/* Brand */}
        <div className="bg-white rounded-xl border border-slate-200 p-5">
          <h3 className="text-sm font-bold text-slate-800 mb-4">Brand</h3>
          <div className="space-y-2.5">
            {BRANDS.map((brand) => (
              <label
                key={brand}
                className="flex items-center gap-2.5 cursor-pointer"
              >
                <input
                  type="checkbox"
                  checked={selectedBrands.includes(brand)}
                  onChange={() => toggleBrand(brand)}
                  className="w-4 h-4 accent-[#1a1f3c] rounded"
                />
                <span className="text-sm text-slate-600">{brand}</span>
              </label>
            ))}
          </div>
        </div>

        {/* Star rating */}
        <div className="bg-white rounded-xl border border-slate-200 p-5">
          <h3 className="text-sm font-bold text-slate-800 mb-4">
            Hotel Star Rating
          </h3>
          <div className="space-y-2.5">
            {STAR_RATINGS.map((star) => (
              <label
                key={star}
                className="flex items-center gap-2.5 cursor-pointer"
              >
                <input
                  type="checkbox"
                  checked={selectedStars.includes(star)}
                  onChange={() => toggleStar(star)}
                  className="w-4 h-4 accent-[#1a1f3c] rounded"
                />
                <span className="text-sm text-slate-600 flex items-center gap-1">
                  {"★".repeat(star)}
                  <span className="text-slate-400 text-xs ml-1">
                    ({star} stars)
                  </span>
                </span>
              </label>
            ))}
          </div>
        </div>
      </aside>

      {/* ── ROOM RESULTS ── */}
      <div className="flex-1 min-w-0">
        {/* Header hasil */}
        <div className="flex items-center justify-between mb-5">
          <p className="text-sm text-slate-500">
            Menampilkan{" "}
            <span className="font-semibold text-slate-800">
              {enriched.length}
            </span>{" "}
            tipe kamar tersedia
          </p>
          {/* Filter toggle mobile */}
          <button
            onClick={() => setShowFilter(!showFilter)}
            className="lg:hidden flex items-center gap-2 text-sm border border-slate-200 px-3 py-2 rounded-lg hover:bg-slate-50"
          >
            <SlidersHorizontal size={14} />
            Filter
          </button>
        </div>

        {enriched.length === 0 ? (
          <div className="rounded-xl border border-slate-200 bg-white p-8 text-center">
            <p className="text-slate-500 text-sm">
              Tidak ada kamar sesuai filter yang dipilih.
            </p>
            <button
              onClick={() => {
                setPriceRange(2000000);
                setSelectedBrands([]);
                setSelectedStars([]);
              }}
              className="mt-3 text-sm text-[#1a1f3c] underline"
            >
              Reset filter
            </button>
          </div>
        ) : (
          <div className="space-y-5">
            {enriched.map((room) => (
              <RoomCard
                key={room.id}
                room={room}
                branch={branch}
                search={search}
                nights={nights}
              />
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
