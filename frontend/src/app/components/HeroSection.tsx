"use client";

import { useState } from "react";

import BannerCarousel from "./BannerCarausel";

import type { PublicBanner } from "@/types/home";

type Props = {
  banners: PublicBanner[];
};

export default function HeroSection({ banners }: Props) {
  const [current, setCurrent] = useState(0);

  const activeBanner = banners[current] ?? banners[0];

  return (
    <section className="relative h-[520px] w-full overflow-hidden">
      <BannerCarousel
        banners={banners}
        current={current}
        setCurrent={setCurrent}
      />

      <div className="absolute inset-0 bg-black/35" />

      <div className="absolute inset-x-0 bottom-20 mx-auto max-w-7xl px-6 text-white">
        <div
          key={activeBanner?.id}
          className="max-w-2xl transition-all duration-500"
        >
          <h1 className="text-4xl font-bold leading-tight tracking-tight drop-shadow-lg md:text-5xl">
            {activeBanner?.title ?? "Hotel Booking Platform"}
          </h1>

          <p className="mt-4 text-sm leading-relaxed text-slate-200 drop-shadow-md md:text-base">
            {activeBanner?.subtitle ??
              "Discover luxury stays, curated destinations, and unforgettable experiences across Indonesia."}
          </p>
        </div>
      </div>
    </section>
  );
}
