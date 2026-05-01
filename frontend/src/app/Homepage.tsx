"use client";

import Image from "next/image";
import Link from "next/link";
import { useEffect, useRef, useState } from "react";
import { BranchSearchForm } from "@/features/tenant/components/BranchSearchForm";
import { Navbar } from "@/components/layout/navbar";

// ─── DATA ────────────────────────────────────────────────────────────────────

const HERO_SLIDES = [
  {
    image:
      "https://images.unsplash.com/photo-1537996194471-e657df975ab4?w=1600&q=80",
    title: "Explore Bali",
    subtitle: "The Captivating Island of Gods",
    price: "Rp 350.000",
  },
  {
    image:
      "https://images.unsplash.com/photo-1537996194471-e657df975ab4?w=1600&q=80",
    title: "Discover Surabaya",
    subtitle: "The City of Heroes",
    price: "Rp 280.000",
  },
  {
    image:
      "https://images.unsplash.com/photo-1537996194471-e657df975ab4?w=1600&q=80",
    title: "Visit Yogyakarta",
    subtitle: "The Heart of Javanese Culture",
    price: "Rp 220.000",
  },
];

const SPECIAL_OFFERS = [
  {
    title: "Weekend Offer",
    desc: "Book direct to get our best offer for your weekend stay. Get FREE Breakfast, Wi-Fi and other exclusive benefits.",
    image:
      "https://images.unsplash.com/photo-1520250497591-112f2f40a3f4?w=600&q=70",
  },
  {
    title: "Best Flexible Rate",
    desc: "Planning your trip ahead? Book our Best Flexible Rate! Get FREE Wi-Fi, FREE cancellation and more.",
    image:
      "https://images.unsplash.com/photo-1584132967334-10e028bd69f7?w=600&q=70",
  },
  {
    title: "Staycation Offer",
    desc: "Book direct and prepare your next Staycation at a great price with our exclusive member rates.",
    image:
      "https://images.unsplash.com/photo-1566073771259-6a8506099945?w=600&q=70",
  },
];

const POPULAR_HOTELS = [
  {
    name: "Amaris Hotel Darmo – Surabaya",
    location: "Surabaya, Jawa Timur",
    price: "Rp 300.000",
    rating: "4.3",
    image:
      "https://images.unsplash.com/photo-1566073771259-6a8506099945?w=900&q=70",
    branch: "SBY",
  },
  {
    name: "Santika Premiere Semarang",
    location: "Semarang, Jawa Tengah",
    price: "Rp 520.000",
    rating: "4.6",
    image:
      "https://images.unsplash.com/photo-1564501049412-61c2a3083791?w=900&q=70",
    branch: "SMG",
  },
  {
    name: "The Kayana Seminyak",
    location: "Seminyak, Bali",
    price: "Rp 1.100.000",
    rating: "4.9",
    image:
      "https://images.unsplash.com/photo-1520250497591-112f2f40a3f4?w=900&q=70",
    branch: "BALI",
  },
];

const DESTINATIONS = [
  {
    name: "Bali",
    from: "Rp 262.500",
    image:
      "https://images.unsplash.com/photo-1537996194471-e657df975ab4?w=600&q=70",
  },
  {
    name: "Jawa Tengah",
    from: "Rp 260.000",
    image:
      "https://images.unsplash.com/photo-1588668214407-6ea9a6d8c272?w=600&q=70",
  },
  {
    name: "Jawa Timur",
    from: "Rp 280.000",
    image:
      "https://images.unsplash.com/photo-1588668214407-6ea9a6d8c272?w=600&q=70",
  },
  {
    name: "Jakarta",
    from: "Rp 450.000",
    image:
      "https://images.unsplash.com/photo-1555899434-94d1368aa7af?w=600&q=70",
  },
  {
    name: "Lombok",
    from: "Rp 300.000",
    image:
      "https://images.unsplash.com/photo-1518548419970-58e3b4079ab2?w=600&q=70",
  },
  {
    name: "Yogyakarta",
    from: "Rp 220.000",
    image:
      "https://images.unsplash.com/photo-1518548419970-58e3b4079ab2?w=600&q=70",
  },
];

const BLOG_POSTS = [
  {
    title: "10 Tips Menginap Hemat di Bali Tanpa Kurangi Kenyamanan",
    category: "Travel Tips",
    date: "28 Apr 2026",
    image:
      "https://images.unsplash.com/photo-1537996194471-e657df975ab4?w=600&q=70",
  },
  {
    title: "Menjelajahi Kuliner Khas Surabaya yang Wajib Dicoba",
    category: "Destination",
    date: "22 Apr 2026",
    image:
      "https://images.unsplash.com/photo-1537996194471-e657df975ab4?w=600&q=70",
  },
  {
    title: "Hotel Ramah Keluarga Terbaik di Yogyakarta 2026",
    category: "Family Travel",
    date: "15 Apr 2026",
    image:
      "https://images.unsplash.com/photo-1537996194471-e657df975ab4?w=600&q=70",
  },
];

// ─── HERO SLIDER ─────────────────────────────────────────────────────────────

function HeroSlider() {
  const [current, setCurrent] = useState(0);
  const timerRef = useRef<ReturnType<typeof setInterval> | null>(null);

  const startTimer = () => {
    timerRef.current = setInterval(() => {
      setCurrent((prev) => (prev + 1) % HERO_SLIDES.length);
    }, 5000);
  };

  useEffect(() => {
    startTimer();
    return () => {
      if (timerRef.current) clearInterval(timerRef.current);
    };
  }, []);

  const goTo = (index: number) => {
    setCurrent(index);
    if (timerRef.current) clearInterval(timerRef.current);
    startTimer();
  };

  const prev = () =>
    goTo((current - 1 + HERO_SLIDES.length) % HERO_SLIDES.length);
  const next = () => goTo((current + 1) % HERO_SLIDES.length);

  const slide = HERO_SLIDES[current];

  return (
    <div className="relative w-full h-[540px] overflow-hidden">
      {/* Background image */}
      {HERO_SLIDES.map((s, i) => (
        <div
          key={i}
          className={`absolute inset-0 transition-opacity duration-700 ${
            i === current ? "opacity-100" : "opacity-0"
          }`}
        >
          <Image
            src={s.image}
            alt={s.title}
            fill
            className="object-cover"
            priority={i === 0}
          />
        </div>
      ))}

      {/* Dark overlay — bottom half lebih gelap biar teks terbaca */}
      <div className="absolute inset-0 bg-gradient-to-t from-[#0d1020]/80 via-[#0d1020]/30 to-transparent z-10" />

      {/* Content */}
      <div className="absolute bottom-24 left-0 right-0 z-20 max-w-7xl mx-auto px-6 flex items-end justify-between">
        {/* Kiri: judul */}
        <div>
          <h1 className="text-5xl md:text-6xl font-bold text-white leading-tight">
            {slide.title}
          </h1>
          <p className="text-xl text-slate-200 mt-2 font-medium">
            {slide.subtitle}
          </p>
        </div>

        {/* Kanan: harga */}
        <div className="hidden md:block text-right">
          <p className="text-slate-300 text-sm">Starts from</p>
          <p className="text-white text-4xl font-bold">
            {slide.price}
            <span className="text-lg font-normal text-slate-300">/night</span>
          </p>
          <p className="text-slate-300 text-sm mt-1">
            Enjoy special rates for MyLynn Members.{" "}
            <Link
              href="/register"
              className="text-[#c4a661] font-semibold underline underline-offset-2"
            >
              Register for free today!
            </Link>
          </p>
        </div>
      </div>

      {/* Prev / Next arrows */}
      <button
        onClick={prev}
        className="absolute left-4 top-1/2 -translate-y-1/2 z-20 w-10 h-10 rounded-full bg-black/30 hover:bg-black/50 flex items-center justify-center text-white transition"
      >
        <svg
          width="20"
          height="20"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          strokeWidth="2.5"
        >
          <polyline points="15 18 9 12 15 6" />
        </svg>
      </button>
      <button
        onClick={next}
        className="absolute right-4 top-1/2 -translate-y-1/2 z-20 w-10 h-10 rounded-full bg-black/30 hover:bg-black/50 flex items-center justify-center text-white transition"
      >
        <svg
          width="20"
          height="20"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          strokeWidth="2.5"
        >
          <polyline points="9 18 15 12 9 6" />
        </svg>
      </button>

      {/* Dots */}
      <div className="absolute bottom-6 left-1/2 -translate-x-1/2 z-20 flex gap-2">
        {HERO_SLIDES.map((_, i) => (
          <button
            key={i}
            onClick={() => goTo(i)}
            className={`transition-all rounded-full ${
              i === current
                ? "w-7 h-2.5 bg-[#c4a661]"
                : "w-2.5 h-2.5 bg-white/40 hover:bg-white/60"
            }`}
          />
        ))}
      </div>
    </div>
  );
}

// ─── SECTION HEADER ──────────────────────────────────────────────────────────

function SectionHeader({ title, sub }: { title: string; sub: string }) {
  return (
    <div className="mb-8">
      <h2 className="text-2xl font-bold text-slate-800">{title}</h2>
      <p className="text-slate-500 mt-1 text-sm">{sub}</p>
    </div>
  );
}

// ─── PAGE ─────────────────────────────────────────────────────────────────────

export default function Home() {
  return (
    <main className="min-h-screen bg-white">
      <Navbar />

      {/* ── HERO ── */}
      <HeroSlider />

      {/* ── SEARCH BAR ── */}
      <div className="bg-white py-5">
        <div className="max-w-7xl mx-auto px-6 py-4">
          <BranchSearchForm />
        </div>
      </div>

      {/* ── SPECIAL OFFERS ── */}
      <section id="special-offers" className="py-16 px-6 max-w-7xl mx-auto">
        <SectionHeader
          title="Special Offers"
          sub="Promotions, discounts, and special offers for you"
        />
        <div className="grid md:grid-cols-3 gap-6">
          {SPECIAL_OFFERS.map((offer) => (
            <div
              key={offer.title}
              className="rounded-xl overflow-hidden border border-slate-100 hover:shadow-lg transition-shadow"
            >
              <div className="relative h-52 w-full">
                <Image
                  src={offer.image}
                  alt={offer.title}
                  fill
                  className="object-cover"
                />
              </div>
              <div className="p-5">
                <h3 className="font-bold text-base text-slate-800">
                  {offer.title}
                </h3>
                <p className="text-sm text-slate-500 mt-2 leading-relaxed line-clamp-3">
                  {offer.desc}
                </p>
                <Link
                  href="#"
                  className="mt-4 inline-flex items-center gap-1.5 text-sm font-semibold text-[#1a6b5a] hover:text-[#c4a661] transition-colors"
                >
                  More Details
                  <svg
                    width="16"
                    height="16"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="2.5"
                  >
                    <line x1="5" y1="12" x2="19" y2="12" />
                    <polyline points="12 5 19 12 12 19" />
                  </svg>
                </Link>
              </div>
            </div>
          ))}
        </div>
      </section>

      {/* ── POPULAR HOTELS ── */}
      <section className="py-16 bg-slate-50">
        <div className="px-6 max-w-7xl mx-auto">
          <SectionHeader
            title="Our Popular Hotels"
            sub="Recommended based on your activity"
          />
          <div className="flex flex-col gap-6">
            {POPULAR_HOTELS.map((hotel) => (
              <div
                key={hotel.name}
                className="bg-white rounded-xl overflow-hidden border border-slate-100 hover:shadow-lg transition-shadow flex flex-col md:flex-row"
              >
                <div className="relative w-full md:w-[420px] h-56 md:h-auto shrink-0">
                  <Image
                    src={hotel.image}
                    alt={hotel.name}
                    fill
                    className="object-cover"
                  />
                </div>
                <div className="p-6 flex flex-col justify-between flex-1">
                  <div>
                    <h3 className="font-bold text-lg text-slate-800">
                      {hotel.name}
                    </h3>
                    <p className="text-sm text-slate-500 mt-1">
                      {hotel.location}
                    </p>
                    <span className="inline-block mt-3 text-xs bg-amber-50 text-amber-700 border border-amber-200 px-2.5 py-0.5 rounded-full font-semibold">
                      ★ {hotel.rating}
                    </span>
                  </div>
                  <div className="flex items-center justify-between mt-6">
                    <div>
                      <p className="text-xs text-slate-400">Starting from</p>
                      <p className="text-xl font-bold text-slate-800">
                        {hotel.price}
                        <span className="text-sm font-normal text-slate-400">
                          /room/night
                        </span>
                      </p>
                    </div>
                    <Link
                      href={`/hotel/${hotel.branch}`}
                      className="bg-[#1a6b5a] hover:bg-[#155a4b] text-white text-sm font-semibold px-5 py-2.5 rounded-md transition-colors"
                    >
                      View Hotel
                    </Link>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* ── YOUR DESTINATION ── */}
      <section id="destination" className="py-16 px-6 max-w-7xl mx-auto">
        <SectionHeader
          title="Your Destination"
          sub="Discover our Hotels across Indonesia"
        />
        <div className="grid grid-cols-2 md:grid-cols-3 gap-4">
          {DESTINATIONS.map((dest) => (
            <Link
              key={dest.name}
              href={`/search?q=${dest.name}`}
              className="relative rounded-xl overflow-hidden h-52 group block"
            >
              <Image
                src={dest.image}
                alt={dest.name}
                fill
                className="object-cover group-hover:scale-105 transition-transform duration-500"
              />
              <div className="absolute inset-0 bg-gradient-to-t from-[#0d1020]/70 via-transparent to-transparent" />
              <div className="absolute bottom-4 left-4">
                <p className="text-white font-bold text-lg leading-tight">
                  {dest.name}
                </p>
                <p className="text-[#c4a661] text-sm font-medium">
                  Starts from {dest.from}
                </p>
              </div>
            </Link>
          ))}
        </div>
      </section>

      {/* ── BLOG ── */}
      <section id="blog" className="py-16 bg-slate-50">
        <div className="px-6 max-w-7xl mx-auto">
          <SectionHeader
            title="From Our Blog"
            sub="Explore travel inspiration, hotel tips, and destination stories from across Indonesia."
          />
          <div className="grid md:grid-cols-3 gap-6">
            {BLOG_POSTS.map((post) => (
              <Link
                key={post.title}
                href="#"
                className="bg-white rounded-xl overflow-hidden border border-slate-100 hover:shadow-lg transition-shadow group block"
              >
                <div className="relative h-48 w-full overflow-hidden">
                  <Image
                    src={post.image}
                    alt={post.title}
                    fill
                    className="object-cover group-hover:scale-105 transition-transform duration-500"
                  />
                </div>
                <div className="p-5">
                  <div className="flex items-center gap-2 mb-3">
                    <span className="text-xs font-semibold bg-slate-100 text-slate-600 px-2.5 py-1 rounded-full">
                      {post.category}
                    </span>
                    <span className="text-xs text-slate-400">{post.date}</span>
                  </div>
                  <h3 className="font-semibold text-slate-800 leading-snug line-clamp-2">
                    {post.title}
                  </h3>
                </div>
              </Link>
            ))}
          </div>
        </div>
      </section>

      {/* ── FOOTER ── */}
      <footer className="bg-[#1a1f3c] text-white py-10">
        <div className="max-w-7xl mx-auto px-6 flex flex-col md:flex-row items-center justify-between gap-4">
          <div>
            <p className="text-xl font-bold">
              my<span className="text-[#c4a661]">Lynn</span>
            </p>
            <p className="text-slate-400 text-sm mt-1">Hotel & Resorts</p>
          </div>
          <p className="text-slate-500 text-xs">
            © 2026 MyLynn Hotel. All rights reserved.
          </p>
        </div>
      </footer>
    </main>
  );
}
