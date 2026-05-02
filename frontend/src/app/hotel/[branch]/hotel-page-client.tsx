"use client";

import Image from "next/image";
import Link from "next/link";
import { useMemo, useState } from "react";
import { useMutation, useQuery } from "@tanstack/react-query";
import Script from "next/script";
import * as Icons from "lucide-react";
import type { LucideIcon } from "lucide-react";
import { BranchRouteSync } from "@/features/tenant/components/BranchRouteSync";
import { BranchSearchForm } from "@/features/tenant/components/BranchSearchForm";
import { Navbar } from "@/components/layout/navbar";
import { addOrderItem, deleteOrderItem, getCurrentOrder } from "@/services/order.service";
import { getHotelFull } from "@/services/hotel.service";

type HotelPageClientProps = {
  slug: string;
  checkIn: string;
  checkOut: string;
  totalRooms: number;
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

export default function HotelPageClient({ slug, checkIn, checkOut, totalRooms }: HotelPageClientProps) {
  const [isOrderOpen, setIsOrderOpen] = useState(false);

  const hotelQuery = useQuery({
    queryKey: ["hotel-full", slug, checkIn, checkOut, totalRooms],
    queryFn: () =>
      getHotelFull({
        slug,
        checkIn,
        checkOut,
        adult: totalRooms,
        child: 0,
      }),
    enabled: Boolean(slug && checkIn && checkOut),
  });

  const orderQuery = useQuery({
    queryKey: ["order-current", slug],
    queryFn: getCurrentOrder,
  });

  const addMutation = useMutation({
    mutationFn: addOrderItem,
    onSuccess: () => orderQuery.refetch(),
  });

  const deleteMutation = useMutation({
    mutationFn: deleteOrderItem,
    onSuccess: () => orderQuery.refetch(),
  });

  const hotel = hotelQuery.data?.hotel;
  const images = useMemo(() => hotelQuery.data?.images ?? [], [hotelQuery.data]);
  const roomTypes = useMemo(() => hotelQuery.data?.roomTypes ?? [], [hotelQuery.data]);
  const facilities = useMemo(() => hotelQuery.data?.facilities ?? [], [hotelQuery.data]);
  const nearby = useMemo(() => hotelQuery.data?.nearby ?? [], [hotelQuery.data]);
  const order = orderQuery.data;

  const imageList = useMemo(() => images.slice(0, 5), [images]);

  const sortedNearby = useMemo(() => [...nearby].sort((a, b) => a.distanceKm - b.distanceKm), [nearby]);

  const priceFrom = useMemo(() => {
    const prices = roomTypes.flatMap((rt) => rt.ratePlans.map((rp) => rp.price));
    return prices.length > 0 ? Math.min(...prices) : 0;
  }, [roomTypes]);

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
        {hotelQuery.isLoading && <p className="text-sm text-slate-500">Memuat detail hotel...</p>}
        {hotelQuery.isError && <p className="text-sm text-red-600">Gagal memuat detail hotel.</p>}

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
                  address: hotel.address,
                  aggregateRating: {
                    "@type": "AggregateRating",
                    ratingValue: hotel.rating,
                    reviewCount: hotel.reviewCount,
                  },
                  geo: {
                    "@type": "GeoCoordinates",
                    latitude: hotel.latitude,
                    longitude: hotel.longitude,
                  },
                }),
              }}
            />

            <div className="mb-6 flex flex-col gap-4 md:flex-row md:items-start md:justify-between">
              <div>
                <h1 className="text-2xl font-bold text-slate-900">{hotel.name}</h1>
                <div className="mt-2 flex items-center gap-2">
                  <div className="flex items-center gap-1">
                    {[1, 2, 3, 4, 5].map((star) => (
                      <Icons.Star
                        key={star}
                        size={14}
                        className={star <= Math.round(hotel.rating) ? "fill-amber-400 text-amber-400" : "fill-slate-200 text-slate-200"}
                      />
                    ))}
                  </div>
                  <span className="text-sm text-slate-600">{hotel.rating.toFixed(1)} ({hotel.reviewCount} reviews)</span>
                </div>
                <div className="mt-2 flex items-center gap-1.5 text-sm text-slate-500">
                  <Icons.MapPin size={14} className="text-[#c4a661]" />
                  {hotel.address}
                </div>
              </div>
              <div className="text-right">
                <p className="text-xs text-slate-400">Starts From</p>
                <p className="text-2xl font-bold text-slate-900">Rp {priceFrom.toLocaleString("id-ID")}</p>
                <p className="text-xs text-slate-400">/night</p>
              </div>
            </div>

            {imageList.length > 0 && (
              <div className="mb-8 grid h-[380px] grid-cols-4 grid-rows-2 gap-2">
                <div className="relative col-span-2 row-span-2 overflow-hidden rounded-l-xl">
                  <Image src={toImageUrl(imageList[0].url)} alt={hotel.name} fill className="object-cover" unoptimized />
                </div>
                {imageList.slice(1).map((image, index) => (
                  <div key={`${image.url}-${index}`} className="relative overflow-hidden">
                    <Image src={toImageUrl(image.url)} alt={`${hotel.name}-${index + 2}`} fill className="object-cover" unoptimized />
                  </div>
                ))}
              </div>
            )}

            <div className="mb-10">
              <BranchSearchForm branch={hotel.branchCode} />
            </div>

            <section className="mb-10">
              <h2 className="mb-3 text-xl font-bold text-slate-800">Overview</h2>
              <p className="max-w-4xl leading-relaxed text-slate-600">{hotel.description}</p>
            </section>

            <section className="mb-10">
              <h2 className="mb-4 text-xl font-bold text-slate-800">Facilities</h2>
              <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 md:grid-cols-4">
                {facilities.map((facility) => {
                  const Icon = Icons[facility.icon as keyof typeof Icons] as LucideIcon | undefined;
                  return (
                    <div key={facility.name} className="group flex items-center gap-3 rounded-2xl border border-[#e2e8f0] bg-white p-4 transition-all duration-200 hover:-translate-y-[2px] hover:shadow-md">
                      <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-[#f8fafc] transition group-hover:bg-[#1a1f3c]/5">
                        {Icon ? <Icon size={18} className="text-[#1a1f3c]" /> : <span className="text-xs text-[#94a3b8]">?</span>}
                      </div>
                      <span className="text-sm font-medium text-[#0f172a]">{facility.name}</span>
                    </div>
                  );
                })}
              </div>
            </section>

            <section className="mb-10 grid gap-6 md:grid-cols-2">
              <div>
                <h2 className="mb-4 text-xl font-bold text-slate-800">Location</h2>
                <iframe
                  src={`https://maps.google.com/maps?q=${encodeURIComponent(`${hotel.name} ${hotel.latitude},${hotel.longitude}`)}&z=15&output=embed`}
                  className="h-64 w-full rounded-xl border-0"
                  title="Hotel Location"
                />
              </div>
              <div>
                <h2 className="mb-4 text-xl font-bold text-slate-800">Nearby Places</h2>
                <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 md:grid-cols-3 lg:grid-cols-2">
                  {sortedNearby.map((item) => (
                    <div key={item.name} className="group rounded-2xl border border-[#e2e8f0] bg-white p-4 transition-all duration-200 hover:-translate-y-[2px] hover:shadow-md">
                      <div className="text-sm font-medium text-[#0f172a] leading-snug">{item.name}</div>
                      <div className="mt-2 text-sm font-semibold text-[#c4a661]">{formatDistance(item.distanceKm)}</div>
                    </div>
                  ))}
                </div>
              </div>
            </section>

            <section>
              <h2 className="mb-4 text-xl font-bold text-slate-800">Select Your Room</h2>
              <div className="space-y-5">
                {roomTypes.map((roomType) => (
                  <div key={roomType.id} className="rounded-xl border border-slate-200 bg-white p-5">
                    <div className="mb-4 flex flex-col gap-4 md:flex-row">
                      <div className="relative h-40 w-full overflow-hidden rounded-lg md:w-56">
                        <Image src={toImageUrl(roomType.image)} alt={roomType.name} fill className="object-cover" unoptimized />
                      </div>
                      <div className="flex-1">
                        <h3 className="text-lg font-semibold text-slate-900">{roomType.name}</h3>
                        <p className="mt-1 text-sm text-slate-500">{roomType.description}</p>
                        <p className="mt-2 text-xs text-slate-500">{roomType.capacity} guests | {roomType.bedType} | {roomType.size} m2</p>
                        <div className="mt-2 flex flex-wrap gap-2">
                          {roomType.facilities.map((facility) => (
                            <span key={facility} className="rounded bg-slate-100 px-2 py-1 text-xs text-slate-600">{facility}</span>
                          ))}
                        </div>
                      </div>
                    </div>

                    <div className="space-y-3">
                      {roomType.ratePlans.map((ratePlan) => (
                        <div key={ratePlan.id} className="flex flex-col items-start justify-between gap-3 rounded-lg border border-slate-200 p-4 md:flex-row md:items-center">
                          <div>
                            <p className="font-semibold text-slate-900">{ratePlan.name}</p>
                            <p className="text-xs text-slate-500">{ratePlan.benefits}</p>
                            <p className="text-xs text-slate-400">{ratePlan.terms}</p>
                          </div>
                          <div className="text-right">
                            <p className="text-lg font-bold text-[#c4a661]">Rp {ratePlan.price.toLocaleString("id-ID")}</p>
                            <p className="text-xs text-slate-400">per night</p>
                            <button
                              onClick={() =>
                                addMutation.mutate({
                                  roomTypeId: roomType.id,
                                  ratePlanId: ratePlan.id,
                                  checkIn,
                                  checkOut,
                                  totalRooms,
                                })
                              }
                              className="mt-2 rounded-lg bg-[#1a1f3c] px-4 py-2 text-sm font-semibold text-white"
                            >
                              Select
                            </button>
                          </div>
                        </div>
                      ))}
                    </div>
                  </div>
                ))}
              </div>
            </section>
          </>
        )}
      </div>

      {order && order.items.length > 0 && (
        <>
          <button onClick={() => setIsOrderOpen(true)} className="fixed bottom-6 right-6 z-40 rounded-full bg-[#1a1f3c] px-5 py-3 text-sm font-semibold text-white shadow-lg">
            View Order ({order.items.length})
          </button>

          {isOrderOpen && (
            <div className="fixed inset-0 z-50 bg-black/35">
              <div className="absolute right-0 top-0 h-full w-full max-w-md bg-white p-5 shadow-2xl">
                <div className="mb-4 flex items-center justify-between">
                  <h3 className="text-lg font-semibold">Order Summary</h3>
                  <button onClick={() => setIsOrderOpen(false)} className="text-sm text-slate-500">Close</button>
                </div>

                <div className="space-y-3 overflow-auto">
                  {order.items.map((item) => (
                    <div key={item.id} className="rounded-lg border border-slate-200 p-3">
                      <p className="font-semibold text-slate-900">{item.roomTypeName}</p>
                      <p className="text-xs text-slate-500">{item.ratePlanName}</p>
                      <p className="text-xs text-slate-500">{item.checkIn} - {item.checkOut}</p>
                      <p className="text-xs text-slate-500">{item.totalRooms} room(s)</p>
                      <p className="mt-1 font-semibold text-[#c4a661]">Rp {item.totalPrice.toLocaleString("id-ID")}</p>
                      <button onClick={() => deleteMutation.mutate(item.id)} className="mt-2 text-xs text-red-600 underline">Remove</button>
                    </div>
                  ))}
                </div>

                <div className="mt-6 border-t border-slate-200 pt-4">
                  <div className="mb-4 flex items-center justify-between">
                    <span className="text-sm text-slate-600">Grand Total</span>
                    <span className="text-xl font-bold text-slate-900">Rp {order.grandTotal.toLocaleString("id-ID")}</span>
                  </div>
                  <Link href={`/booking?branch=${hotel?.branchCode ?? ""}`} className="block rounded-lg bg-[#1a1f3c] px-4 py-3 text-center text-sm font-semibold text-white">
                    Continue Booking
                  </Link>
                </div>
              </div>
            </div>
          )}
        </>
      )}
    </main>
  );
}
