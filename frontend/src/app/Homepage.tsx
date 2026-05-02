"use client";

import Image from "next/image";
import Link from "next/link";
import { useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { BranchSearchForm } from "@/features/tenant/components/BranchSearchForm";
import { Navbar } from "@/components/layout/navbar";
import { getPublicHome } from "@/services/branch.service";

function toImageUrl(url: string) {
  return /^https?:\/\//i.test(url)
    ? url
    : "https://images.unsplash.com/photo-1566073771259-6a8506099945?w=1200&q=80";
}

export default function Home() {
  const [current, setCurrent] = useState(0);
  const homeQuery = useQuery({
    queryKey: ["public-home"],
    queryFn: getPublicHome,
  });

  const banners = useMemo(() => homeQuery.data?.heroBanners ?? [], [homeQuery.data]);
  const popularHotels = useMemo(() => homeQuery.data?.popularHotels ?? [], [homeQuery.data]);
  const destinations = useMemo(() => homeQuery.data?.destinations ?? [], [homeQuery.data]);
  const blogs = useMemo(() => homeQuery.data?.blogs ?? [], [homeQuery.data]);

  const activeBanner = banners.length > 0 ? banners[current % banners.length] : null;

  return (
    <main className="min-h-screen bg-white">
      <Navbar />

      <section className="relative h-[520px] w-full overflow-hidden">
        {activeBanner ? (
          <Image
            src={toImageUrl(activeBanner.imageUrl)}
            alt={activeBanner.title}
            fill
            className="object-cover"
            priority
            unoptimized
          />
        ) : (
          <div className="h-full w-full bg-slate-200" />
        )}
        <div className="absolute inset-0 bg-black/35" />
        <div className="absolute inset-x-0 bottom-20 mx-auto max-w-7xl px-6 text-white">
          <h1 className="text-4xl font-bold">{activeBanner?.title ?? "Hotel Booking Platform"}</h1>
        </div>
        {banners.length > 1 && (
          <div className="absolute bottom-5 left-1/2 z-20 flex -translate-x-1/2 gap-2">
            {banners.map((banner, idx) => (
              <button
                key={banner.id}
                onClick={() => setCurrent(idx)}
                className={`h-2 rounded-full ${idx === current ? "w-8 bg-[#c4a661]" : "w-2 bg-white/70"}`}
              />
            ))}
          </div>
        )}
      </section>

      <div className="mx-auto max-w-7xl px-6 py-5">
        <BranchSearchForm />
      </div>

      <section className="mx-auto max-w-7xl px-6 py-10">
        <h2 className="mb-4 text-2xl font-bold text-slate-800">Popular Hotels</h2>
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          {popularHotels.map((hotel) => (
            <Link key={hotel.hotelId} href={`/hotel/${hotel.slug}`} className="rounded-xl border bg-white p-3 hover:shadow-md">
              <div className="relative mb-3 h-40 w-full overflow-hidden rounded-lg">
                <Image src={toImageUrl(hotel.image)} alt={hotel.name} fill className="object-cover" unoptimized />
              </div>
              <p className="font-semibold text-slate-900">{hotel.name}</p>
              <p className="text-sm text-slate-500">{hotel.city}</p>
            </Link>
          ))}
        </div>
      </section>

      <section className="bg-slate-50 py-10">
        <div className="mx-auto max-w-7xl px-6">
          <h2 className="mb-4 text-2xl font-bold text-slate-800">Destinations</h2>
          <div className="grid gap-3 md:grid-cols-3">
            {destinations.map((destination) => (
              <Link key={destination.city} href={`/search?q=${encodeURIComponent(destination.city)}`} className="rounded-xl border bg-white p-4">
                <p className="font-semibold text-slate-900">{destination.city}</p>
                <p className="text-sm text-slate-500">From Rp {destination.minPrice.toLocaleString("id-ID")}</p>
              </Link>
            ))}
          </div>
        </div>
      </section>

      <section className="mx-auto max-w-7xl px-6 py-10">
        <h2 className="mb-4 text-2xl font-bold text-slate-800">From Our Blog</h2>
        <div className="grid gap-4 md:grid-cols-3">
          {blogs.map((blog) => (
            <article key={blog.id} className="rounded-xl border bg-white p-4">
              <div className="relative mb-3 h-40 w-full overflow-hidden rounded-lg">
                <Image src={toImageUrl(blog.imageUrl)} alt={blog.title} fill className="object-cover" unoptimized />
              </div>
              <p className="font-semibold text-slate-900">{blog.title}</p>
            </article>
          ))}
        </div>
      </section>
    </main>
  );
}
