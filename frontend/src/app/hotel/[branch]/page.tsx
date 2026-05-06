import { redirect } from "next/navigation";
import type { Metadata } from "next";
import HotelPageClient from "./hotel-page-client";
import { getHotel } from "@/services/hotel.service";

type PageProps = {
  params: { branch: string };
  searchParams: {
    checkIn?: string;
    checkOut?: string;
    total_rooms?: string;
  };
};

function getDefaultDates() {
  const today = new Date();
  const tomorrow = new Date();
  tomorrow.setDate(today.getDate() + 1);

  const format = (date: Date) => date.toISOString().split("T")[0];

  return {
    checkIn: format(today),
    checkOut: format(tomorrow),
  };
}

export const metadata: Metadata = {
  title: "Hotel Detail",
};

export default async function Page({ params, searchParams }: PageProps) {
  const defaults = getDefaultDates();

  const checkIn = searchParams.checkIn ?? defaults.checkIn;
  const checkOut = searchParams.checkOut ?? defaults.checkOut;
  const totalRooms = Number(searchParams.total_rooms ?? "1");

  if (!searchParams.checkIn || !searchParams.checkOut) {
    redirect(
      `/hotel/${params.branch}?checkIn=${checkIn}&checkOut=${checkOut}&total_rooms=${totalRooms}`,
    );
  }

  const hotel = await getHotel(params.branch);

  return (
    <HotelPageClient
      slug={params.branch}
      checkIn={checkIn}
      checkOut={checkOut}
      totalRooms={totalRooms}
      hotel={hotel}
    />
  );
}
