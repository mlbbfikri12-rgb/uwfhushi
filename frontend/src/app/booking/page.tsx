"use client";

import dynamic from "next/dynamic";

const BookingClient = dynamic(() => import("./ClientPage"), {
  ssr: false,
});

export default function Page() {
  return <BookingClient />;
}
