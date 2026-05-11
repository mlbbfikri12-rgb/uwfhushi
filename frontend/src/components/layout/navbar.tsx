"use client";

import Link from "next/link";
import { useState } from "react";
import { NavbarBackground } from "./NavbarBackground";
import { Sparkles } from "lucide-react";

const NAV_LINKS = [
  { label: "Home", href: "/" },
  { label: "Destination", href: "/#destination" },
  { label: "Special Offers", href: "/#special-offers" },
  { label: "Brands", href: "/#brands" },
  { label: "Blog", href: "/#blog" },
  { label: "My Booking", href: "/booking" },
];

export function Navbar() {
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);

  const toggleMobileMenu = () => {
    setIsMobileMenuOpen((prev) => !prev);
  };

  const closeMobileMenu = () => {
    setIsMobileMenuOpen(false);
  };

  return (
    <NavbarBackground>
      <div className="max-w-7xl mx-auto flex items-center justify-between px-6 h-16 bg-transparent relative z-20">
        {/* LOGO */}
        <Link
          href="/"
          onClick={closeMobileMenu}
          className="flex items-center gap-2 group outline-none focus-visible:ring-2 focus-visible:ring-[#c4a661] rounded-lg"
        >
          <Sparkles
            className="text-[#c4a661] transition-all duration-500 group-hover:rotate-12 group-hover:scale-110"
            size={24}
            strokeWidth={1.5}
          />
          <span className="text-2xl font-light tracking-[0.2em] text-white">
            MY<span className="font-semibold text-[#c4a661]">LYNN</span>
          </span>
        </Link>

        {/* DESKTOP NAV */}
        <nav className="hidden md:flex items-center gap-7">
          {NAV_LINKS.map((link) => (
            <Link
              key={link.label}
              href={link.href}
              className="text-sm text-slate-200 hover:text-[#c4a661] transition-colors font-medium"
            >
              {link.label}
            </Link>
          ))}
        </nav>

        {/* DESKTOP AUTH BUTTON */}
        <Link
          href="/login"
          className="hidden md:flex items-center gap-2 bg-[#c4a661] hover:bg-[#b8954f] text-[#1a1f3c] font-semibold text-sm px-5 py-2.5 rounded-md transition-colors shrink-0"
        >
          Register / Sign In
          <svg
            xmlns="http://www.w3.org/2000/svg"
            width="16"
            height="16"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            strokeWidth="2.5"
            strokeLinecap="round"
            strokeLinejoin="round"
          >
            <path d="M15 3h4a2 2 0 012 2v14a2 2 0 01-2 2h-4" />
            <polyline points="10 17 15 12 10 7" />
            <line x1="15" y1="12" x2="3" y2="12" />
          </svg>
        </Link>

        {/* MOBILE MENU TOGGLE BUTTON */}
        <button
          className="md:hidden text-white p-2 focus:outline-none transition-transform duration-300"
          onClick={toggleMobileMenu}
          aria-label="Toggle Menu"
        >
          <div className="relative w-6 h-6 flex items-center justify-center">
            {/* Hamburger Icon */}
            <svg
              className={`absolute transition-all duration-300 ease-in-out ${
                isMobileMenuOpen
                  ? "opacity-0 rotate-90 scale-50"
                  : "opacity-100 rotate-0 scale-100"
              }`}
              width="22"
              height="22"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              strokeWidth="2"
              strokeLinecap="round"
              strokeLinejoin="round"
            >
              <line x1="3" y1="6" x2="21" y2="6" />
              <line x1="3" y1="12" x2="21" y2="12" />
              <line x1="3" y1="18" x2="21" y2="18" />
            </svg>
            {/* Close (X) Icon */}
            <svg
              className={`absolute transition-all duration-300 ease-in-out ${
                isMobileMenuOpen
                  ? "opacity-100 rotate-0 scale-100"
                  : "opacity-0 -rotate-90 scale-50"
              }`}
              width="22"
              height="22"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              strokeWidth="2"
              strokeLinecap="round"
              strokeLinejoin="round"
            >
              <line x1="18" y1="6" x2="6" y2="18" />
              <line x1="6" y1="6" x2="18" y2="18" />
            </svg>
          </div>
        </button>
      </div>

      {/* MOBILE MENU DROPDOWN */}
      <div
        className={`md:hidden absolute top-16 left-0 w-full bg-[#1a1f3c] shadow-xl overflow-hidden transition-all duration-500 ease-in-out ${
          isMobileMenuOpen
            ? "max-h-[500px] opacity-100 border-t border-white/10"
            : "max-h-0 opacity-0 pointer-events-none"
        }`}
      >
        <div className="px-6 pb-6 pt-4 flex flex-col gap-4">
          <nav className="flex flex-col gap-4">
            {NAV_LINKS.map((link, index) => (
              <Link
                key={link.label}
                href={link.href}
                onClick={closeMobileMenu}
                style={{ transitionDelay: `${index * 50}ms` }}
                className={`text-base text-slate-200 hover:text-[#c4a661] transition-all duration-300 font-medium border-b border-white/5 pb-2 ${
                  isMobileMenuOpen
                    ? "translate-x-0 opacity-100"
                    : "-translate-x-4 opacity-0"
                }`}
              >
                {link.label}
              </Link>
            ))}
          </nav>

          <Link
            href="/login"
            onClick={closeMobileMenu}
            className={`flex items-center justify-center gap-2 bg-[#c4a661] hover:bg-[#b8954f] text-[#1a1f3c] font-semibold text-base px-5 py-3 mt-2 rounded-md transition-all duration-500 delay-300 w-full ${
              isMobileMenuOpen
                ? "translate-y-0 opacity-100"
                : "translate-y-4 opacity-0"
            }`}
          >
            Register / Sign In
          </Link>
        </div>
      </div>
    </NavbarBackground>
  );
}
