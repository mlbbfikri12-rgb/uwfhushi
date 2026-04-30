import Image from "next/image";
import { differenceInCalendarDays } from "date-fns";

import { BranchRouteSync } from "@/features/tenant/components/BranchRouteSync";
import { BranchSearchForm } from "@/features/tenant/components/BranchSearchForm";
import { RoomList } from "@/features/rooms/components/RoomList";
import { Navbar } from "@/components/layout/navbar";

type Props = {
  params: {
    branch: string;
  };
  searchParams: {
    checkIn?: string;
    checkOut?: string;
    adult?: string;
    child?: string;
  };
};

export default function BranchHotelPage({ params, searchParams }: Props) {
  const branch = params.branch.toUpperCase();

  const checkIn = searchParams.checkIn;
  const checkOut = searchParams.checkOut;
  const adultCount = Number(searchParams.adult ?? "2");
  const childCount = Number(searchParams.child ?? "0");

  const hasRequiredSearch = Boolean(checkIn && checkOut);

  let isValidDateRange = false;

  if (checkIn && checkOut) {
    const checkInDate = new Date(checkIn);
    const checkOutDate = new Date(checkOut);

    isValidDateRange =
      !isNaN(checkInDate.getTime()) &&
      !isNaN(checkOutDate.getTime()) &&
      differenceInCalendarDays(checkOutDate, checkInDate) > 0;
  }

  return (
    <main className="min-h-screen bg-slate-50">
      <BranchRouteSync branch={branch} />

      {/* ✅ NAVBAR */}
      <Navbar />

      {/* HERO */}
      <section className="relative h-[320px] w-full">
        <Image
          src="https://images.unsplash.com/photo-1566073771259-6a8506099945"
          alt="Hotel"
          fill
          className="object-cover"
        />

        {/* overlay */}
        <div className="absolute inset-0 bg-[#1a1f3c]/80" />

        {/* content */}
        <div className="absolute inset-0 flex flex-col items-center justify-center text-white pt-16">
          {/* pt-16 biar gak ketabrak navbar */}
          <h1 className="text-3xl font-bold">Hotel {branch}</h1>
        </div>
      </section>

      {/* SEARCH */}
      <div className="-mt-12 relative z-30">
        <div className="max-w-5xl mx-auto px-5">
          <div className="bg-white rounded-2xl shadow-2xl p-5">
            <BranchSearchForm branch={branch} />
          </div>
        </div>
      </div>

      {/* CONTENT */}
      <div className="max-w-7xl mx-auto px-5 mt-16 pb-16">
        {!hasRequiredSearch ? (
          <div className="rounded-lg border border-amber-200 bg-amber-50 p-4 text-sm text-amber-900">
            Isi tanggal terlebih dahulu
          </div>
        ) : !isValidDateRange ? (
          <div className="rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-800">
            Rentang tanggal tidak valid
          </div>
        ) : (
          <RoomList
            branch={branch}
            search={{
              checkIn: checkIn!,
              checkOut: checkOut!,
              adultCount: Number.isNaN(adultCount) ? 1 : adultCount,
              childCount: Number.isNaN(childCount) ? 0 : childCount,
            }}
          />
        )}
      </div>
    </main>
  );
}
