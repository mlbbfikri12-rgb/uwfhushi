export default function Footer() {
  return (
    <footer className="relative overflow-hidden bg-[#0f172a] text-white">
      {/* TOP GRADIENT */}
      <div className="absolute inset-x-0 top-0 h-px bg-gradient-to-r from-transparent via-[#c4a661] to-transparent" />

      {/* BACKGROUND GLOW */}
      <div className="pointer-events-none absolute -left-32 top-0 h-72 w-72 rounded-full bg-[#c4a661]/10 blur-3xl" />
      <div className="pointer-events-none absolute right-0 bottom-0 h-72 w-72 rounded-full bg-blue-500/10 blur-3xl" />

      <div className="relative mx-auto max-w-7xl px-6 py-16">
        {/* TOP SECTION */}
        <div className="grid gap-12 lg:grid-cols-[1.4fr_1fr_1fr_1fr]">
          {/* BRAND */}
          <div>
            <div className="flex items-center gap-1 text-3xl font-bold tracking-tight">
              <span>my</span>
              <span className="text-[#c4a661]">Lynn</span>
            </div>

            <p className="mt-5 max-w-md text-sm leading-relaxed text-slate-400">
              Discover curated stays, premium destinations, and unforgettable
              hospitality experiences across Indonesia.
            </p>

            {/* SOCIALS */}
            <div className="mt-6 flex items-center gap-3">
              {["Instagram", "Facebook", "Twitter", "YouTube"].map((social) => (
                <button
                  key={social}
                  className="rounded-full border border-white/10 bg-white/5 px-4 py-2 text-xs font-medium text-slate-300 transition-all hover:border-[#c4a661]/40 hover:bg-[#c4a661]/10 hover:text-white"
                >
                  {social}
                </button>
              ))}
            </div>
          </div>

          {/* COMPANY */}
          <div>
            <h3 className="text-sm font-semibold uppercase tracking-[0.2em] text-[#c4a661]">
              Company
            </h3>

            <ul className="mt-5 space-y-3 text-sm text-slate-400">
              {["About Us", "Careers", "Press", "Investor Relations"].map(
                (item) => (
                  <li key={item}>
                    <a href="#" className="transition-colors hover:text-white">
                      {item}
                    </a>
                  </li>
                ),
              )}
            </ul>
          </div>

          {/* SUPPORT */}
          <div>
            <h3 className="text-sm font-semibold uppercase tracking-[0.2em] text-[#c4a661]">
              Support
            </h3>

            <ul className="mt-5 space-y-3 text-sm text-slate-400">
              {[
                "Help Center",
                "Cancellation Options",
                "Safety Information",
                "Contact Support",
              ].map((item) => (
                <li key={item}>
                  <a href="#" className="transition-colors hover:text-white">
                    {item}
                  </a>
                </li>
              ))}
            </ul>
          </div>

          {/* NEWSLETTER */}
          <div>
            <h3 className="text-sm font-semibold uppercase tracking-[0.2em] text-[#c4a661]">
              Stay Updated
            </h3>

            <p className="mt-5 text-sm leading-relaxed text-slate-400">
              Get exclusive hotel deals, travel inspiration, and special offers.
            </p>

            <form className="mt-5 space-y-3">
              <input
                type="email"
                placeholder="Enter your email"
                className="w-full rounded-xl border border-white/10 bg-white/5 px-4 py-3 text-sm text-white placeholder:text-slate-500 outline-none transition-all focus:border-[#c4a661]/50 focus:bg-white/10"
              />

              <button
                type="submit"
                className="w-full rounded-xl bg-[#c4a661] px-4 py-3 text-sm font-semibold text-[#0f172a] transition-all hover:bg-[#d8b76d]"
              >
                Subscribe Newsletter
              </button>
            </form>
          </div>
        </div>

        {/* BOTTOM */}
        <div className="mt-14 flex flex-col items-center justify-between gap-4 border-t border-white/10 pt-6 text-sm text-slate-500 md:flex-row">
          <p>© {new Date().getFullYear()} myLynn. All rights reserved.</p>

          <div className="flex items-center gap-6">
            <a href="#" className="hover:text-white transition-colors">
              Privacy Policy
            </a>

            <a href="#" className="hover:text-white transition-colors">
              Terms of Service
            </a>

            <a href="#" className="hover:text-white transition-colors">
              Cookie Policy
            </a>
          </div>
        </div>
      </div>
    </footer>
  );
}
