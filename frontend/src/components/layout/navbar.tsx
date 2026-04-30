"use client";

import Link from "next/link";

export function Navbar() {
  return (
    <header className="fixed top-0 left-0 w-full z-50 bg-[#1a1f3c]/70 backdrop-blur-md border-b border-white/10">
      <div className="max-w-7xl mx-auto flex items-center justify-between px-5 py-4">
        {/* Logo */}
        <h1 className="text-white font-bold text-lg tracking-wide">
          My<span className="text-[#c4a661]">Lynn</span>
        </h1>

        {/* Menu */}
        <nav className="hidden md:flex gap-6 text-sm text-slate-200">
          <Link href="/">Home</Link>
          <Link href="#">Destination</Link>
          <Link href="#">Offers</Link>
          <Link href="#">Blog</Link>
        </nav>

        {/* CTA */}
        <button className="bg-[#c4a661] text-[#1a1f3c] px-4 py-2 rounded-full text-sm font-semibold">
          Sign In
        </button>
      </div>
    </header>
  );
}
