"use client";

import Image from "next/image";
import Link from "next/link";
import { Key, useMemo, useState, useEffect } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import Script from "next/script";
import * as Icons from "lucide-react";
import type { LucideIcon } from "lucide-react";
import { BranchRouteSync } from "@/features/tenant/components/BranchRouteSync";
import BranchSearchForm from "@/features/tenant/components/BranchSearchForm";
import { Navbar } from "@/components/layout/navbar";
import { getHotelPricing, getRoomDetail } from "@/services/hotel.service";
import { Facility, Hotel, UIRatePlan, UIRoomType } from "@/types/hotel";
import { PricingRatePlan, PricingRoom } from "@/types/admin-rateplan";
import { BENEFIT_MAP } from "@/utils/BenefitsMap";
import { useRouter } from "next/navigation";
import { clearDraft, getDraft, saveDraft } from "@/utils/BookingDraftUtils";

type Props = {
  slug: string;
  checkIn: string;
  checkOut: string;
  // totalRooms: number;
  hotel: Hotel;
};

function formatDistance(distanceKm: number) {
  if (!Number.isFinite(distanceKm)) return "-";
  if (distanceKm < 1) return `${Math.round(distanceKm * 1000)} m`;
  return `${distanceKm.toFixed(1)} km`;
}

function toImageUrl(url: string) {
  return /^https?:\/\//i.test(url)
    ? url
    : "https://images.unsplash.com/photo-1566665797739-1674de7a421a?w=1200&q=80";
}

export default function HotelPageClient({
  slug,
  checkIn,
  checkOut,
  //totalRooms,
  hotel,
}: Props) {
  const router = useRouter();
  const queryClient = useQueryClient();

  type SelectedItem = {
    roomTypeId: string;
    ratePlanId: string;
    qty: number;
  };

  const [selected, setSelected] = useState<SelectedItem[]>([]);

  const pricingQuery = useQuery<PricingRoom[]>({
    queryKey: ["pricing", slug, checkIn, checkOut],
    queryFn: () => getHotelPricing({ slug, checkIn, checkOut }),
    enabled: Boolean(slug && checkIn && checkOut),
  });

  const images = useMemo(() => hotel.images ?? [], [hotel.images]);
  const facilities = useMemo(() => hotel.facilities ?? [], [hotel.facilities]);
  const nearby = useMemo(() => hotel.nearby ?? [], [hotel.nearby]);

  const imageList = useMemo(() => images.slice(0, 5), [images]);

  const sortedNearby = useMemo(
    () => [...nearby].sort((a, b) => a.distanceKm - b.distanceKm),
    [nearby],
  );

  const priceFrom = hotel.priceFrom ?? 0;

  // =========================
  // 🔥 DEFINE roomTypes DULU (IMPORTANT FIX)
  // =========================
  const roomTypes = useMemo<UIRoomType[]>(() => {
    if (!pricingQuery.data) {
      return [];
    }

    return pricingQuery.data.map(
      (room: PricingRoom): UIRoomType => ({
        id: room.roomTypeId,
        name: room.name,
        description: room.description ?? "",
        image: room.image ?? "",
        capacity: room.capacity ?? 0,
        bedType: room.bedType ?? "",
        size: room.size ?? 0,
        MaxAdults: room.maxAdults ?? 0,
        MaxChildren: room.maxChildren ?? 0,
        facilities: (room.facilities ?? []).map(
          (f: string): Facility => ({
            name: f,
            icon: null,
          }),
        ),

        ratePlans: room.ratePlans.map(
          (rp: PricingRatePlan): UIRatePlan => ({
            id: rp.id,
            name: rp.name,
            benefits: rp.benefits ?? [],
            terms: rp.termsPreview ?? "",
            price: rp.price,
            isBreakFast: rp.benefits?.includes("BREAKFAST") ?? false,

            isRefundable: rp.benefits?.includes("CANCELABLE") ?? false,
          }),
        ),
      }),
    );
  }, [pricingQuery.data]);

  console.log(roomTypes);

  // =========================
  // HELPERS
  // =========================

  const isSelected = (roomTypeId: string, ratePlanId: string) =>
    selected.some(
      (x) => x.roomTypeId === roomTypeId && x.ratePlanId === ratePlanId,
    );

  const getSelectedItem = (roomTypeId: string, ratePlanId: string) =>
    selected.find(
      (x) => x.roomTypeId === roomTypeId && x.ratePlanId === ratePlanId,
    );

  const isSameRoomType = (roomTypeId: string) =>
    selected.some((x) => x.roomTypeId === roomTypeId);

  // =========================
  // LOAD + VALIDATE DRAFT
  // =========================

  useEffect(() => {
    const draft = getDraft();

    if (!draft || !draft.items?.length) return;

    if (
      draft.slug !== slug ||
      draft.checkIn !== checkIn ||
      draft.checkOut !== checkOut
    ) {
      clearDraft();
      setSelected([]);
    }
  }, [slug, checkIn, checkOut]);

  useEffect(() => {
    const draft = getDraft();

    if (!draft || !draft.items?.length) return;

    setSelected(
      draft.items.map((item) => ({
        roomTypeId: item.roomTypeId,
        ratePlanId: item.ratePlanId,
        qty: item.qty,
      })),
    );
  }, []);

  // =========================
  // 🔥 SYNC KE LOCAL STORAGE
  // =========================

  useEffect(() => {
    if (!selected.length) {
      clearDraft();
      return;
    }

    const items = selected.map((item) => {
      const room = roomTypes.find((r) => r.id === item.roomTypeId);
      const rp = room?.ratePlans.find((r) => r.id === item.ratePlanId);

      return {
        roomTypeId: item.roomTypeId,
        ratePlanId: item.ratePlanId,
        roomTypeName: room?.name,
        ratePlanName: rp?.name,
        imageUrl: room?.image,
        qty: item.qty,
        price: rp?.price,
        MaxAdults: room?.MaxAdults,
        MaxChildren: room?.MaxChildren,
        isBreakFast: rp?.isBreakFast,
        isRefundable: rp?.isRefundable,
      };
    });

    saveDraft({
      slug,
      checkIn,
      checkOut,
      items,
    });
  }, [selected, roomTypes, slug, checkIn, checkOut]);

  // =========================
  // ACTIONS
  // =========================

  const handleSelectRateplan = (roomType: UIRoomType, ratePlan: UIRatePlan) => {
    setSelected((prev) => {
      const exist = prev.find(
        (x) => x.roomTypeId === roomType.id && x.ratePlanId === ratePlan.id,
      );

      if (exist) {
        return prev.map((x) =>
          x.roomTypeId === roomType.id && x.ratePlanId === ratePlan.id
            ? { ...x, qty: x.qty + 1 }
            : x,
        );
      }

      return [
        ...prev,
        {
          roomTypeId: roomType.id,
          ratePlanId: ratePlan.id,
          qty: 1,
        },
      ];
    });
  };

  const incrementQty = (roomTypeId: string, ratePlanId: string) => {
    setSelected((prev) =>
      prev.map((item) =>
        item.roomTypeId === roomTypeId && item.ratePlanId === ratePlanId
          ? { ...item, qty: item.qty + 1 }
          : item,
      ),
    );
  };

  const decrementQty = (roomTypeId: string, ratePlanId: string) => {
    setSelected((prev) =>
      prev
        .map((item) =>
          item.roomTypeId === roomTypeId && item.ratePlanId === ratePlanId
            ? { ...item, qty: item.qty - 1 }
            : item,
        )
        .filter((item) => item.qty > 0),
    );
  };

  // =========================
  // CONTINUE BOOKING
  // =========================

  const handleContinueBooking = async () => {
    if (!selected.length) return;

    const firstRoomType = selected[0]?.roomTypeId;

    if (firstRoomType) {
      await queryClient.prefetchQuery({
        queryKey: ["room-detail", slug, firstRoomType],
        queryFn: () => getRoomDetail(slug, firstRoomType),
      });
    }

    router.push("/booking");
  };

  // =========================
  // TOTAL
  // =========================

  const totalRoomsSelected = selected.reduce((acc, x) => acc + x.qty, 0);

  const totalPrice = selected.reduce((acc, item) => {
    const room = roomTypes.find((r) => r.id === item.roomTypeId);
    const rp = room?.ratePlans.find((r) => r.id === item.ratePlanId);

    return acc + (rp?.price ?? 0) * item.qty;
  }, 0);

  return (
    <>
      <Navbar />
      <main className="min-h-screen bg-slate-50">
        {hotel?.branchCode && <BranchRouteSync branch={hotel.branchCode} />}
        <div className="border-b border-slate-100 bg-white pt-16">
          <div className="mx-auto max-w-7xl px-6 py-4">
            <div className="flex items-center gap-2 text-sm text-slate-500">
              {/* HOME */}
              <Link href="/" className="hover:text-slate-900 transition-colors">
                Home
              </Link>

              <Icons.ChevronRight size={14} className="text-slate-400" />

              {/* CITY */}
              <Link
                href={`/search?q=${encodeURIComponent(hotel.city)}`}
                className="hover:text-slate-900 transition-colors"
              >
                {hotel.city}
              </Link>

              <Icons.ChevronRight size={14} className="text-slate-400" />

              {/* HOTEL */}
              <span className="font-semibold text-slate-900 max-w-[250px] truncate">
                {hotel.name}
              </span>
            </div>
          </div>
        </div>
        <div className="mx-auto max-w-7xl px-6 py-6">
          {pricingQuery.isLoading && (
            <p className="text-sm text-slate-500">Memuat room...</p>
          )}

          {hotel && (
            <>
              <Script
                id="hotel-jsonld"
                type="application/ld+json"
                dangerouslySetInnerHTML={{
                  __html: JSON.stringify({
                    "@context": "https://schema.org",
                    "@type": "Hotel",
                    name: hotel.name,
                  }),
                }}
              />

              {/* HEADER */}
              <div className="flex justify-between items-start mb-8">
                <div>
                  <h1 className="text-2xl font-bold">{hotel.name}</h1>
                  <p className="text-sm text-slate-500">{hotel.city}</p>
                </div>

                <div className="text-right">
                  <p className="text-xs text-slate-400">Starts from</p>

                  <p className="text-2xl font-bold text-[#c4a661]">
                    Rp {priceFrom.toLocaleString("id-ID")}
                  </p>

                  <Link
                    href="#rooms"
                    className="mt-2 inline-block px-4 py-2 bg-[#1a1f3c] text-white rounded-lg text-sm font-semibold hover:bg-[#2a3160] transition"
                  >
                    Select Room
                  </Link>
                </div>
              </div>

              {/* IMAGES */}
              {imageList.length > 0 && (
                <div className="mb-8 grid h-[380px] grid-cols-4 grid-rows-2 gap-2">
                  <div className="relative col-span-2 row-span-2 overflow-hidden rounded-l-xl">
                    <Image
                      src={toImageUrl(imageList[0])}
                      alt={hotel.name}
                      fill
                      className="object-cover"
                    />
                  </div>
                  {imageList
                    .slice(1)
                    .map((image: string, index: Key | null | undefined) => (
                      <div key={index} className="relative overflow-hidden">
                        <Image
                          src={toImageUrl(image)}
                          alt={hotel.name}
                          fill
                          className="object-cover"
                        />
                      </div>
                    ))}
                </div>
              )}

              <div className="mb-10">
                <BranchSearchForm
                  branch={hotel.branchCode}
                  initialKeyword={hotel.name} // 🔥 INI KUNCINYA
                  hideSearchButton
                />
              </div>

              {/* OVERVIEW */}
              <section className="mb-10">
                <h2 className="mb-3 text-xl font-bold text-slate-800">
                  Overview
                </h2>
                <p className="max-w-4xl leading-relaxed text-slate-600">
                  {hotel.description}
                </p>
              </section>

              {/* FACILITIES */}
              <section className="mb-10">
                <h2 className="mb-4 text-xl font-bold text-slate-800">
                  Facilities
                </h2>
                <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 md:grid-cols-4">
                  {facilities.map((facility: Facility) => {
                    const Icon = Icons[facility.icon as keyof typeof Icons] as
                      | LucideIcon
                      | undefined;

                    return (
                      <div
                        key={facility.name}
                        className="group flex items-center gap-3 rounded-2xl border border-[#e2e8f0] bg-white p-4 transition-all duration-200 hover:-translate-y-[2px] hover:shadow-md"
                      >
                        <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-[#f8fafc] transition group-hover:bg-[#1a1f3c]/5">
                          {Icon ? (
                            <Icon size={18} className="text-[#1a1f3c]" />
                          ) : (
                            <span className="text-xs text-[#94a3b8]">?</span>
                          )}
                        </div>

                        <span className="text-sm font-medium text-[#0f172a]">
                          {facility.name}
                        </span>
                      </div>
                    );
                  })}
                </div>
              </section>

              {/* MAP + NEARBY */}
              <section className="mb-10 grid gap-6 md:grid-cols-2">
                <iframe
                  src={`https://maps.google.com/maps?q=${hotel.latitude},${hotel.longitude}&z=15&output=embed`}
                  className="h-64 w-full rounded-xl border-0"
                />

                <div className="grid grid-cols-1 gap-2 sm:grid-cols-2 md:grid-cols-2 lg:grid-cols-2">
                  {sortedNearby.map((item) => (
                    <div
                      key={item.name}
                      className="group rounded-2xl border border-[#e2e8f0] bg-white p-4 transition-all duration-200 hover:-translate-y-[2px] hover:shadow-md"
                    >
                      <div className="text-sm font-medium text-[#0f172a] leading-snug">
                        {item.name}
                      </div>

                      <div className="mt-2 text-sm font-semibold text-[#c4a661]">
                        {formatDistance(item.distanceKm)}
                      </div>
                    </div>
                  ))}
                </div>
              </section>

              {/* ROOMS (LAYOUT SAMA) */}
              <section id="rooms" className="scroll-mt-24">
                <h2 className="mb-4 text-xl font-bold text-slate-800">
                  Select Your Room
                </h2>

                <div className="space-y-6">
                  {roomTypes.map((roomType: UIRoomType) => {
                    const lowestPrice =
                      roomType.ratePlans.length > 0
                        ? Math.min(...roomType.ratePlans.map((rp) => rp.price))
                        : 0;

                    return (
                      <div
                        key={roomType.id}
                        className="grid grid-cols-1 md:grid-cols-3 gap-4 items-start bg-slate-100 p-4 rounded-2xl border border-slate-200"
                      >
                        {/* ================= LEFT: ROOM CARD ================= */}
                        <div className="relative">
                          <div className="rounded-xl border border-slate-200 bg-white overflow-hidden sticky top-24">
                            {/* IMAGE */}
                            <div className="relative h-48 w-full">
                              <Image
                                src={toImageUrl(roomType.image)}
                                alt={roomType.name}
                                fill
                                className="object-cover"
                              />

                              {/* ROOM BADGE */}
                              <div className="absolute top-2 left-2 z-10 rounded-full bg-white/90 backdrop-blur px-3 py-1 text-xs font-semibold text-slate-700 border">
                                {roomType.name}
                              </div>
                            </div>

                            {/* CONTENT */}
                            <div className="p-4">
                              <h3 className="text-lg font-semibold text-slate-900">
                                {roomType.name}
                              </h3>

                              {roomType.description && (
                                <p className="text-sm text-slate-500 mt-1">
                                  {roomType.description}
                                </p>
                              )}

                              <p className="text-sm text-slate-500 mt-2">
                                {roomType.capacity ?? 2} guests •{" "}
                                {roomType.bedType || "Standard Bed"}
                              </p>

                              {/* FACILITIES */}
                              {roomType.facilities?.length > 0 && (
                                <div className="mt-3 border-t pt-3">
                                  <p className="text-sm font-medium text-slate-700">
                                    Amenities
                                  </p>

                                  <div className="mt-2 flex flex-wrap gap-2 text-xs">
                                    {roomType.facilities
                                      .slice(0, 4)
                                      .map((f) => (
                                        <span
                                          key={f.name}
                                          className="bg-slate-100 px-2 py-1 rounded text-slate-600"
                                        >
                                          {f.name}
                                        </span>
                                      ))}
                                  </div>
                                </div>
                              )}

                              <button className="mt-4 w-full rounded-lg bg-[#1a1f3c] text-white py-2 text-sm font-semibold hover:bg-[#252c52] transition">
                                See Room Detail
                              </button>
                            </div>
                          </div>
                        </div>

                        {/* ================= RIGHT: RATE PLANS ================= */}
                        <div className="md:col-span-2 space-y-3 border-l pl-4 border-slate-200">
                          {roomType.ratePlans.length === 0 ? (
                            <div className="rounded-xl border border-slate-200 bg-white p-4 text-sm text-slate-500">
                              No available rate for this room
                            </div>
                          ) : (
                            roomType.ratePlans.map((ratePlan: UIRatePlan) => {
                              const isBest = ratePlan.price === lowestPrice;

                              const isDisabled =
                                isSameRoomType(roomType.id) &&
                                !isSelected(roomType.id, ratePlan.id);
                              const selectedItem = getSelectedItem(
                                roomType.id,
                                ratePlan.id,
                              );

                              return (
                                <div
                                  key={ratePlan.id}
                                  className={`rounded-xl border p-4 flex flex-col md:flex-row justify-between gap-4 transition
                      ${
                        isBest
                          ? "border-green-400 bg-green-50"
                          : "border-slate-200 bg-white"
                      }`}
                                >
                                  {/* LEFT */}
                                  <div className="flex-1">
                                    <div className="flex items-center gap-2">
                                      <p className="font-semibold text-slate-900">
                                        {ratePlan.name}
                                      </p>

                                      {isBest && (
                                        <span className="text-[10px] font-bold text-green-700 bg-green-100 px-2 py-0.5 rounded">
                                          BEST DEAL
                                        </span>
                                      )}
                                    </div>

                                    {/* BENEFITS */}
                                    <div className="mt-2 flex flex-wrap gap-2">
                                      {ratePlan.benefits?.map((code) => {
                                        const benefit =
                                          BENEFIT_MAP[
                                            code as keyof typeof BENEFIT_MAP
                                          ];
                                        if (!benefit) return null;

                                        const Icon = benefit.icon;

                                        return (
                                          <span
                                            key={code}
                                            className="flex items-center gap-1 text-xs bg-slate-100 px-2 py-1 rounded text-slate-700"
                                          >
                                            <Icon size={12} />
                                            {benefit.label}
                                          </span>
                                        );
                                      })}
                                    </div>

                                    {/* TERMS */}
                                    {ratePlan.terms && (
                                      <p className="text-xs text-slate-400 mt-2 line-clamp-2">
                                        {ratePlan.terms}
                                      </p>
                                    )}
                                  </div>

                                  {/* RIGHT */}
                                  <div className="text-right min-w-[160px]">
                                    <p className="text-xl font-bold text-[#c4a661]">
                                      Rp{" "}
                                      {ratePlan.price.toLocaleString("id-ID")}
                                    </p>

                                    <p className="text-xs text-slate-400">
                                      /room/night
                                    </p>

                                    {selectedItem ? (
                                      <div className="mt-3 flex items-center justify-between rounded-lg border border-slate-300 overflow-hidden">
                                        <button
                                          className="px-4 py-2 text-lg font-bold text-slate-700 hover:bg-slate-100"
                                          onClick={() =>
                                            decrementQty(
                                              roomType.id,
                                              ratePlan.id,
                                            )
                                          }
                                        >
                                          -
                                        </button>

                                        <span className="px-4 text-sm font-semibold">
                                          {selectedItem.qty}
                                        </span>

                                        <button
                                          onClick={() =>
                                            incrementQty(
                                              roomType.id,
                                              ratePlan.id,
                                            )
                                          }
                                          className="px-4 py-2 text-lg font-bold text-slate-700 hover:bg-slate-100"
                                        >
                                          +
                                        </button>
                                      </div>
                                    ) : (
                                      <button
                                        disabled={isDisabled}
                                        onClick={() =>
                                          handleSelectRateplan(
                                            roomType,
                                            ratePlan,
                                          )
                                        }
                                        className={`mt-3 w-full rounded-lg px-4 py-2 text-sm font-semibold transition
    ${
      isDisabled
        ? "bg-slate-300 cursor-not-allowed"
        : "bg-[#1a1f3c] text-white hover:bg-[#252c52]"
    }
  `}
                                      >
                                        Select
                                      </button>
                                    )}
                                  </div>
                                </div>
                              );
                            })
                          )}
                        </div>
                      </div>
                    );
                  })}
                </div>
              </section>
            </>
          )}
        </div>
      </main>
      {/* <Footer /> */}
      {selected.length > 0 && (
        <div className="fixed bottom-6 right-6 z-50">
          <div className="flex items-center gap-4 rounded-2xl bg-white shadow-xl border border-slate-200 px-5 py-4">
            {/* SUMMARY */}
            <div className="text-right">
              <p className="text-xs text-slate-400">
                {totalRoomsSelected} room
              </p>

              <p className="text-sm font-semibold text-[#c4a661]">
                Rp {totalPrice.toLocaleString("id-ID")}
              </p>
            </div>

            {/* BUTTON */}
            <button
              onClick={handleContinueBooking}
              className="flex items-center gap-2 rounded-xl bg-[#1a1f3c] px-5 py-3 text-sm font-semibold text-white hover:bg-[#252c52] transition shadow-md"
            >
              {/* ICON */}
              <Icons.ShoppingCart size={16} />
              Continue Booking
            </button>
          </div>
        </div>
      )}
    </>
  );
}
