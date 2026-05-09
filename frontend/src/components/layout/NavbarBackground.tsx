"use client";

import { useEffect, useState } from "react";

export function NavbarBackground({ children }: { children: React.ReactNode }) {
  const [scrolled, setScrolled] = useState(false);

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

  return (
    <header
      className={`fixed top-0 left-0 w-full z-50 transition-colors duration-300 ${
        scrolled ? "bg-[#1a1f3c] shadow-lg" : "bg-[#1a1f3c]/80 backdrop-blur-md"
      }`}
    >
      {children}
    </header>
  );
}
