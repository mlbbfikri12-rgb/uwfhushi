import { Suspense } from "react";
import type { Metadata } from "next";
import HotelPageClient from "./hotel-page-client";

type PageProps = {
  params: { branch: string };
  searchParams: {
    checkIn?: string;
    checkOut?: string;
    total_rooms?: string;
  };
};

export const metadata: Metadata = {
  title: "Hotel Detail",
  description: "View hotel detail, facilities, and room rate plans.",
};

export default function Page({ params, searchParams }: PageProps) {
  const checkIn = searchParams.checkIn ?? "";
  const checkOut = searchParams.checkOut ?? "";
  const totalRooms = Number(searchParams.total_rooms ?? "1");

  return (
    <Suspense fallback={null}>
      <HotelPageClient
        slug={params.branch}
        checkIn={checkIn}
        checkOut={checkOut}
        totalRooms={Number.isFinite(totalRooms) && totalRooms > 0 ? totalRooms : 1}
      />
    </Suspense>
  );
}
