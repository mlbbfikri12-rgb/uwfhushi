"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { addDays, differenceInCalendarDays, format } from "date-fns";
import { BedDouble, CalendarDays, MapPin, Search } from "lucide-react";
import { useRouter, useSearchParams } from "next/navigation";
import { useEffect, useRef, useState } from "react";
import { type DateRange, DayPicker } from "react-day-picker";
import { useForm } from "react-hook-form";
import { toast } from "sonner";
import { z } from "zod";
import { useBranchSearchQuery } from "@/features/tenant/hooks/useBranchSearchQuery";
import type { PublicBranch } from "@/types/branch";

const searchSchema = z.object({
  branchCode: z.string().min(2, "Pilih kota atau hotel"),
  dateRange: z.object({
    from: z.date({ required_error: "Check-in wajib diisi" }),
    to: z.date({ required_error: "Check-out wajib diisi" }),
  }),
  totalRooms: z.number().int().min(1).max(10),
});

type SearchValues = z.infer<typeof searchSchema>;

type Props = {
  branch?: string;
};

export function BranchSearchForm({ branch }: Props) {
  const router = useRouter();
  const searchParams = useSearchParams();

  const [keyword, setKeyword] = useState("");
  const [selectedBranch, setSelectedBranch] = useState<PublicBranch | null>(
    null,
  );
  const [dateRange, setDateRange] = useState<DateRange>({
    from: new Date(),
    to: addDays(new Date(), 1),
  });
  const [isCalendarOpen, setIsCalendarOpen] = useState(false);
  const calendarRef = useRef<HTMLDivElement>(null);

  const { data, isFetching } = useBranchSearchQuery(keyword);

  const { handleSubmit, setValue, watch } = useForm<SearchValues>({
    resolver: zodResolver(searchSchema),
    defaultValues: {
      totalRooms: 1,
      branchCode: "",
      dateRange: {
        from: new Date(),
        to: addDays(new Date(), 1),
      },
    },
  });

  const totalRooms = watch("totalRooms");

  useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (
        calendarRef.current &&
        !calendarRef.current.contains(e.target as Node)
      ) {
        setIsCalendarOpen(false);
      }
    };
    document.addEventListener("mousedown", handler);
    return () => document.removeEventListener("mousedown", handler);
  }, []);

  useEffect(() => {
    const checkIn =
      searchParams.get("checkIn") ?? searchParams.get("checkin_date");
    const checkOut = searchParams.get("checkOut");
    const rooms = searchParams.get("total_rooms") ?? searchParams.get("rooms");
    if (checkIn && checkOut) {
      const from = new Date(checkIn);
      const to = new Date(checkOut);
      if (!isNaN(from.getTime()) && !isNaN(to.getTime())) {
        setDateRange({ from, to });
        setValue("dateRange", { from, to });
      }
    }
    if (rooms) setValue("totalRooms", Number(rooms));
  }, [searchParams, setValue]);

  useEffect(() => {
    if (branch) {
      setValue("branchCode", branch);
      setKeyword(branch);
    }
  }, [branch, setValue]);

  useEffect(() => {
    if (!selectedBranch && data && data.length > 0) {
      const first = data[0];
      setSelectedBranch(first);
      setKeyword(first.name);
      setValue("branchCode", first.code, { shouldValidate: true });
    }
  }, [data, selectedBranch, setValue]);

  const nights =
    dateRange?.from && dateRange?.to
      ? differenceInCalendarDays(dateRange.to, dateRange.from)
      : 1;

  const checkInLabel = dateRange?.from
    ? format(dateRange.from, "EEE, dd MMM yyyy")
    : "—";
  const checkOutLabel = dateRange?.to
    ? format(dateRange.to, "EEE, dd MMM yyyy")
    : "—";

  const onSubmit = handleSubmit((values) => {
    if (!values.branchCode) {
      toast.error("Pilih kota atau hotel terlebih dahulu");
      return;
    }
    const b = values.branchCode.toUpperCase();
    const checkIn = format(values.dateRange.from, "yyyy-MM-dd");
    const checkOut = format(values.dateRange.to, "yyyy-MM-dd");
    router.push(
      `/hotel/${b}?checkIn=${checkIn}&checkOut=${checkOut}&total_rooms=${values.totalRooms}&duration=${nights}`,
    );
  });

  return (
    <form onSubmit={onSubmit} className="w-full">
      <div className="flex flex-col lg:flex-row rounded-2xl overflow-visible shadow-lg border border-slate-200 bg-white">
        {/* ── DESTINATION ── */}
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
                  setValue("branchCode", "");
                }}
                placeholder="City, hotel name..."
                className="w-full text-sm font-medium text-slate-800 placeholder:text-slate-400 bg-transparent outline-none"
              />
            </div>
          </div>

          {!isFetching &&
            data &&
            data.length > 0 &&
            !selectedBranch &&
            keyword.length > 1 && (
              <ul className="absolute left-0 top-full mt-2 z-50 w-72 bg-white border border-slate-200 rounded-xl shadow-2xl overflow-hidden">
                {data.map((b) => (
                  <li key={b.id}>
                    <button
                      type="button"
                      onClick={() => {
                        setSelectedBranch(b);
                        setKeyword(b.name);
                        setValue("branchCode", b.code, {
                          shouldValidate: true,
                        });
                      }}
                      className="w-full flex items-center gap-3 px-4 py-3 text-left text-sm hover:bg-slate-50 transition-colors border-b border-slate-100 last:border-0"
                    >
                      <MapPin size={14} className="text-[#c4a661] shrink-0" />
                      <span className="text-slate-500">{b.name}</span>
                    </button>
                  </li>
                ))}
              </ul>
            )}
        </div>

        {/* ── CHECK IN / CHECK OUT ── */}
        <div
          className="relative flex-[2.5] px-5 py-4 border-b lg:border-b-0 lg:border-r border-slate-200"
          ref={calendarRef}
        >
          <button
            type="button"
            onClick={() => setIsCalendarOpen((p) => !p)}
            className="w-full flex items-start gap-3 text-left"
          >
            <CalendarDays
              className="shrink-0 text-[#c4a661] mt-0.5"
              size={18}
            />
            <div>
              <p className="text-[10px] font-bold uppercase tracking-widest text-slate-400 mb-1.5">
                Check In – Check Out
              </p>
              <p className="text-sm font-medium text-slate-800">
                {checkInLabel}{" "}
                <span className="text-slate-400 font-normal">→</span>{" "}
                {checkOutLabel}
              </p>
              <p className="text-xs text-[#c4a661] font-semibold mt-0.5">
                {nights} {nights === 1 ? "Night" : "Nights"}
              </p>
            </div>
          </button>

          {isCalendarOpen && (
            <div className="absolute left-0 top-full mt-2 z-50 rounded-2xl border border-slate-200 bg-white p-4 shadow-2xl">
              <DayPicker
                mode="range"
                numberOfMonths={2}
                selected={dateRange}
                onSelect={(range) => {
                  if (range?.from) {
                    const newRange = {
                      from: range.from,
                      to: range.to ?? addDays(range.from, 1),
                    };
                    setDateRange(newRange);
                    setValue("dateRange", newRange, { shouldValidate: true });
                    if (range.from && range.to) setIsCalendarOpen(false);
                  }
                }}
                disabled={{ before: new Date() }}
                classNames={{
                  day: "h-9 w-9 rounded-full hover:bg-[#c4a661]/20 text-sm",
                  day_selected: "bg-[#c4a661] text-white",
                  day_range_start: "bg-[#c4a661] text-white rounded-l-full",
                  day_range_end: "bg-[#c4a661] text-white rounded-r-full",
                  day_range_middle: "bg-[#c4a661]/20",
                  nav_button: "hover:bg-slate-100 rounded-full p-1",
                  caption: "text-sm font-semibold text-slate-700 mb-2",
                }}
              />
            </div>
          )}
        </div>

        {/* ── ROOM ── */}
        <div className="flex-1 px-5 py-4 border-b lg:border-b-0 lg:border-r border-slate-200 flex items-start gap-3">
          <BedDouble className="shrink-0 text-[#c4a661] mt-0.5" size={18} />
          <div>
            <p className="text-[10px] font-bold uppercase tracking-widest text-slate-400 mb-1.5">
              Room
            </p>
            <div className="flex items-center gap-2.5 mt-1">
              <button
                type="button"
                onClick={() =>
                  setValue("totalRooms", Math.max(1, totalRooms - 1))
                }
                className="w-7 h-7 rounded-full border border-slate-300 flex items-center justify-center text-slate-600 hover:border-[#1a1f3c] hover:text-[#1a1f3c] transition-colors text-lg leading-none"
              >
                −
              </button>
              <span className="text-base font-bold text-slate-800 min-w-[20px] text-center">
                {totalRooms}
              </span>
              <button
                type="button"
                onClick={() =>
                  setValue("totalRooms", Math.min(10, totalRooms + 1))
                }
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

        {/* ── BUTTON ── */}
        <div className="px-4 py-4 flex items-center justify-center lg:justify-end shrink-0">
          <button
            type="submit"
            className="flex items-center gap-2 bg-[#1a1f3c] hover:bg-[#252c52] active:scale-95 text-white text-sm font-bold px-7 py-4 rounded-xl transition-all whitespace-nowrap w-full lg:w-auto justify-center"
          >
            <Search size={16} />
            Search Hotels
          </button>
        </div>
      </div>
    </form>
  );
}
