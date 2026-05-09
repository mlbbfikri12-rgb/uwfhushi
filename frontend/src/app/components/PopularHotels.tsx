import { PublicPopularHotel } from "@/types/home";
import Image from "next/image";
import Link from "next/link";

function toImageUrl(url: string) {
  return /^https?:\/\//i.test(url)
    ? url
    : "https://images.unsplash.com/photo-1566073771259-6a8506099945?w=1200&q=80";
}

type Props = {
  hotels: PublicPopularHotel[];
};

export default function PopularHotels({ hotels }: Props) {
  return (
    <section className="mx-auto max-w-7xl px-6 py-10">
      <h2 className="mb-4 text-2xl font-bold text-slate-800">Popular Hotels</h2>

      <div className="grid gap-5 md:grid-cols-2 lg:grid-cols-3">
        {hotels.map((hotel) => (
          <Link
            key={hotel.hotelId}
            href={`/hotel/${hotel.slug}`}
            className="group overflow-hidden rounded-2xl border border-slate-200 bg-white transition-all hover:-translate-y-1 hover:shadow-xl"
          >
            {/* IMAGE */}
            <div className="relative h-52 overflow-hidden  rounded-2xl">
              <Image
                src={toImageUrl(hotel.image)}
                alt={hotel.name}
                fill
                className="object-cover transition-transform duration-500 group-hover:scale-105 will-change-transform"
              />

              {/* BRAND */}
              <div className="absolute left-4 top-4 rounded-full bg-white/90 px-3 py-1 text-xs font-semibold text-slate-800 backdrop-blur">
                {hotel.brand}
              </div>
            </div>

            {/* CONTENT */}
            <div className="space-y-3 p-5">
              <div>
                <h3 className="text-lg font-semibold text-slate-900">
                  {hotel.name}
                </h3>

                <div className="mt-1 flex items-center gap-2">
                  <div className="flex text-[#c4a661]">
                    {"★".repeat(hotel.rating)}
                  </div>

                  <span className="text-sm text-slate-500">{hotel.city}</span>
                </div>
              </div>

              <div className="border-t pt-3">
                <p className="text-xs uppercase tracking-wide text-slate-400">
                  Starting from
                </p>

                <p className="text-xl font-bold text-slate-900">
                  Rp {hotel.priceFrom.toLocaleString("id-ID")}
                </p>

                <p className="text-xs text-slate-500">per night</p>
              </div>
            </div>
          </Link>
        ))}
      </div>
    </section>
  );
}
