import { Suspense } from "react";
import BookingClient from "./ClientPage";

export default function Page() {
  return (
    <Suspense fallback={<div>Loading...</div>}>
      <BookingClient />
    </Suspense>
  );
}
