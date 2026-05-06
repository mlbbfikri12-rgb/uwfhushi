"use client";

import Image from "next/image";
import Link from "next/link";
import { Key, useMemo } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import Script from "next/script";
import * as Icons from "lucide-react";
import type { LucideIcon } from "lucide-react";
import { BranchRouteSync } from "@/features/tenant/components/BranchRouteSync";
import { BranchSearchForm } from "@/features/tenant/components/BranchSearchForm";
import { Navbar } from "@/components/layout/navbar";
import { addOrderItem, getCurrentOrder } from "@/services/order.service";
import { getHotelPricing, getRoomDetail } from "@/services/hotel.service";
import { getCurrentCustomer } from "@/services/auth.service";
import { Facility, Hotel, UIRatePlan, UIRoomType } from "@/types/hotel";
import { PricingRatePlan, PricingRoom } from "@/types/admin-rateplan";
import { BENEFIT_MAP } from "@/utils/BenefitsMap";
import { useRouter } from "next/navigation";
import { addToBookingDraft } from "@/utils/BookingDraftUtils";

type Props = {
  slug: string;
  checkIn: string;
  checkOut: string;
  totalRooms: number;
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
  totalRooms,
  hotel,
}: Props) {
  const router = useRouter();
  const queryClient = useQueryClient();

  const customerQuery = useQuery({
    queryKey: ["customer-me"],
    queryFn: getCurrentCustomer,
    retry: false,
  });

  const isCustomerAuthenticated =
    customerQuery.isSuccess && Boolean(customerQuery.data?.id);

  const pricingQuery = useQuery<PricingRoom[]>({
    queryKey: ["pricing", slug, checkIn, checkOut],
    queryFn: () => getHotelPricing({ slug, checkIn, checkOut }),
    enabled: Boolean(slug && checkIn && checkOut),
  });

  const orderQuery = useQuery({
    queryKey: ["order-current", slug],
    queryFn: getCurrentOrder,
    enabled: isCustomerAuthenticated,
  });

  const addMutation = useMutation({
    mutationFn: addOrderItem,
    onSuccess: () => orderQuery.refetch(),
  });

  const images = useMemo(() => hotel.images ?? [], [hotel.images]);
  const facilities = useMemo(() => hotel.facilities ?? [], [hotel.facilities]);
  const nearby = useMemo(() => hotel.nearby ?? [], [hotel.nearby]);
  //const order = orderQuery.data;

  const imageList = useMemo(() => images.slice(0, 5), [images]);

  const sortedNearby = useMemo(
    () => [...nearby].sort((a, b) => a.distanceKm - b.distanceKm),
    [nearby],
  );

  const handleSelect = async (roomType: UIRoomType, ratePlan: UIRatePlan) => {
    const draft = {
      roomTypeId: roomType.id,
      roomTypeName: roomType.name,

      ratePlanId: ratePlan.id,
      ratePlanName: ratePlan.name,

      price: ratePlan.price,

      checkIn,
      checkOut,
      totalRooms,
      slug,

      // 🔥 TAMBAHAN (BIAR BOOKING PAGE GAK KOSONG)
      image: roomType.image,
      capacity: roomType.capacity,
      bedType: roomType.bedType,
    };

    // ✅ save ke local
    addToBookingDraft(draft);

    // 🔥 prefetch room detail (biar booking page instant)
    await queryClient.prefetchQuery({
      queryKey: ["room-detail", slug, roomType.id],

      queryFn: () => getRoomDetail(slug, roomType.id),
    });

    // ✅ clean URL
    router.push("/booking");

    // ✅ clean URL (tanpa branch query)
    router.push("/booking");
  };

  const priceFrom = hotel.priceFrom ?? 0;

  // 🔥 mapping pricing → roomTypes lama
  const roomTypes = useMemo<UIRoomType[]>(() => {
    if (!pricingQuery.data) return [];

    return pricingQuery.data.map(
      (room: PricingRoom): UIRoomType => ({
        id: room.roomTypeId,
        name: room.name,
        description: room.description ?? "",
        image: room.image ?? "",
        capacity: room.capacity ?? 0,
        bedType: room.bedType ?? "",
        size: room.size ?? 0,

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
          }),
        ),
      }),
    );
  }, [pricingQuery.data]);

  const redirectToLogin = () => {
    const redirect = encodeURIComponent(window.location.href);
    window.location.href = `/login?redirect=${redirect}`;
  };

  const getLowestPrice = (roomType: UIRoomType) => {
    if (!roomType.ratePlans.length) return 0;
    return Math.min(...roomType.ratePlans.map((rp) => rp.price));
  };

  return (
    <main className="min-h-screen bg-slate-50">
      {hotel?.branchCode && <BranchRouteSync branch={hotel.branchCode} />}
      <Navbar />

      <div className="border-b border-slate-100 bg-white pt-16">
        <div className="mx-auto flex max-w-7xl items-center gap-2 px-6 py-3 text-xs text-slate-500">
          <Link href="/">Home</Link>
          <span>/</span>
          <span>Hotel</span>
          <span>/</span>
          <span>{slug}</span>
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

                <button className="mt-2 px-4 py-2 bg-[#1a1f3c] text-white rounded-lg text-sm font-semibold">
                  Select Room
                </button>
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
                    unoptimized
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
                        unoptimized
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
            <section>
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
                              unoptimized
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
                                  {roomType.facilities.slice(0, 4).map((f) => (
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
                                    Rp {ratePlan.price.toLocaleString("id-ID")}
                                  </p>

                                  <p className="text-xs text-slate-400">
                                    /room/night
                                  </p>

                                  <button
                                    onClick={() =>
                                      handleSelect(roomType, ratePlan)
                                    }
                                    className="mt-3 w-full rounded-lg bg-[#1a1f3c] px-4 py-2 text-sm font-semibold text-white hover:bg-[#252c52] transition"
                                  >
                                    Select
                                  </button>
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
  );
}
