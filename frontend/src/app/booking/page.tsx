import { differenceInCalendarDays, format } from "date-fns";
import { BookingForm } from "@/features/booking/components/BookingForm";

type Props = {
  searchParams: {
    roomId?: string;
    branch?: string;
    checkIn?: string;
    checkOut?: string;
    adult?: string;
    child?: string;
  };
};

export default function BookingPage({ searchParams }: Props) {
  const { roomId, checkIn, checkOut, adult, child, branch } = searchParams;

  if (!roomId || !checkIn || !checkOut) {
    return (
      <div className="max-w-4xl mx-auto p-5">
        <div className="rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-700">
          Data booking tidak lengkap.
        </div>
      </div>
    );
  }

  const checkInDate = new Date(checkIn);
  const checkOutDate = new Date(checkOut);

  const nights = differenceInCalendarDays(checkOutDate, checkInDate);

  return (
    <main className="min-h-screen bg-slate-100 py-10 px-5">
      <div className="max-w-7xl mx-auto grid lg:grid-cols-[1.6fr_1fr] gap-6">
        {/* LEFT FORM */}
        <div className="space-y-6">
          {/* STEP */}
          <div className="flex items-center gap-4 text-sm text-slate-500">
            <span className="font-semibold text-[#1a1f3c]">1 Fill Data</span>
            <span>2 Review</span>
            <span>3 Payment</span>
          </div>

          <BookingForm
            roomId={roomId}
            checkIn={checkIn}
            checkOut={checkOut}
            adult={adult}
            child={child}
          />
        </div>

        {/* RIGHT SUMMARY */}
        <div className="rounded-2xl bg-white p-5 shadow-sm h-fit sticky top-24">
          <h2 className="text-lg font-semibold mb-4">Booking Details</h2>

          {/* ROOM */}
          <div className="flex gap-3 items-center">
            <div className="w-16 h-16 bg-slate-200 rounded-lg" />
            <div>
              <p className="font-medium">Room #{roomId}</p>
              <p className="text-xs text-slate-500">{branch}</p>
            </div>
          </div>

          {/* DATE */}
          <div className="mt-5 text-sm space-y-2">
            <div className="flex justify-between">
              <span>Check In</span>
              <span>{format(checkInDate, "dd MMM yyyy")}</span>
            </div>

            <div className="flex justify-between">
              <span>Duration</span>
              <span>{nights} malam</span>
            </div>
          </div>

          {/* PRICE */}
          <div className="mt-6 border-t pt-4">
            <p className="text-sm text-slate-500 mb-2">Price Detail</p>

            <div className="flex justify-between text-sm">
              <span>Room</span>
              <span>Rp 500.000</span>
            </div>

            <div className="flex justify-between font-semibold mt-3 text-lg">
              <span>Total</span>
              <span className="text-[#c4a661]">
                Rp {(500000 * nights).toLocaleString("id-ID")}
              </span>
            </div>
          </div>
        </div>
      </div>
    </main>
  );
}
