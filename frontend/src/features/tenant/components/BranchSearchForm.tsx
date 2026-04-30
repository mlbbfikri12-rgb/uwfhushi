"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { differenceInCalendarDays, format } from "date-fns";
import { CalendarDays, MapPin, Search } from "lucide-react";
import { useRouter, useSearchParams } from "next/navigation";
import { useEffect, useState } from "react";
import { type DateRange, DayPicker } from "react-day-picker";
import { useForm } from "react-hook-form";
import { toast } from "sonner";
import { z } from "zod";
import { useBranchSearchQuery } from "@/features/tenant/hooks/useBranchSearchQuery";
import type { PublicBranch } from "@/types/branch";

const homeSearchSchema = z
  .object({
    branchCode: z.string().min(2, "Pilih kota atau hotel"),
    dateRange: z.object({
      from: z.date({ required_error: "Check-in wajib diisi" }),
      to: z.date({ required_error: "Check-out wajib diisi" }),
    }),
    adultCount: z.number().int().min(1).max(8),
    childCount: z.number().int().min(0).max(8),
  })
  .refine((value) => value.dateRange.to > value.dateRange.from, {
    path: ["dateRange"],
    message: "Check-out harus setelah check-in",
  });

type HomeSearchValues = z.infer<typeof homeSearchSchema>;

type Props = {
  branch?: string; // 🔥 dari server
};

export function BranchSearchForm({ branch }: Props) {
  const router = useRouter();
  const searchParams = useSearchParams();

  const [keyword, setKeyword] = useState("");
  const [selectedBranch, setSelectedBranch] = useState<PublicBranch | null>(
    null,
  );
  const [selectedDateRange, setSelectedDateRange] = useState<
    DateRange | undefined
  >();
  const [isCalendarOpen, setIsCalendarOpen] = useState(false);

  const { data, isFetching } = useBranchSearchQuery(keyword);

  const {
    handleSubmit,
    setValue,
    watch,
    register,
    formState: { errors },
  } = useForm<HomeSearchValues>({
    resolver: zodResolver(homeSearchSchema),
    defaultValues: {
      adultCount: 2,
      childCount: 0,
      branchCode: "",
    },
  });

  const dateRange = watch("dateRange");
  const activeRange = selectedDateRange ?? dateRange;

  const nights =
    activeRange?.from && activeRange?.to
      ? differenceInCalendarDays(activeRange.to, activeRange.from)
      : null;

  const dateLabel =
    activeRange?.from && activeRange?.to
      ? `${format(activeRange.from, "dd MMM yyyy")} - ${format(
          activeRange.to,
          "dd MMM yyyy",
        )}`
      : "Pilih check-in dan check-out";

  const isValid = watch("branchCode") && activeRange?.from && activeRange?.to;

  // 🔥 =========================
  // 🔥 SYNC URL → FORM
  // 🔥 =========================
  useEffect(() => {
    const checkIn = searchParams.get("checkIn");
    const checkOut = searchParams.get("checkOut");
    const adult = searchParams.get("adult");
    const child = searchParams.get("child");

    // DATE
    if (checkIn && checkOut) {
      const from = new Date(checkIn);
      const to = new Date(checkOut);

      if (!isNaN(from.getTime()) && !isNaN(to.getTime())) {
        setValue("dateRange", { from, to });
        setSelectedDateRange({ from, to });
      }
    }

    // GUEST
    if (adult) setValue("adultCount", Number(adult));
    if (child) setValue("childCount", Number(child));
  }, [searchParams, setValue]);

  // 🔥 =========================
  // 🔥 SYNC BRANCH (ROUTE PARAM)
  // 🔥 =========================
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
      setKeyword(`${first.code} - ${first.name}`);

      setValue("branchCode", first.code, {
        shouldValidate: true,
      });
    }
  }, [data, selectedBranch, setValue]);

  // 🔥 =========================
  // 🔥 SUBMIT
  // 🔥 =========================
  const onSubmit = handleSubmit((values) => {
    if (!values.branchCode) {
      toast.error("Pilih branch hotel terlebih dahulu");
      return;
    }

    if (!values.dateRange?.from || !values.dateRange?.to) {
      toast.error("Pilih tanggal menginap terlebih dahulu");
      return;
    }

    const branch = values.branchCode.toUpperCase();
    const checkIn = format(values.dateRange.from, "yyyy-MM-dd");
    const checkOut = format(values.dateRange.to, "yyyy-MM-dd");

    router.push(
      `/hotel/${branch}?checkIn=${checkIn}&checkOut=${checkOut}&adult=${values.adultCount}&child=${values.childCount}`,
    );
  });

  return (
    <form
      onSubmit={onSubmit}
      className="mx-auto w-full max-w-7xl rounded-2xl bg-white p-3"
    >
      <div className="grid gap-3 lg:grid-cols-[2.2fr_2.2fr_2fr_auto] lg:items-end">
        {/* ================= DESTINATION ================= */}
        <div className="min-w-0">
          <label className="mb-1 block text-xs font-semibold uppercase text-slate-600">
            Kota / Hotel
          </label>

          {errors.branchCode && (
            <p className="text-xs text-red-600">{errors.branchCode.message}</p>
          )}

          <div className="relative">
            <MapPin className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />

            <input
              value={keyword}
              onChange={(event) => {
                setKeyword(event.target.value);
                setSelectedBranch(null);
                setValue("branchCode", "");
              }}
              placeholder="Cari kota atau nama hotel"
              className="h-11 w-full rounded-md bg-white px-4 pl-10 text-sm shadow-sm ring-1 ring-slate-200"
            />

            {!isFetching && data && data.length > 0 && !selectedBranch && (
              <ul className="absolute left-0 top-full z-50 mt-2 max-h-48 w-full overflow-auto rounded-xl border bg-white shadow-xl">
                {data.map((branch) => (
                  <li key={branch.id}>
                    <button
                      type="button"
                      onClick={() => {
                        setSelectedBranch(branch);
                        setKeyword(`${branch.code} - ${branch.name}`);
                        setValue("branchCode", branch.code, {
                          shouldValidate: true,
                        });
                      }}
                      className="w-full px-3 py-2 text-left text-sm hover:bg-slate-100"
                    >
                      <span className="font-semibold">{branch.code}</span> -{" "}
                      {branch.name}
                    </button>
                  </li>
                ))}
              </ul>
            )}
          </div>
        </div>

        {/* ================= DATE ================= */}
        <div className="min-w-0">
          <div className="mb-1 flex justify-between">
            <label className="text-xs font-semibold uppercase text-slate-600">
              Tanggal Menginap
            </label>
            <span className="text-xs text-amber-700">
              {nights ? `${nights} malam` : ""}
            </span>
          </div>

          <div className="relative">
            <button
              type="button"
              onClick={() => setIsCalendarOpen((prev) => !prev)}
              className="h-11 w-full rounded-md bg-white px-4 pl-10 text-left text-sm shadow-sm ring-1 ring-slate-200"
            >
              {dateLabel}
            </button>

            <CalendarDays className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />

            {isCalendarOpen && (
              <div className="absolute z-50 mt-2 rounded-xl border bg-white p-3 shadow-2xl">
                <DayPicker
                  mode="range"
                  numberOfMonths={2}
                  selected={activeRange}
                  onSelect={(range) => {
                    setSelectedDateRange(range);

                    if (range?.from && range?.to) {
                      setSelectedDateRange(range);

                      setValue(
                        "dateRange",
                        {
                          from: range.from,
                          to: range.to,
                        },
                        { shouldValidate: true },
                      );

                      setIsCalendarOpen(false);
                    }
                  }}
                  disabled={{ before: new Date() }}
                  classNames={{
                    day: "h-9 w-9 rounded-full hover:bg-[#c4a661]/20",
                    day_selected: "bg-[#c4a661] text-white",
                    day_range_start: "bg-[#c4a661] text-white rounded-l-full",
                    day_range_end: "bg-[#c4a661] text-white rounded-r-full",
                    day_range_middle: "bg-[#c4a661]/30",
                  }}
                />
              </div>
            )}
          </div>
        </div>

        {/* ================= GUEST ================= */}
        <div>
          <label className="mb-1 block text-xs font-semibold uppercase text-slate-600">
            Tamu
          </label>

          <div className="grid grid-cols-2 gap-2">
            <input
              type="number"
              min={1}
              {...register("adultCount", { valueAsNumber: true })}
              className="h-11 rounded-md bg-white px-3 text-sm shadow-sm ring-1 ring-slate-200"
              placeholder="Adult"
            />

            <input
              type="number"
              min={0}
              {...register("childCount", { valueAsNumber: true })}
              className="h-11 rounded-md bg-white px-3 text-sm shadow-sm ring-1 ring-slate-200"
              placeholder="Child"
            />
          </div>
        </div>

        {/* ================= BUTTON ================= */}
        <div className="flex lg:justify-end">
          <button
            type="submit"
            disabled={!isValid}
            className={`h-11 w-full rounded-md px-8 text-sm font-semibold lg:w-auto
              ${
                isValid
                  ? "bg-slate-900 text-white"
                  : "bg-slate-300 text-slate-500 cursor-not-allowed"
              }
            `}
          >
            <span className="inline-flex items-center gap-2">
              <Search className="h-4 w-4" />
              Cari Kamar
            </span>
          </button>
        </div>
      </div>
    </form>
  );
}
