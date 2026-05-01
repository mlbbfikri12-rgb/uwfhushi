"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";

type Props = {
  role: "SUPER_ADMIN" | "SPV" | "FO";
};

export function AdminSidebar({ role }: Props) {
  const pathname = usePathname();

  const active = (path: string) =>
    pathname === path
      ? "bg-[#c4a661] text-[#1a1f3c]"
      : "text-slate-300 hover:bg-white/10";

  return (
    <aside className="w-64 bg-[#1a1f3c] text-white flex flex-col">
      {/* LOGO */}
      <div className="px-6 py-5 border-b border-white/10">
        <h1 className="text-xl font-bold">
          My<span className="text-[#c4a661]">Lynn</span>
        </h1>
      </div>

      {/* MENU */}
      <nav className="flex-1 px-3 py-4 space-y-1 text-sm">
        <Link
          href="/admin"
          className={`block px-3 py-2 rounded ${active("/admin")}`}
        >
          Dashboard
        </Link>

        {role === "SUPER_ADMIN" && (
          <>
            <Link
              href="/admin/branches"
              className={`block px-3 py-2 rounded ${active("/admin/branches")}`}
            >
              Branch
            </Link>
            <Link
              href="/admin/staff"
              className={`block px-3 py-2 rounded ${active("/admin/staff")}`}
            >
              Staff
            </Link>
            <Link
              href="/admin/banners"
              className={`block px-3 py-2 rounded ${active("/admin/banners")}`}
            >
              Banner
            </Link>
            <Link
              href="/admin/master/cities"
              className={`block px-3 py-2 rounded ${active("/admin/master/cities")}`}
            >
              Master Cities
            </Link>
            <Link
              href="/admin/master/brands"
              className={`block px-3 py-2 rounded ${active("/admin/master/brands")}`}
            >
              Master Brands
            </Link>
            <Link
              href="/admin/master/hotels"
              className={`block px-3 py-2 rounded ${active("/admin/master/hotels")}`}
            >
              Master Hotels
            </Link>
            <Link
              href="/admin/master/facilities"
              className={`block px-3 py-2 rounded ${active("/admin/master/facilities")}`}
            >
              Master Facilities
            </Link>
          </>
        )}

        {(role === "SPV" || role === "FO") && (
          <Link
            href="/admin/rooms"
            className={`block px-3 py-2 rounded ${active("/admin/rooms")}`}
          >
            Rooms
          </Link>
        )}
      </nav>

      {/* FOOTER */}
      <div className="p-4 border-t border-white/10">
        <button className="w-full rounded bg-white/10 py-2 text-sm hover:bg-white/20">
          Logout
        </button>
      </div>
    </aside>
  );
}
