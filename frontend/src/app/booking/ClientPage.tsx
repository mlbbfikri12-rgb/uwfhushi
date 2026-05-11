"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import { useMutation, useQueries, useQuery } from "@tanstack/react-query";

import { differenceInCalendarDays } from "date-fns";

import { toast } from "sonner";

import Image from "next/image";

import { useBranchStore } from "@/store/useBranchStore";

import { getRoomDetail } from "@/services/hotel.service";

import { checkoutFromOrder, guestCheckout } from "@/services/booking.service";

import { getCurrentOrder, addOrderItem } from "@/services/order.service";

import { getCurrentCustomer } from "@/services/auth.service";

import { BranchRouteSync } from "@/features/tenant/components/BranchRouteSync";

import { Navbar } from "@/components/layout/navbar";

import { CalendarX, ArrowRight } from "lucide-react";
import Link from "next/link";

import type {
  CheckoutOrderResponse,
  GuestCheckoutResponse,
} from "@/types/booking";
import type { OrderCurrent } from "@/types/order";
import {
  buildGuestCheckoutPayload,
  clearDraft,
  getDraft,
} from "@/utils/BookingDraftUtils";
import { BookingDraftItem } from "@/types/BookingDraft";
import {
  Calendar,
  Users,
  BedDouble,
  DoorOpen,
  Moon,
  Coffee,
  ShieldCheck,
} from "lucide-react";
import { getImageUrl } from "@/utils/ImageCombineUrl";
import { queryKeys } from "@/lib/query-keys";
import { appLogger } from "@/lib/logger";
import { formatBookingDate } from "@/utils/FormatDate";
import { splitName } from "@/utils/SplitName";
type GuestFormState = {
  adultCount: number;
  childCount: number;
};

type DraftItemWithContext = BookingDraftItem & {
  slug: string;
  checkIn: string;
  checkOut: string;
};

type UIOrderItem = {
  roomTypeId: string;

  ratePlanId: string;

  roomTypeName: string;

  ratePlanName: string;

  checkIn: string;

  checkOut: string;

  totalRooms: number;

  totalPrice: number;

  image: string;

  capacity: number;
  MaxAdults?: number;
  MaxChildren?: number;
  isBreakFast: boolean;
  isRefundable: boolean;

  bedType: string;
};

export default function BookingPage() {
  const branch = useBranchStore((s) => s.activeBranch);
  const hasSyncedDraftRef = useRef(false);

  const [mounted, setMounted] = useState(false);
  const [step, setStep] = useState(1);
  const [bookingCode, setBookingCode] = useState<string | null>(null);
  const [specialRequest, setSpecialRequest] = useState("");
  const [tripType, setTripType] = useState<"" | "business" | "leisure">("");

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

  const [guest] = useState<GuestFormState>({
    adultCount: 2,
    childCount: 0,
  });

  // =========================
  // 🔐 AUTH
  // =========================

  const customerQuery = useQuery({
    queryKey: queryKeys.customer.me,
    queryFn: getCurrentCustomer,
    retry: false,
  });

  const isAuthenticated =
    customerQuery.isSuccess && Boolean(customerQuery.data?.id);

  useEffect(() => {
    if (!customerQuery.data) return;

    const customer = customerQuery.data;

    const { firstName, lastName } = splitName(customer.name);

    setContact({
      email: customer.email ?? "",
      firstName: firstName ?? "",
      lastName: lastName ?? "",
      phone: customer.phone ?? "",
    });
  }, [customerQuery.data]);

  // =========================
  // 📦 ORDER LOGIN
  // =========================

  const orderQuery = useQuery({
    queryKey: queryKeys.order.current(branch),
    queryFn: getCurrentOrder,
    enabled: mounted && isAuthenticated && !!branch,
  });

  // =========================
  // 📦 LOCAL DRAFT
  // =========================

  // =========================
  // 📦 LOCAL DRAFT
  // =========================

  const localDraft = useMemo(() => {
    if (!mounted) return null;

    const draft = getDraft();

    if (!draft?.items?.length) return null;

    return draft;
  }, [mounted]);

  // 🔥 inject root context (PENTING)
  const draftItems = useMemo<DraftItemWithContext[]>(() => {
    if (!localDraft) return [];

    return localDraft.items.map((item) => ({
      ...item,
      slug: localDraft.slug,
      checkIn: localDraft.checkIn,
      checkOut: localDraft.checkOut,
    }));
  }, [localDraft]);

  // =========================
  // 🏨 ROOM DETAIL QUERIES
  // =========================

  const roomDetailQueries = useQueries({
    queries: draftItems.map((item) => ({
      queryKey: queryKeys.hotel.roomDetail(item.slug, item.roomTypeId),
      queryFn: () => getRoomDetail(item.slug, item.roomTypeId),
      enabled: !!item.slug && !!item.roomTypeId,
      staleTime: 1000 * 60 * 5,
    })),
  });

  // =========================
  // 🔥 ORDER ITEMS (FIX TOTALROOMS)
  // =========================

  const orderItems = useMemo<UIOrderItem[]>(() => {
    // LOGIN FLOW
    if (orderQuery.data?.items?.length) {
      return orderQuery.data.items.map(
        (item: OrderCurrent["items"][number]): UIOrderItem => ({
          roomTypeId: item.roomTypeId ?? "",
          ratePlanId: item.ratePlanId ?? "",
          roomTypeName: item.roomTypeName ?? "Room",
          ratePlanName: item.ratePlanName ?? "Rate Plan",
          checkIn: item.checkIn,
          checkOut: item.checkOut,
          totalRooms: item.totalRooms ?? 1,
          totalPrice: item.totalPrice ?? 0,
          image: item.image ?? "",
          capacity: item.capacity ?? 2,
          MaxAdults: item.MaxAdults,
          isBreakFast: item.isBreakFast,
          isRefundable: item.isRefundable,
          MaxChildren: item.MaxChildren,
          bedType: item.bedType ?? "",
        }),
      );
    }

    // GUEST FLOW (🔥 FIX DI SINI)
    return draftItems.map((item, index): UIOrderItem => {
      const room = roomDetailQueries[index]?.data;

      const selectedRatePlan = room?.ratePlans?.find(
        (rp: { id: string }) => rp.id === item.ratePlanId,
      );

      return {
        roomTypeId: item.roomTypeId,
        ratePlanId: item.ratePlanId,

        roomTypeName: room?.name ?? item.roomTypeName ?? "Room",
        ratePlanName: item.ratePlanName ?? "Rate Plan",

        checkIn: item.checkIn,
        checkOut: item.checkOut,
        MaxAdults: item.MaxAdults,
        MaxChildren: item.MaxChildren,
        isBreakFast: selectedRatePlan?.isBreakFast ?? false,
        isRefundable: selectedRatePlan?.isRefundable ?? false,

        // 🔥 INI YANG NYAMBUNG KE HOTEL DETAIL
        totalRooms: item.qty,
        totalPrice: (item.price ?? 0) * item.qty,

        image: room?.image ?? item.imageUrl ?? "",
        capacity: room?.capacity ?? 2,
        bedType: room?.bedType ?? "Standard Bed",
      };
    });
  }, [orderQuery.data, draftItems, roomDetailQueries]);

  // =========================
  // 💰 GRAND TOTAL
  // =========================

  const grandTotal = useMemo(() => {
    return orderItems.reduce((acc, item) => acc + (item.totalPrice ?? 0), 0);
  }, [orderItems]);

  // =========================
  // 📅 NIGHTS
  // =========================

  const nights = useMemo(() => {
    if (!orderItems.length) return 0;

    const first = orderItems[0];

    if (!first?.checkIn || !first?.checkOut) return 0;

    return Math.max(
      1,
      differenceInCalendarDays(
        new Date(first.checkOut),
        new Date(first.checkIn),
      ),
    );
  }, [orderItems]);

  // =========================
  // 🔄 SYNC DRAFT → DB (FIX DOUBLE INSERT)
  // =========================

  const addMutation = useMutation({
    mutationFn: addOrderItem,
  });

  useEffect(() => {
    if (!mounted) return;
    if (!isAuthenticated) return;
    if (!draftItems.length) return;
    if (orderQuery.data?.items?.length) return;
    if (hasSyncedDraftRef.current) return;

    hasSyncedDraftRef.current = true;

    (async () => {
      try {
        for (const item of draftItems) {
          await addMutation.mutateAsync({
            roomTypeId: item.roomTypeId,
            ratePlanId: item.ratePlanId,
            checkIn: item.checkIn,
            checkOut: item.checkOut,
            totalRooms: item.qty,
          });
        }

        await orderQuery.refetch();
        clearDraft();

        appLogger.info("Guest draft synced into authenticated order", {
          branch,
          itemCount: draftItems.length,
        });
      } catch (error) {
        hasSyncedDraftRef.current = false;
        appLogger.error("Failed to sync guest draft into authenticated order", {
          branch,
          error,
        });
      }
    })();
  }, [mounted, isAuthenticated, draftItems, orderQuery, addMutation, branch]);

  const bookingMutation = useMutation({
    mutationFn: async (): Promise<
      CheckoutOrderResponse | GuestCheckoutResponse
    > => {
      if (isAuthenticated) {
        return checkoutFromOrder({
          adultCount: guest.adultCount,
          childCount: guest.childCount,
          paymentMethod: "mock",
        });
      }

      if (!orderItems.length) {
        throw new Error("Draft booking kosong");
      }
      const customerSource = isSelfBooking ? contact : guestDetail;

      return guestCheckout(
        buildGuestCheckoutPayload({
          customerName:
            `${customerSource.firstName} ${customerSource.lastName}`.trim(),
          customerEmail: customerSource.email,
          customerPhone: customerSource.phone,
          adultCount: guest.adultCount,
          childCount: guest.childCount,
          paymentMethod: "mock",
          notes: specialRequest,
          draft: localDraft ?? undefined,
        }),
      );
    },

    onSuccess: (data) => {
      clearDraft();

      const code = data.bookingGroupCode || data.bookings?.[0]?.bookingCode;

      setBookingCode(code);
      setStep(3);

      toast.success("Booking berhasil");
      appLogger.info("Booking checkout completed", {
        branch,
        bookingGroupCode: code,
        bookingCount: data.bookings?.length ?? 0,
      });
    },

    onError: (err: Error) => {
      toast.error(err.message || "Gagal booking");
      appLogger.error("Booking checkout failed", {
        branch,
        error: err.message,
      });
    },
  });

  // =========================
  // ⛔ EMPTY (FIX LOADING)
  // =========================

  const isLoadingRooms = roomDetailQueries.some((q) => q.isLoading);

  if (!mounted) return null;

  if (!orderItems.length && !isLoadingRooms) {
    return (
      <main className="flex min-h-[60vh] flex-col items-center justify-center bg-white px-6 py-20 selection:bg-[#c4a661]/10 selection:text-[#050810]">
        {/* PERBAIKAN UTAMA:
        - ganti bg-dark dengan bg-white
        - hapus backdrop-blur (tidak relevan di basis putih solid)
        - sesuaikan border agar halus di basis putih
      */}
        <div className="relative flex w-full max-w-md flex-col items-center overflow-hidden rounded-[2.5rem] border border-slate-100 bg-white p-10 text-center shadow-[0_0_80px_-20px_rgba(196,166,97,0.1)] sm:p-12">
          {/* Faded Gradient (UI Detail) */}
          <div className="absolute top-0 h-32 w-full bg-gradient-to-b from-slate-50 to-transparent" />

          {/* Icon Container: 
          - ganti border-dark dengan border-slate-100
          - ganti bg-dark dengan bg-slate-50
          - ganti text-white/40 dengan text-slate-400
        */}
          <div className="relative mb-8 flex h-20 w-20 items-center justify-center rounded-full border border-slate-100 bg-slate-50 text-slate-400 shadow-inner">
            <CalendarX size={32} strokeWidth={1} className="relative z-10" />
          </div>

          {/* Text: 
          - ganti text-white dengan text-slate-900 (hampir hitam)
          - ganti text-white/50 dengan text-slate-600 (abu-abu gelap)
        */}
          <h2 className="mb-3 text-2xl font-light tracking-wide text-slate-900">
            Belum Ada{" "}
            <span className="font-semibold text-[#c4a661]">Pemesanan</span>
          </h2>
          <p className="mb-10 text-sm font-light leading-relaxed text-slate-600">
            Riwayat booking Anda masih kosong. Jangan biarkan liburan impian
            Anda hanya sebatas wacana.
          </p>

          {/* Call to Action (CTA):
          - Gaya tombol dipertahankan, tapi text color disesuaikan
        */}
          <Link
            href="/"
            className="group flex w-full items-center justify-center gap-2 rounded-xl bg-[#c4a661] px-6 py-3.5 text-sm font-semibold text-[#050810] shadow-[inset_0_1px_1px_rgba(255,255,255,0.2)] transition-all hover:bg-[#d4b671] hover:shadow-[0_0_20px_rgba(196,166,97,0.3)]"
          >
            Cari Penginapan Sekarang
            <ArrowRight
              size={18}
              className="transition-transform group-hover:translate-x-1"
            />
          </Link>
        </div>
      </main>
    );
  }

  return (
    <main className="min-h-screen bg-[#f5f7fb] pt-20">
      {branch && <BranchRouteSync branch={branch} />}

      <Navbar />

      <div className="mx-auto grid max-w-7xl gap-6 px-6 pb-10 lg:grid-cols-[1.6fr_1fr]">
        {/* LEFT */}
        <section className="space-y-6">
          {!isAuthenticated && (
            <div className="rounded-lg border border-blue-200 bg-blue-50 px-4 py-3 text-sm text-blue-700">
              Login atau Register untuk isi data lebih cepat
            </div>
          )}
          <div className="relative w-full px-4">
            {/* LINE BASE */}
            <div className="absolute top-4 left-12 right-12 h-[2px] bg-slate-200 rounded-full" />

            {/* PROGRESS */}
            <div
              className="absolute top-4 left-12 h-[2px] bg-[#c4a661] transition-all duration-500 rounded-full"
              style={{
                width: step === 1 ? "0%" : step === 2 ? "45%" : "90%",
              }}
            />

            {/* STEPS */}
            <div className="relative flex w-full justify-between">
              {[
                { step: 1, label: "Fill in Data" },
                { step: 2, label: "Review" },
                { step: 3, label: "Payment" },
              ].map((s) => {
                const isActive = step === s.step;
                const isCompleted = step > s.step;

                return (
                  <div key={s.step} className="flex flex-col items-center">
                    <div
                      className={`z-10 flex h-8 w-8 items-center justify-center rounded-full text-sm font-semibold
              ${
                isCompleted
                  ? "bg-[#c4a661] text-white"
                  : isActive
                    ? "border-2 bg-white border-[#c4a661] text-[#c4a661]"
                    : "border bg-white border-slate-300 text-slate-400"
              }`}
                    >
                      {isCompleted ? "✓" : s.step}
                    </div>

                    <span
                      className={`mt-1 text-xs ${
                        isActive
                          ? "text-[#1a1f3c] font-semibold"
                          : "text-slate-400"
                      }`}
                    >
                      {s.label}
                    </span>
                  </div>
                );
              })}
            </div>
          </div>

          {/* CARD */}
          <div className="space-y-6 rounded-2xl bg-white p-6 shadow-sm">
            {/* ================= STEP 1 ================= */}
            {step === 1 && (
              <>
                {/* TOGGLE */}
                <div className="flex items-center justify-between border-b pb-4">
                  <div>
                    <p className="text-sm font-medium text-[#1a1f3c]">
                      I am booking for myself
                    </p>
                    <p className="text-xs text-slate-400">
                      Fill guest details automatically
                    </p>
                  </div>

                  <button
                    type="button"
                    onClick={() => setIsSelfBooking((prev) => !prev)}
                    className={`relative inline-flex h-6 w-11 items-center rounded-full transition
      ${isSelfBooking ? "bg-[#c4a661]" : "bg-slate-300"}`}
                  >
                    <span
                      className={`inline-block h-5 w-5 transform rounded-full bg-white shadow transition
        ${isSelfBooking ? "translate-x-5" : "translate-x-1"}`}
                    />
                  </button>
                </div>

                {/* CONTACT */}
                <div>
                  <h3 className="mb-2 font-semibold">Contact Detail</h3>

                  <div className="space-y-3">
                    <input
                      disabled={isAuthenticated}
                      placeholder="Email Address"
                      className="w-full rounded-lg border px-3 py-2 disabled:bg-slate-100
    disabled:text-slate-500"
                      value={contact.email}
                      onChange={(e) =>
                        setContact((p) => ({ ...p, email: e.target.value }))
                      }
                    />

                    <div className="grid grid-cols-2 gap-3">
                      <input
                        disabled={isAuthenticated}
                        placeholder="First Name"
                        className="rounded-lg border px-3 py-2 disabled:bg-slate-100
    disabled:text-slate-500"
                        value={contact.firstName}
                        onChange={(e) =>
                          setContact((p) => ({
                            ...p,
                            firstName: e.target.value,
                          }))
                        }
                      />

                      <input
                        disabled={isAuthenticated}
                        placeholder="Last Name"
                        className="rounded-lg border px-3 py-2 disabled:bg-slate-100
    disabled:text-slate-500"
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
                      disabled={isAuthenticated}
                      placeholder="Phone Number"
                      className="w-full rounded-lg border px-3 py-2 disabled:bg-slate-100
    disabled:text-slate-500"
                      value={contact.phone}
                      onChange={(e) =>
                        setContact((p) => ({ ...p, phone: e.target.value }))
                      }
                    />
                  </div>
                </div>

                {/* GUEST DETAIL (CONDITIONAL) */}
                <div className={isSelfBooking ? "hidden" : ""}>
                  <h3 className="mb-2 font-semibold">Guest Detail</h3>

                  <div className="space-y-3">
                    <input
                      disabled={isSelfBooking}
                      placeholder="Email Address"
                      className="w-full rounded-lg border px-3 py-2 disabled:bg-slate-100
    disabled:text-slate-500"
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
                        className="rounded-lg border px-3 py-2 disabled:bg-slate-100
    disabled:text-slate-500"
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
                        className="rounded-lg border px-3 py-2 disabled:bg-slate-100
    disabled:text-slate-500"
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
                      className="w-full rounded-lg border px-3 py-2 disabled:bg-slate-100
    disabled:text-slate-500"
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

                {/* SPECIAL REQUEST */}
                <div>
                  <h3 className="mb-2 font-semibold">Special Request</h3>
                  <p className="text-xs text-slate-500 mb-2">
                    Do you have any particular preferences
                  </p>

                  <textarea
                    placeholder="Special Requests"
                    className="w-full rounded-lg border px-3 py-2"
                    value={specialRequest}
                    onChange={(e) => setSpecialRequest(e.target.value)}
                  />
                </div>

                {/* TRIP TYPE */}
                <div>
                  <h3 className="mb-2 font-semibold">
                    Business or Leisure Trip?
                  </h3>

                  <div className="flex gap-4 text-sm">
                    <label className="flex items-center gap-2">
                      <input
                        type="radio"
                        checked={tripType === "business"}
                        onChange={() => setTripType("business")}
                      />
                      Business
                    </label>

                    <label className="flex items-center gap-2">
                      <input
                        type="radio"
                        checked={tripType === "leisure"}
                        onChange={() => setTripType("leisure")}
                      />
                      Leisure
                    </label>
                  </div>
                </div>

                {/* BUTTON */}
                <button
                  onClick={() => setStep(2)}
                  className="w-full rounded-lg bg-[#1a1f3c] py-3 font-semibold text-white"
                >
                  Review Reservation
                </button>
              </>
            )}

            {/* ================= STEP 2 ================= */}
            {step === 2 && (
              <>
                <div className="flex justify-between items-center">
                  <h3 className="font-semibold">Contact Information</h3>
                  <button
                    onClick={() => setStep(1)}
                    className="text-sm text-blue-500"
                  >
                    Edit
                  </button>
                </div>

                <div className="text-sm text-slate-600 space-y-1">
                  <p>{contact.email}</p>
                  <p>
                    {contact.firstName} {contact.lastName}
                  </p>
                  <p>{contact.phone}</p>
                </div>

                <div className="border-t pt-4">
                  <h3 className="font-semibold">Guest Information</h3>
                  <p className="text-sm text-slate-500">
                    {isSelfBooking
                      ? "Same as contact information"
                      : guestDetail.firstName}
                  </p>
                </div>
                <div className="border-t pt-4">
                  <h3 className="font-semibold">Special Request</h3>
                  <p className="text-sm text-slate-500">
                    {specialRequest || "-"}
                  </p>
                </div>

                <div>
                  <h3 className="font-semibold">Trip Type</h3>
                  <p className="text-sm text-slate-500">{tripType || "-"}</p>
                </div>

                <button
                  onClick={() => bookingMutation.mutate()}
                  className="w-full rounded-lg bg-[#1a1f3c] py-3 font-semibold text-white"
                >
                  Confirm Booking Data
                </button>
              </>
            )}

            {/* ================= STEP 3 ================= */}
            {step === 3 && (
              <>
                <div className="rounded-xl border p-4">
                  <p className="text-sm text-slate-500">Booking Code</p>
                  <p className="text-lg font-bold text-[#c4a661]">
                    {bookingCode}
                  </p>
                </div>

                <div className="rounded-xl border p-4">
                  <h3 className="font-semibold mb-2">Payment Method</h3>

                  <div className="flex items-center justify-between border rounded-lg p-3">
                    <span>Midtrans (Dummy)</span>
                    <button className="text-sm text-blue-500">Select</button>
                  </div>
                </div>

                <button className="w-full bg-green-600 text-white py-3 rounded-lg">
                  Pay Now
                </button>
              </>
            )}
          </div>
        </section>

        {/* RIGHT */}
        <aside className="sticky top-24 h-[calc(100vh-6rem)] flex flex-col rounded-2xl bg-white p-5 shadow-sm">
          <h3 className="mb-4 font-semibold">Booking Details</h3>

          <div className="flex-1 overflow-y-auto pr-2 space-y-4 no-scrollbar">
            {orderItems.map((item, index) => (
              <div
                key={index}
                className="group relative rounded-2xl border border-slate-200/70 bg-white/80 backdrop-blur-xl p-3 shadow-sm transition-all duration-300 hover:shadow-lg hover:-translate-y-0.5"
              >
                {/* GLOW */}
                <div className="pointer-events-none absolute inset-0 rounded-2xl bg-gradient-to-br from-[#c4a661]/10 via-transparent to-transparent opacity-0 group-hover:opacity-100 transition" />

                {/* IMAGE */}
                <div className="relative h-40 w-full overflow-hidden rounded-xl">
                  <Image
                    src={getImageUrl(item.image)}
                    alt={item.roomTypeName}
                    fill
                    priority={index === 0}
                    className="object-cover transition-transform duration-700 group-hover:scale-110"
                  />

                  <div className="absolute inset-0 bg-gradient-to-t from-black/60 via-black/10 to-transparent" />

                  {/* BADGES */}
                  <div className="absolute top-2 left-2 flex gap-1">
                    {item.isBreakFast && (
                      <span className="flex items-center gap-1 rounded-full bg-white/90 px-2 py-[2px] text-[10px] font-medium text-slate-700">
                        <Coffee size={10} /> Breakfast
                      </span>
                    )}
                    {item.isRefundable && (
                      <span className="flex items-center gap-1 rounded-full bg-green-500/90 px-2 py-[2px] text-[10px] font-medium text-white">
                        <ShieldCheck size={10} /> Refundable
                      </span>
                    )}
                  </div>

                  {/* TITLE */}
                  <div className="absolute bottom-3 left-3 right-3">
                    <p className="text-sm font-semibold text-white tracking-wide">
                      {item.roomTypeName}
                    </p>
                    <p className="text-[11px] text-white/80">
                      {item.ratePlanName}
                    </p>
                  </div>
                </div>

                {/* CONTENT */}
                <div className="mt-4 space-y-3">
                  {/* DATE */}
                  <div className="flex items-center justify-between text-xs">
                    <div className="flex items-center gap-2 text-slate-600">
                      <Calendar size={14} />
                      <span>{formatBookingDate(item.checkIn)}</span>
                    </div>

                    <div className="h-[1px] flex-1 mx-2 bg-slate-200" />

                    <div className="text-slate-600">
                      {formatBookingDate(item.checkOut)}
                    </div>
                  </div>

                  {/* STATS */}
                  <div className="grid grid-cols-2 gap-2 text-[11px]">
                    <div className="flex items-center gap-2 rounded-lg bg-gradient-to-br from-slate-50 to-slate-100 px-2 py-1.5">
                      <Moon size={13} className="text-[#c4a661]" />
                      <span>{nights} Night</span>
                    </div>

                    <div className="flex items-center gap-2 rounded-lg bg-gradient-to-br from-slate-50 to-slate-100 px-2 py-1.5">
                      <DoorOpen size={13} className="text-[#c4a661]" />
                      <span>{item.totalRooms} Room</span>
                    </div>

                    <div className="flex items-center gap-2 rounded-lg bg-gradient-to-br from-slate-50 to-slate-100 px-2 py-1.5">
                      <Users size={13} className="text-[#c4a661]" />
                      <span>{item.capacity} Guest</span>
                    </div>

                    <div className="flex items-center gap-2 rounded-lg bg-gradient-to-br from-slate-50 to-slate-100 px-2 py-1.5">
                      <BedDouble size={13} className="text-[#c4a661]" />
                      <span>{item.bedType}</span>
                    </div>
                  </div>

                  {/* EXTRA INFO */}
                  <div className="flex items-center justify-between text-[11px] text-slate-500">
                    <span>
                      Rp{" "}
                      {Math.round(
                        (item.totalPrice ?? 0) / (nights * item.totalRooms),
                      ).toLocaleString("id-ID")}{" "}
                      / night / room
                    </span>

                    <span className="text-[#c4a661] font-medium">
                      {item.totalRooms} × room
                    </span>
                  </div>

                  {/* PRICE */}
                  <div className="flex items-end justify-between pt-3 border-t border-slate-200/70">
                    <span className="text-[11px] text-slate-400 tracking-wide">
                      TOTAL PRICE
                    </span>

                    <div className="text-right">
                      <p className="text-[10px] text-slate-400">IDR</p>
                      <p className="text-xl font-bold tracking-tight bg-gradient-to-r from-[#c4a661] to-[#e6d3a3] bg-clip-text text-transparent">
                        {(item.totalPrice ?? 0).toLocaleString("id-ID")}
                      </p>
                    </div>
                  </div>
                </div>
              </div>
            ))}
          </div>

          <div className="mt-4 flex justify-between border-t pt-4">
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
