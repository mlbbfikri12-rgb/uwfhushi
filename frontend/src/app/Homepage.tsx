import { useMemo } from "react";

import { Navbar } from "@/components/layout/navbar";

import HeroSection from "./components/HeroSection";
import PopularHotels from "./components/PopularHotels";
import Destinations from "./components/Destinations";
import Blogs from "./components/Blogs";
import BranchSearchForm from "@/features/tenant/components/BranchSearchForm";
import { Suspense } from "react";

import type { PublicHomeResponse } from "@/types/home";
import Footer from "./components/Footer";

type Props = {
  initialData: PublicHomeResponse;
};

export default function Homepage({ initialData }: Props) {
  const banners = useMemo(() => initialData?.heroBanners ?? [], [initialData]);

  const popularHotels = useMemo(
    () => initialData?.popularHotels ?? [],
    [initialData],
  );

  const destinations = useMemo(
    () => initialData?.destinations ?? [],
    [initialData],
  );

  const blogs = useMemo(() => initialData?.blogs ?? [], [initialData]);

  return (
    <>
      <Navbar />
      <main className="min-h-screen bg-white mb-2">
        <HeroSection banners={banners} />

        <Suspense
          fallback={
            <div className="h-[96px] w-full rounded-2xl border border-slate-200 bg-white shadow-lg animate-pulse" />
          }
        >
          <div className="mx-auto max-w-7xl px-6 py-5">
            <BranchSearchForm />
          </div>
        </Suspense>

        <PopularHotels hotels={popularHotels} />

        <Destinations destinations={destinations} />

        <Blogs blogs={blogs} />
      </main>
      <Footer />
    </>
  );
}
