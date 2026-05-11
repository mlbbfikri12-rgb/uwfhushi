"use client";

import Image from "next/image";

import { ChevronLeft, ChevronRight } from "lucide-react";

import { useEffect } from "react";

import type { PublicBanner } from "@/types/home";
import { getImageUrl } from "@/utils/ImageCombineUrl";

type Props = {
  banners: PublicBanner[];

  current: number;

  setCurrent: React.Dispatch<React.SetStateAction<number>>;
};

export default function BannerCarousel({
  banners,
  current,
  setCurrent,
}: Props) {
  const activeBanner =
    banners.length > 0 ? banners[current % banners.length] : null;

  // =========================
  // AUTOPLAY
  // =========================

  useEffect(() => {
    if (banners.length <= 1) return;

    const interval = setInterval(() => {
      setCurrent((prev) => (prev + 1) % banners.length);
    }, 5000);

    return () => clearInterval(interval);
  }, [banners.length, setCurrent]);

  // =========================
  // NAVIGATION
  // =========================

  const nextSlide = () => {
    setCurrent((prev) => (prev + 1) % banners.length);
  };

  const prevSlide = () => {
    setCurrent((prev) => (prev === 0 ? banners.length - 1 : prev - 1));
  };

  return (
    <>
      {/* IMAGE */}

      {activeBanner ? (
        <Image
          key={activeBanner.id}
          src={getImageUrl(activeBanner.imageUrl)}
          alt={activeBanner.title}
          fill
          priority
          sizes="100vw"
          className="object-cover transition-opacity duration-700"
        />
      ) : (
        <div className="h-full w-full bg-slate-200" />
      )}

      {/* NAVIGATION */}

      {banners.length > 1 && (
        <>
          {/* PREV */}

          <button
            onClick={prevSlide}
            className="absolute left-5 top-1/2 z-30 flex h-11 w-11 -translate-y-1/2 items-center justify-center rounded-full bg-black/30 text-white backdrop-blur transition-all hover:bg-black/50"
          >
            <ChevronLeft size={22} />
          </button>

          {/* NEXT */}

          <button
            onClick={nextSlide}
            className="absolute right-5 top-1/2 z-30 flex h-11 w-11 -translate-y-1/2 items-center justify-center rounded-full bg-black/30 text-white backdrop-blur transition-all hover:bg-black/50"
          >
            <ChevronRight size={22} />
          </button>

          {/* INDICATORS */}

          <div className="absolute inset-0">
            {banners.map((banner, idx) => (
              <div
                key={banner.id}
                className={`absolute inset-0 transition-opacity duration-1000 ${
                  idx === current ? "opacity-100" : "opacity-0"
                }`}
              >
                <Image
                  src={getImageUrl(banner.imageUrl)}
                  alt={banner.title}
                  fill
                  priority={idx === 0}
                  sizes="100vw"
                  className="object-cover"
                />
              </div>
            ))}
          </div>
        </>
      )}
    </>
  );
}
