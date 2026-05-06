"use client";

import { useEffect, useMemo, useState } from "react";
import { useMutation, useQueries, useQuery } from "@tanstack/react-query";

import { differenceInCalendarDays } from "date-fns";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import Image from "next/image";

import { useBranchStore } from "@/store/useBranchStore";

import { clearBookingDraft, getBookingDraft } from "@/utils/BookingDraftUtils";

import { getRoomDetail } from "@/services/hotel.service";

import { checkoutFromOrder, createBooking } from "@/services/booking.service";

import { getCurrentOrder, addOrderItem } from "@/services/order.service";

import { getCurrentCustomer } from "@/services/auth.service";

import { BranchRouteSync } from "@/features/tenant/components/BranchRouteSync";
import { Navbar } from "@/components/layout/navbar";

type GuestFormState = {
  adultCount: number;
  childCount: number;
};

export default function BookingPage() {
  const router = useRouter();

  const branch = useBranchStore((s) => s.activeBranch);

  const [mounted, setMounted] = useState(false);

  useEffect(() => {
    setMounted(true);
  }, []);

  const [isSelfBooking, setIsSelfBooking] = useState(true);

  const [contact, setContact] = useState({
    email: "",
    firstName: "",
    lastName: "",
    phone: "",
  });

  const [guestDetail, setGuestDetail] = useState({
    email: "",
    firstName: "",
    lastName: "",
    phone: "",
  });

  const [guest, setGuest] = useState<GuestFormState>({
    adultCount: 2,
    childCount: 0,
  });

  // =========================
  // 🔐 AUTH
  // =========================

  const customerQuery = useQuery({
    queryKey: ["customer-me"],
    queryFn: getCurrentCustomer,
    retry: false,
  });

  const isAuthenticated =
    customerQuery.isSuccess && Boolean(customerQuery.data?.id);

  // =========================
  // 📦 ORDER LOGIN
  // =========================

  const orderQuery = useQuery({
    queryKey: ["order-current", branch],
    queryFn: getCurrentOrder,
    enabled: mounted && isAuthenticated && !!branch,
  });

  // =========================
  // 📦 LOCAL DRAFT
  // =========================

  const localDraft = useMemo(() => {
    if (!mounted) return null;

    const draft = getBookingDraft();

    if (!draft?.items?.length) {
      return null;
    }

    return draft;
  }, [mounted]);

  const draftItems = localDraft?.items ?? [];

  // =========================
  // 🏨 ROOM DETAIL QUERIES
  // =========================

  const roomDetailQueries = useQueries({
    queries: draftItems.map((item) => ({
      queryKey: ["room-detail", item.slug, item.roomTypeId],

      queryFn: () => getRoomDetail(item.slug, item.roomTypeId),

      enabled: !!item.slug && !!item.roomTypeId,

      staleTime: 1000 * 60 * 5,
    })),
  });

  // =========================
  // 🔥 ORDER ITEMS
  // =========================

  const orderItems = useMemo(() => {
    // ✅ LOGIN ORDER
    if (orderQuery.data?.items?.length) {
      return orderQuery.data.items.map((item: any) => ({
        roomTypeName: item.roomTypeName ?? "Room",

        ratePlanName: item.ratePlanName ?? "Rate Plan",

        checkIn: item.checkIn,

        checkOut: item.checkOut,

        totalRooms: item.totalRooms ?? 1,

        totalPrice: item.totalPrice ?? 0,

        roomId: item.roomId ?? item.roomTypeId ?? "",

        image: item.image ?? "",

        capacity: item.capacity ?? 2,

        bedType: item.bedType ?? "",
      }));
    }

    // ✅ GUEST FLOW
    return draftItems.map((item, index) => {
      const room = roomDetailQueries[index]?.data;

      return {
        roomTypeName: room?.name ?? item.roomTypeName ?? "Room",

        ratePlanName: item.ratePlanName ?? "Rate Plan",

        checkIn: item.checkIn,

        checkOut: item.checkOut,

        totalRooms: item.totalRooms ?? 1,

        totalPrice: (item.price ?? 0) * (item.totalRooms ?? 1),

        roomId: room?.id ?? item.roomId ?? "",

        image: room?.image ?? item.imageUrl ?? "",

        capacity: room?.capacity ?? 2,

        bedType: room?.bedType ?? "",
      };
    });
  }, [orderQuery.data, draftItems, roomDetailQueries]);

  // =========================
  // 💰 GRAND TOTAL
  // =========================

  const grandTotal = useMemo(() => {
    return orderItems.reduce((acc: number, item: any) => {
      return acc + (item.totalPrice ?? 0);
    }, 0);
  }, [orderItems]);

  // =========================
  // 📅 NIGHTS
  // =========================

  const nights = useMemo(() => {
    if (!orderItems.length) {
      return 0;
    }

    const first = orderItems[0];

    if (!first?.checkIn || !first?.checkOut) {
      return 0;
    }

    return Math.max(
      1,
      differenceInCalendarDays(
        new Date(first.checkOut),
        new Date(first.checkIn),
      ),
    );
  }, [orderItems]);

  // =========================
  // 🔄 SYNC DRAFT → DB
  // =========================

  const addMutation = useMutation({
    mutationFn: addOrderItem,

    onSuccess: async () => {
      await orderQuery.refetch();

      clearBookingDraft();
    },
  });

  useEffect(() => {
    if (!mounted) return;

    if (!isAuthenticated) return;

    if (!draftItems.length) return;

    if (orderQuery.data?.items?.length) {
      return;
    }

    if (addMutation.isPending) {
      return;
    }

    draftItems.forEach((item) => {
      addMutation.mutate({
        roomTypeId: item.roomTypeId,

        ratePlanId: item.ratePlanId,

        checkIn: item.checkIn,

        checkOut: item.checkOut,

        totalRooms: item.totalRooms,
      });
    });
  }, [mounted, isAuthenticated, draftItems, orderQuery.data]);

  // =========================
  // 🔥 AUTO COPY CONTACT
  // =========================

  useEffect(() => {
    if (!isSelfBooking) return;

    setGuestDetail({
      email: contact.email,

      firstName: contact.firstName,

      lastName: contact.lastName,

      phone: contact.phone,
    });
  }, [isSelfBooking, contact]);

  // =========================
  // 🚀 BOOKING
  // =========================

  const bookingMutation = useMutation({
    mutationFn: async () => {
      // ✅ LOGIN FLOW
      if (isAuthenticated) {
        return checkoutFromOrder({
          adultCount: guest.adultCount,

          childCount: guest.childCount,

          paymentMethod: "mock",
        });
      }

      // ✅ GUEST FLOW
      if (!orderItems.length) {
        throw new Error("Draft booking kosong");
      }

      const bookings = [];

      for (const item of orderItems) {
        if (!item.roomId) {
          throw new Error("Room belum siap");
        }

        const booking = await createBooking({
          roomId: item.roomId,

          checkIn: item.checkIn,

          checkOut: item.checkOut,

          adultCount: guest.adultCount,

          childCount: guest.childCount,

          customerName:
            `${guestDetail.firstName} ${guestDetail.lastName}`.trim(),

          customerEmail: guestDetail.email,

          customerPhone: guestDetail.phone,
        });

        bookings.push(booking);
      }

      return bookings;
    },

    onSuccess: (data: any) => {
      clearBookingDraft();

      const firstBooking = Array.isArray(data) ? data?.[0] : data;

      const bookingCode =
        firstBooking?.bookingCode || firstBooking?.bookings?.[0]?.bookingCode;

      toast.success("Booking berhasil");

      router.push(`/booking-success?code=${bookingCode}`);
    },

    onError: (err: any) => {
      toast.error(err?.message || "Gagal booking");
    },
  });

  // =========================
  // ⛔ EMPTY
  // =========================

  if (!mounted) {
    return null;
  }

  if (!orderItems.length) {
    return (
      <main className="p-10 text-center text-red-500">
        Tidak ada data booking
      </main>
    );
  }

  // =========================
  // 🎯 UI
  // =========================

  return (
    <main className="min-h-screen bg-[#f5f7fb] pt-20">
      {branch && <BranchRouteSync branch={branch} />}

      <Navbar />

      <div className="mx-auto max-w-7xl px-6 pb-10 grid lg:grid-cols-[1.6fr_1fr] gap-6">
        {/* LEFT */}
        <section className="space-y-6">
          {!isAuthenticated && (
            <div className="rounded-lg border border-blue-200 bg-blue-50 px-4 py-3 text-sm text-blue-700">
              Login atau Register untuk isi data lebih cepat
            </div>
          )}

          <div className="flex gap-4 text-sm">
            <span className="font-semibold text-[#1a1f3c]">1 Fill in Data</span>

            <span className="text-slate-400">2 Review</span>

            <span className="text-slate-400">3 Payment</span>
          </div>

          <div className="bg-white p-6 rounded-2xl shadow-sm space-y-6">
            {/* TOGGLE */}
            <div className="flex justify-between items-center border-b pb-4">
              <p className="text-sm font-medium">I am booking for myself</p>

              <input
                type="checkbox"
                checked={isSelfBooking}
                onChange={(e) => setIsSelfBooking(e.target.checked)}
              />
            </div>

            {/* CONTACT */}
            <div>
              <h3 className="font-semibold mb-2">Contact Detail</h3>

              <div className="space-y-3">
                <input
                  placeholder="Email Address"
                  className="w-full border rounded-lg px-3 py-2"
                  value={contact.email}
                  onChange={(e) =>
                    setContact((p) => ({
                      ...p,
                      email: e.target.value,
                    }))
                  }
                />

                <div className="grid grid-cols-2 gap-3">
                  <input
                    placeholder="First Name"
                    className="border rounded-lg px-3 py-2"
                    value={contact.firstName}
                    onChange={(e) =>
                      setContact((p) => ({
                        ...p,
                        firstName: e.target.value,
                      }))
                    }
                  />

                  <input
                    placeholder="Last Name"
                    className="border rounded-lg px-3 py-2"
                    value={contact.lastName}
                    onChange={(e) =>
                      setContact((p) => ({
                        ...p,
                        lastName: e.target.value,
                      }))
                    }
                  />
                </div>

                <input
                  placeholder="Phone Number"
                  className="w-full border rounded-lg px-3 py-2"
                  value={contact.phone}
                  onChange={(e) =>
                    setContact((p) => ({
                      ...p,
                      phone: e.target.value,
                    }))
                  }
                />
              </div>
            </div>

            {/* GUEST */}
            <div className={isSelfBooking ? "opacity-60" : ""}>
              <h3 className="font-semibold mb-2">Guest Detail</h3>

              <div className="space-y-3">
                <input
                  disabled={isSelfBooking}
                  placeholder="Email Address"
                  className="w-full border rounded-lg px-3 py-2 disabled:bg-slate-100"
                  value={guestDetail.email}
                  onChange={(e) =>
                    setGuestDetail((p) => ({
                      ...p,
                      email: e.target.value,
                    }))
                  }
                />

                <div className="grid grid-cols-2 gap-3">
                  <input
                    disabled={isSelfBooking}
                    placeholder="First Name"
                    className="border rounded-lg px-3 py-2 disabled:bg-slate-100"
                    value={guestDetail.firstName}
                    onChange={(e) =>
                      setGuestDetail((p) => ({
                        ...p,
                        firstName: e.target.value,
                      }))
                    }
                  />

                  <input
                    disabled={isSelfBooking}
                    placeholder="Last Name"
                    className="border rounded-lg px-3 py-2 disabled:bg-slate-100"
                    value={guestDetail.lastName}
                    onChange={(e) =>
                      setGuestDetail((p) => ({
                        ...p,
                        lastName: e.target.value,
                      }))
                    }
                  />
                </div>

                <input
                  disabled={isSelfBooking}
                  placeholder="Phone Number"
                  className="w-full border rounded-lg px-3 py-2 disabled:bg-slate-100"
                  value={guestDetail.phone}
                  onChange={(e) =>
                    setGuestDetail((p) => ({
                      ...p,
                      phone: e.target.value,
                    }))
                  }
                />
              </div>
            </div>

            {/* SUBMIT */}
            <button
              onClick={() => bookingMutation.mutate()}
              className="w-full bg-[#1a1f3c] text-white py-3 rounded-lg font-semibold"
            >
              Review Reservation
            </button>
          </div>
        </section>

        {/* RIGHT */}
        <aside className="bg-white rounded-2xl p-5 shadow-sm h-fit sticky top-24">
          <h3 className="font-semibold mb-4">Booking Details</h3>

          <div className="space-y-4">
            {orderItems.map((item, index) => (
              <div key={index} className="border-b pb-4">
                {/* IMAGE */}
                <div className="relative mb-3 h-40 w-full overflow-hidden rounded-xl">
                  <Image
                    src={
                      item.image ||
                      "https://images.unsplash.com/photo-1566665797739-1674de7a421a?w=1200&q=80"
                    }
                    alt={item.roomTypeName}
                    fill
                    priority={index === 0}
                    className="object-cover"
                    unoptimized
                  />
                </div>

                {/* INFO */}
                <p className="font-medium">{item.roomTypeName}</p>

                <p className="text-sm text-slate-500">{item.ratePlanName}</p>

                <div className="mt-3 text-sm space-y-1">
                  <p>Check In: {item.checkIn}</p>

                  <p>Check Out: {item.checkOut}</p>

                  <p>{nights} Night</p>

                  <p>{item.totalRooms} Room</p>

                  <p>{item.capacity} Guest</p>

                  <p>{item.bedType}</p>
                </div>

                <div className="mt-3 text-right font-semibold text-[#c4a661]">
                  Rp {(item.totalPrice ?? 0).toLocaleString("id-ID")}
                </div>
              </div>
            ))}
          </div>

          <div className="border-t mt-4 pt-4 flex justify-between">
            <span>Total</span>

            <span className="font-bold text-[#c4a661]">
              Rp {grandTotal.toLocaleString("id-ID")}
            </span>
          </div>
        </aside>
      </div>
    </main>
  );
}
