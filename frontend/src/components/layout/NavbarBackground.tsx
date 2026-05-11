"use client";

import { useEffect, useState } from "react";
import { usePathname } from "next/navigation";

export function NavbarBackground({ children }: { children: React.ReactNode }) {
  const [scrolled, setScrolled] = useState(false);
  const pathname = usePathname();

  // Deteksi apakah saat ini berada di halaman Home
  const isHome = pathname === "/";

  useEffect(() => {
    let ticking = false;

    const onScroll = () => {
      if (!ticking) {
        window.requestAnimationFrame(() => {
          setScrolled(window.scrollY > 10);
          ticking = false;
        });

        ticking = true;
      }
    };

    window.addEventListener("scroll", onScroll);

    return () => {
      window.removeEventListener("scroll", onScroll);
    };
  }, []);

  // Menentukan class background berdasarkan halaman dan status scroll
  let bgClass = "";

  if (isHome) {
    // HOME: Transparan di pucuk, jadi biru blur saat di-scroll
    bgClass = scrolled
      ? "bg-[#1a1f3c] backdrop-blur-md shadow-lg"
      : "bg-transparent";
  } else {
    // BUKAN HOME: Selalu biru blur
    bgClass = "bg-[#1a1f3c] backdrop-blur-md shadow-lg border-b border-white/5";
  }

  return (
    <header
      className={`fixed left-0 top-0 z-50 w-full transition-all duration-300 ${bgClass}`}
    >
      {children}
    </header>
  );
}
