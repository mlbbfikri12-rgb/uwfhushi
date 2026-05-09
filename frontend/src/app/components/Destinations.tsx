import { PublicDestination } from "@/types/home";
import Link from "next/link";

type Props = {
  destinations: PublicDestination[];
};

export default function Destinations({ destinations }: Props) {
  return (
    <section className="bg-slate-50 py-10">
      <div className="mx-auto max-w-7xl px-6">
        <h2 className="mb-4 text-2xl font-bold text-slate-800">Destinations</h2>

        <div className="grid gap-4 md:grid-cols-3">
          {destinations.map((destination) => (
            <Link
              key={destination.city}
              href={`/search?q=${encodeURIComponent(destination.city)}`}
              className="group relative overflow-hidden rounded-2xl bg-gradient-to-br from-[#1a1f3c] to-[#2d3561] p-6 text-white transition-all hover:-translate-y-1 hover:shadow-xl"
            >
              <div className="absolute inset-0 bg-white/5 opacity-0 transition-opacity group-hover:opacity-100" />

              <div className="relative z-10">
                <p className="text-sm uppercase tracking-[0.2em] text-slate-300">
                  Destination
                </p>

                <h3 className="mt-2 text-2xl font-bold">{destination.city}</h3>

                <p className="mt-6 text-sm text-slate-300">Starting from</p>

                <p className="text-2xl font-bold text-[#c4a661]">
                  Rp {destination.minPrice.toLocaleString("id-ID")}
                </p>
              </div>
            </Link>
          ))}
        </div>
      </div>
    </section>
  );
}
