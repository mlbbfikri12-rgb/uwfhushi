import { BranchSearchForm } from "@/features/tenant/components/BranchSearchForm";
import { Navbar } from "@/components/layout/navbar";
import Image from "next/image";

export default function Home() {
  return (
    <main className="min-h-screen bg-white">
      <Navbar />

      {/* HERO */}
      <section className="relative min-h-[600px] pb-24 w-full">
        {/* Background */}
        <Image
          src="https://images.unsplash.com/photo-1541976844346-f18aeac57b06?q=80&w=2070"
          alt="MyLynn Hotel"
          fill
          className="object-cover"
          priority
        />

        {/* Overlay */}
        <div className="absolute inset-0 z-10 bg-gradient-to-b from-[#1a1f3c]/80 via-[#1a1f3c]/60 to-[#1a1f3c]/90" />

        {/* Content */}
        <div className="absolute inset-0 z-20 flex flex-col items-center justify-center text-center px-5 pt-20">
          <h1 className="text-4xl md:text-6xl font-bold leading-tight mb-5">
            <span className="text-white">Experience the</span>
            <br />
            <span className="text-[#c4a661]">Twilight Luxury</span>
          </h1>

          <p className="text-lg text-slate-200 max-w-xl">
            Nikmati pengalaman menginap eksklusif bersama MyLynn Hotel &
            Resorts.
          </p>

          <div className="mt-8">
            <button className="flex items-center gap-2 rounded-full border border-[#c4a661] px-5 py-2.5 text-sm font-semibold text-[#c4a661] hover:bg-[#c4a661] hover:text-[#1a1f3c] transition">
              Lihat Penawaran
              <span aria-hidden="true">→</span>
            </button>
          </div>
        </div>

        {/* SEARCH BAR FLOAT */}
        <div className="absolute -bottom-12 left-1/2 -translate-x-1/2 z-30 w-full max-w-5xl px-5 pointer-events-auto">
          <div className="bg-white rounded-2xl shadow-2xl p-5 border border-slate-100">
            <BranchSearchForm />
          </div>
        </div>

        {/* SPACING */}
      </section>

      <div className="h-40 md:h-48" />
      {/* SECTION: PROMO (WAJIB ADA) */}
      <section className="py-16 px-5 max-w-6xl mx-auto">
        <h2 className="text-2xl font-bold text-slate-800">Special Offers</h2>
        <p className="text-slate-500 mt-1">
          Promo terbaik untuk pengalaman menginap Anda
        </p>

        <div className="grid md:grid-cols-3 gap-6 mt-8">
          {[1, 2, 3].map((item) => (
            <div
              key={item}
              className="rounded-xl overflow-hidden shadow-md hover:shadow-xl transition"
            >
              <div className="relative h-48 w-full">
                <Image
                  src="https://images.unsplash.com/photo-1566073771259-6a8506099945"
                  alt="Promo Staycation"
                  fill
                  className="object-cover"
                />
              </div>
              <div className="p-4">
                <h3 className="font-semibold text-lg">Promo Staycation</h3>
                <p className="text-sm text-slate-500 mt-1">
                  Diskon hingga 40% untuk akhir pekan
                </p>
              </div>
            </div>
          ))}
        </div>
      </section>
    </main>
  );
}
