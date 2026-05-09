"use client";

import { addDays, differenceInCalendarDays, format } from "date-fns";
import { CalendarDays } from "lucide-react";
import dynamic from "next/dynamic";
import { useEffect, useRef, useState } from "react";

const DayPicker = dynamic(
  () => import("react-day-picker").then((mod) => mod.DayPicker),
  {
    ssr: false,
  },
);

type Props = {
  dateRange: {
    from: Date;
    to: Date;
  };

  setDateRange: React.Dispatch<
    React.SetStateAction<{
      from: Date;
      to: Date;
    }>
  >;
};

export function CalendarPicker({ dateRange, setDateRange }: Props) {
  const [isCalendarOpen, setIsCalendarOpen] = useState(false);

  const calendarRef = useRef<HTMLDivElement>(null);

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

  return (
    <div
      className="relative flex-[2.5] px-5 py-4 border-b lg:border-b-0 lg:border-r border-slate-200"
      ref={calendarRef}
    >
      <button
        type="button"
        onClick={() => setIsCalendarOpen((p) => !p)}
        className="w-full flex items-start gap-3 text-left"
      >
        <CalendarDays className="shrink-0 text-[#c4a661] mt-0.5" size={18} />

        <div>
          <p className="text-[10px] font-bold uppercase tracking-widest text-slate-400 mb-1.5">
            Check In – Check Out
          </p>

          <p className="text-sm font-medium text-slate-800">
            {checkInLabel} <span className="text-slate-400 font-normal">→</span>{" "}
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

                if (range.from && range.to) {
                  setIsCalendarOpen(false);
                }
              }
            }}
            disabled={{
              before: new Date(),
            }}
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
  );
}
