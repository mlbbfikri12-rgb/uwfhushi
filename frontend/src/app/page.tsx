import { Suspense } from "react";
import type { Metadata } from "next";
import Homepage from "./Homepage";

export const metadata: Metadata = {
  title: "Hotel Booking Platform",
  description: "Book hotels across Indonesia with best price and experience",
};

export default function Page() {
  return (
    <Suspense fallback={<div>Loading...</div>}>
      <Homepage />
    </Suspense>
  );
}
