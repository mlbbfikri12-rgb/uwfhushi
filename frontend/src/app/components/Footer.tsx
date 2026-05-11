import { Send, Sparkles } from "lucide-react";

// Custom SVG Components untuk Sosmed agar tidak bergantung pada library eksternal
const InstagramIcon = ({
  size,
  className,
}: {
  size: number;
  className?: string;
}) => (
  <svg
    width={size}
    height={size}
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    strokeWidth="2"
    strokeLinecap="round"
    strokeLinejoin="round"
    className={className}
  >
    <rect x="2" y="2" width="20" height="20" rx="5" ry="5"></rect>
    <path d="M16 11.37A4 4 0 1 1 12.63 8 4 4 0 0 1 16 11.37z"></path>
    <line x1="17.5" y1="6.5" x2="17.51" y2="6.5"></line>
  </svg>
);

const FacebookIcon = ({
  size,
  className,
}: {
  size: number;
  className?: string;
}) => (
  <svg
    width={size}
    height={size}
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    strokeWidth="2"
    strokeLinecap="round"
    strokeLinejoin="round"
    className={className}
  >
    <path d="M18 2h-3a5 5 0 0 0-5 5v3H7v4h3v8h4v-8h3l1-4h-4V7a1 1 0 0 1 1-1h3z"></path>
  </svg>
);

const TwitterIcon = ({
  size,
  className,
}: {
  size: number;
  className?: string;
}) => (
  <svg
    width={size}
    height={size}
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    strokeWidth="2"
    strokeLinecap="round"
    strokeLinejoin="round"
    className={className}
  >
    <path d="M22 4s-.7 2.1-2 3.4c1.6 10-9.4 17.3-18 11.6 2.2.1 4.4-.6 6-2C3 15.5.5 9.6 3 5c2.2 2.6 5.6 4.1 9 4-.9-4.2 4-6.6 7-3.8 1.1 0 3-1.2 3-1.2z"></path>
  </svg>
);

const YoutubeIcon = ({
  size,
  className,
}: {
  size: number;
  className?: string;
}) => (
  <svg
    width={size}
    height={size}
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    strokeWidth="2"
    strokeLinecap="round"
    strokeLinejoin="round"
    className={className}
  >
    <path d="M22.54 6.42a2.78 2.78 0 0 0-1.94-2C18.88 4 12 4 12 4s-6.88 0-8.6.46a2.78 2.78 0 0 0-1.94 2A29 29 0 0 0 1 11.75a29 29 0 0 0 .46 5.33A2.78 2.78 0 0 0 3.4 19c1.72.46 8.6.46 8.6.46s6.88 0 8.6-.46a2.78 2.78 0 0 0 1.94-2 29 29 0 0 0 .46-5.25 29 29 0 0 0-.46-5.33z"></path>
    <polygon points="9.75 15.02 15.5 11.75 9.75 8.48 9.75 15.02"></polygon>
  </svg>
);

const SOCIALS = [
  { name: "Instagram", icon: InstagramIcon, href: "#" },
  { name: "Facebook", icon: FacebookIcon, href: "#" },
  { name: "Twitter", icon: TwitterIcon, href: "#" },
  { name: "YouTube", icon: YoutubeIcon, href: "#" },
];

const COMPANY_LINKS = ["About Us", "Careers", "Press", "Investor Relations"];
const SUPPORT_LINKS = [
  "Help Center",
  "Cancellation Options",
  "Safety Information",
  "Contact Support",
];

export default function Footer() {
  return (
    <footer className="relative overflow-hidden bg-[#1a1f3c] text-white">
      {/* TOP GRADIENT BORDER */}
      <div className="absolute inset-x-0 top-0 h-px bg-gradient-to-r from-transparent via-[#c4a661] to-transparent opacity-70" />

      {/* BACKGROUND GLOW */}
      <div className="pointer-events-none absolute -left-32 top-0 h-72 w-72 rounded-full bg-[#c4a661]/10 blur-[100px]" />
      <div className="pointer-events-none absolute right-0 bottom-0 h-72 w-72 rounded-full bg-blue-400/10 blur-[100px]" />

      <div className="relative mx-auto max-w-7xl px-6 py-16 lg:py-20">
        {/* TOP SECTION */}
        <div className="grid gap-12 md:grid-cols-2 lg:grid-cols-[1.4fr_1fr_1fr_1.2fr]">
          {/* BRANDING */}
          <div className="pr-4">
            <div className="flex items-center gap-2 group outline-none focus-visible:ring-2 focus-visible:ring-[#c4a661] rounded-lg">
              <Sparkles
                className="text-[#c4a661] transition-all duration-500 group-hover:rotate-12 group-hover:scale-110"
                size={24}
                strokeWidth={1.5}
              />
              <span className="text-2xl font-light tracking-[0.2em] text-white">
                MY<span className="font-semibold text-[#c4a661]">LYNN</span>
              </span>
            </div>

            <p className="mt-5 max-w-sm text-sm leading-relaxed text-slate-400">
              Discover curated stays, premium destinations, and unforgettable
              hospitality experiences across Indonesia.
            </p>

            {/* SOCIAL ICONS */}
            <div className="mt-8 flex items-center gap-3">
              {SOCIALS.map((social) => {
                const Icon = social.icon;
                return (
                  <a
                    key={social.name}
                    href={social.href}
                    aria-label={social.name}
                    className="group flex h-10 w-10 items-center justify-center rounded-full border border-white/10 bg-white/5 text-slate-400 transition-all hover:border-[#c4a661]/40 hover:bg-[#c4a661]/10 hover:text-[#c4a661]"
                  >
                    <Icon
                      size={18}
                      className="transition-transform group-hover:scale-110"
                    />
                  </a>
                );
              })}
            </div>
          </div>

          {/* COMPANY LINKS */}
          <div>
            <h3 className="text-xs font-bold uppercase tracking-[0.2em] text-[#c4a661]">
              Company
            </h3>
            <ul className="mt-6 space-y-4 text-sm text-slate-400">
              {COMPANY_LINKS.map((item) => (
                <li key={item}>
                  <a
                    href="#"
                    className="inline-block transition-all hover:translate-x-1 hover:text-white"
                  >
                    {item}
                  </a>
                </li>
              ))}
            </ul>
          </div>

          {/* SUPPORT LINKS */}
          <div>
            <h3 className="text-xs font-bold uppercase tracking-[0.2em] text-[#c4a661]">
              Support
            </h3>
            <ul className="mt-6 space-y-4 text-sm text-slate-400">
              {SUPPORT_LINKS.map((item) => (
                <li key={item}>
                  <a
                    href="#"
                    className="inline-block transition-all hover:translate-x-1 hover:text-white"
                  >
                    {item}
                  </a>
                </li>
              ))}
            </ul>
          </div>

          {/* NEWSLETTER */}
          <div>
            <h3 className="text-xs font-bold uppercase tracking-[0.2em] text-[#c4a661]">
              Stay Updated
            </h3>
            <p className="mt-6 text-sm leading-relaxed text-slate-400">
              Get exclusive hotel deals, travel inspiration, and special offers
              directly to your inbox.
            </p>

            <form className="mt-5 space-y-3">
              <div className="relative">
                <input
                  type="email"
                  placeholder="Enter your email"
                  className="w-full rounded-xl border border-white/10 bg-white/5 px-4 py-3.5 text-sm text-white placeholder:text-slate-500 outline-none transition-all focus:border-[#c4a661]/50 focus:bg-white/10 focus:ring-1 focus:ring-[#c4a661]/50"
                  required
                />
              </div>

              <button
                type="submit"
                className="group flex w-full items-center justify-center gap-2 rounded-xl bg-[#c4a661] px-4 py-3.5 text-sm font-bold text-[#1a1f3c] transition-all hover:bg-[#d8b76d] active:scale-[0.98]"
              >
                Subscribe
                <Send
                  size={16}
                  className="transition-transform group-hover:-translate-y-0.5 group-hover:translate-x-0.5"
                />
              </button>
            </form>
          </div>
        </div>

        {/* BOTTOM LEGAL */}
        <div className="mt-16 flex flex-col items-center justify-between gap-4 border-t border-white/10 pt-8 text-xs font-medium text-slate-500 md:flex-row">
          <p>© {new Date().getFullYear()} myLynn. All rights reserved.</p>

          <div className="flex items-center gap-6">
            {["Privacy Policy", "Terms of Service", "Cookie Policy"].map(
              (policy) => (
                <a
                  key={policy}
                  href="#"
                  className="hover:text-[#c4a661] transition-colors"
                >
                  {policy}
                </a>
              ),
            )}
          </div>
        </div>
      </div>
    </footer>
  );
}
