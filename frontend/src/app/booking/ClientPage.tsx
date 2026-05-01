"use client";

import { useMemo, useState } from "react";
import { useMutation, useQuery } from "@tanstack/react-query";
import { differenceInCalendarDays, format } from "date-fns";
import { useRouter, useSearchParams } from "next/navigation";
import { toast } from "sonner";
import { checkoutFromOrder } from "@/services/booking.service";
import { getCurrentOrder } from "@/services/order.service";
import { BranchRouteSync } from "@/features/tenant/components/BranchRouteSync";

type GuestFormState = {
  adultCount: number;
  childCount: number;
};

export default function BookingPage() {
  const router = useRouter();
  const params = useSearchParams();
  const branch = (params.get("branch") ?? "").toUpperCase();
  const [step, setStep] = useState(1);
  const [guest, setGuest] = useState<GuestFormState>({
    adultCount: 2,
    childCount: 0,
  });

  const orderQuery = useQuery({
    queryKey: ["order-current", branch],
    queryFn: getCurrentOrder,
  });

  const orderItem = orderQuery.data?.items[0];
  const nights = useMemo(() => {
    if (!orderItem) return 0;
    return Math.max(1, differenceInCalendarDays(new Date(orderItem.checkOut), new Date(orderItem.checkIn)));
  }, [orderItem]);

  const bookingMutation = useMutation({
    mutationFn: () =>
      checkoutFromOrder({
        adultCount: guest.adultCount,
        childCount: guest.childCount,
        paymentMethod: "mock",
      }),
    onSuccess: (data) => {
      toast.success(`Checkout berhasil: ${data.bookings.length} booking dibuat`);
      router.push(`/hotel/${branch}`);
    },
    onError: (error) => {
      toast.error(error instanceof Error ? error.message : "Gagal membuat booking.");
    },
  });

  if (orderQuery.isLoading) {
    return <main className="mx-auto max-w-6xl px-5 py-10 text-sm text-slate-500">Memuat order...</main>;
  }

  if (!orderItem) {
    return (
      <main className="mx-auto max-w-4xl px-5 py-10">
        <div className="rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-700">
          Tidak ada order aktif. Silakan pilih kamar terlebih dahulu.
        </div>
      </main>
    );
  }

  return (
    <main className="min-h-screen bg-slate-100 px-5 py-10">
      {branch && <BranchRouteSync branch={branch} />}
      <div className="mx-auto grid max-w-7xl gap-6 lg:grid-cols-[1.6fr_1fr]">
        <section className="space-y-6">
          <div className="flex items-center gap-4 text-sm text-slate-500">
            <span className={step >= 1 ? "font-semibold text-[#1a1f3c]" : ""}>1 Contact + Guest</span>
            <span className={step >= 2 ? "font-semibold text-[#1a1f3c]" : ""}>2 Review</span>
            <span className={step >= 3 ? "font-semibold text-[#1a1f3c]" : ""}>3 Payment</span>
          </div>

          {step === 1 && (
            <div className="space-y-4 rounded-2xl bg-white p-6 shadow-sm">
              <h2 className="text-lg font-semibold">Contact & Guest</h2>

              <div className="grid gap-3 md:grid-cols-2">
                <input
                  type="number"
                  min={1}
                  className="rounded-lg border px-3 py-2"
                  value={guest.adultCount}
                  onChange={(e) => setGuest((p) => ({ ...p, adultCount: Number(e.target.value) || 1 }))}
                />
                <input
                  type="number"
                  min={0}
                  className="rounded-lg border px-3 py-2"
                  value={guest.childCount}
                  onChange={(e) => setGuest((p) => ({ ...p, childCount: Number(e.target.value) || 0 }))}
                />
              </div>

              <button onClick={() => setStep(2)} className="rounded-lg bg-[#1a1f3c] px-5 py-2.5 text-sm font-semibold text-white">Lanjut Review</button>
            </div>
          )}

          {step === 2 && (
            <div className="space-y-4 rounded-2xl bg-white p-6 shadow-sm">
              <h2 className="text-lg font-semibold">Review</h2>
              <div className="rounded-lg border border-slate-200 p-4 text-sm">
                <p className="font-semibold text-slate-900">{orderItem.roomTypeName}</p>
                <p className="text-slate-600">{orderItem.ratePlanName}</p>
                <p className="mt-2 text-slate-600">Check-in: {orderItem.checkIn}</p>
                <p className="text-slate-600">Check-out: {orderItem.checkOut}</p>
                <p className="text-slate-600">Jumlah kamar: {orderItem.totalRooms}</p>
              </div>
              <div className="flex gap-2">
                <button onClick={() => setStep(1)} className="rounded-lg border px-5 py-2.5 text-sm">Kembali</button>
                <button onClick={() => setStep(3)} className="rounded-lg bg-[#1a1f3c] px-5 py-2.5 text-sm font-semibold text-white">Lanjut Payment</button>
              </div>
            </div>
          )}

          {step === 3 && (
            <div className="space-y-4 rounded-2xl bg-white p-6 shadow-sm">
              <h2 className="text-lg font-semibold">Payment (Mock)</h2>
              <p className="text-sm text-slate-600">Klik tombol di bawah untuk proses booking.</p>
              <div className="flex gap-2">
                <button onClick={() => setStep(2)} className="rounded-lg border px-5 py-2.5 text-sm">Kembali</button>
                <button
                  onClick={() => bookingMutation.mutate()}
                  disabled={bookingMutation.isPending}
                  className="rounded-lg bg-[#1a1f3c] px-5 py-2.5 text-sm font-semibold text-white disabled:opacity-60"
                >
                  {bookingMutation.isPending ? "Memproses..." : "Bayar & Booking"}
                </button>
              </div>
            </div>
          )}
        </section>

        <aside className="h-fit rounded-2xl bg-white p-5 shadow-sm lg:sticky lg:top-24">
          <h2 className="mb-4 text-lg font-semibold">Order Summary</h2>
          <p className="font-medium text-slate-900">{orderItem.roomTypeName}</p>
          <p className="text-sm text-slate-600">{orderItem.ratePlanName}</p>
          <div className="mt-4 space-y-2 text-sm">
            <div className="flex justify-between"><span>Check In</span><span>{format(new Date(orderItem.checkIn), "dd MMM yyyy")}</span></div>
            <div className="flex justify-between"><span>Check Out</span><span>{format(new Date(orderItem.checkOut), "dd MMM yyyy")}</span></div>
            <div className="flex justify-between"><span>Durasi</span><span>{nights} malam</span></div>
            <div className="flex justify-between"><span>Kamar</span><span>{orderItem.totalRooms}</span></div>
          </div>
          <div className="mt-5 border-t pt-4">
            <div className="flex justify-between text-sm"><span>Total</span><span className="font-semibold">Rp {orderItem.totalPrice.toLocaleString("id-ID")}</span></div>
          </div>
        </aside>
      </div>
    </main>
  );
}
