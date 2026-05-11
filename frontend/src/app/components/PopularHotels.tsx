import Image from "next/image";
import Link from "next/link";
import { MapPin, Star } from "lucide-react";

import { PublicPopularHotel } from "@/types/home";
import { getImageUrl } from "@/utils/ImageCombineUrl";

type Props = {
  hotels: PublicPopularHotel[];
};

export default function PopularHotels({ hotels }: Props) {
  if (!hotels || hotels.length === 0) return null;

  return (
    <section className="mx-auto max-w-7xl px-6 py-16">
      {/* SECTION HEADER */}
      <div className="mb-10 flex flex-col items-start justify-between gap-4 sm:flex-row sm:items-end">
        <div>
          <h2 className="text-3xl font-bold tracking-tight text-slate-900 md:text-4xl">
            Popular Hotels
          </h2>
          <p className="mt-2 text-slate-500">
            Pilihan akomodasi favorit dengan fasilitas terbaik untuk Anda.
          </p>
        </div>
        <Link
          href="/hotels"
          className="group flex items-center gap-1 text-sm font-semibold text-[#c4a661] transition-colors hover:text-[#b8954f]"
        >
          Lihat semua
          <span className="transition-transform group-hover:translate-x-1">
            &rarr;
          </span>
        </Link>
      </div>

      {/* CARDS GRID */}
      <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
        {hotels.map((hotel) => (
          <Link
            key={hotel.hotelId}
            href={`/hotel/${hotel.slug}`}
            className="group flex flex-col overflow-hidden rounded-2xl border border-slate-100 bg-white shadow-sm transition-all duration-300 hover:-translate-y-1.5 hover:shadow-xl hover:shadow-[#c4a661]/10"
          >
            {/* IMAGE */}
            <div className="relative h-60 w-full overflow-hidden bg-slate-100">
              <Image
                src={getImageUrl(hotel.image)}
                alt={hotel.name}
                fill
                sizes="(max-width: 768px) 100vw, (max-width: 1200px) 50vw, 33vw"
                className="object-cover transition-transform duration-700 group-hover:scale-110 will-change-transform"
              />

              {/* GRADIENT OVERLAY (Biar gambar yang terlalu terang bawahnya tetap punya depth) */}
              <div className="absolute inset-0 bg-gradient-to-t from-black/20 to-transparent opacity-0 transition-opacity duration-300 group-hover:opacity-100" />

              {/* BRAND BADGE */}
              <div className="absolute left-4 top-4 rounded-full bg-white/95 px-3.5 py-1.5 text-[11px] font-bold uppercase tracking-wider text-slate-800 shadow-sm backdrop-blur-sm">
                {hotel.brand}
              </div>
            </div>

            {/* CONTENT */}
            <div className="flex flex-1 flex-col p-6">
              <div className="flex-1 space-y-2.5">
                {/* TITLE & STARS */}
                <div className="flex items-start justify-between gap-2">
                  <h3 className="line-clamp-2 text-lg font-bold leading-snug text-slate-900 group-hover:text-[#c4a661] transition-colors">
                    {hotel.name}
                  </h3>
                  <div className="flex shrink-0 items-center gap-0.5 rounded-md bg-amber-50 px-1.5 py-0.5">
                    <Star className="fill-[#c4a661] text-[#c4a661]" size={14} />
                    <span className="text-xs font-bold text-[#c4a661]">
                      {hotel.rating}
                    </span>
                  </div>
                </div>

                {/* LOCATION */}
                <div className="flex items-center gap-1.5 text-slate-500">
                  <MapPin
                    size={16}
                    strokeWidth={2.5}
                    className="text-slate-400"
                  />
                  <span className="text-sm font-medium">{hotel.city}</span>
                </div>
              </div>

              {/* PRICE FOOTER */}
              <div className="mt-6 flex items-end justify-between border-t border-slate-100 pt-4">
                <div className="space-y-0.5">
                  <p className="text-[11px] font-semibold uppercase tracking-wider text-slate-400">
                    Mulai dari
                  </p>
                  <p className="text-xl font-black text-slate-900">
                    Rp {hotel.priceFrom.toLocaleString("id-ID")}
                  </p>
                </div>
                <p className="mb-1 text-xs font-medium text-slate-500">
                  / malam
                </p>
              </div>
            </div>
          </Link>
        ))}
      </div>
    </section>
  );
}
