"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation } from "@tanstack/react-query";
import Link from "next/link";
import Image from "next/image";
import { useRouter, useSearchParams } from "next/navigation";
import { useForm } from "react-hook-form";
import { toast } from "sonner";
import {
  ArrowRight,
  Eye,
  EyeOff,
  Loader2,
  LockKeyhole,
  Mail,
  Sparkles,
  Star,
  Quote,
} from "lucide-react";
import { useMemo, useState } from "react";
import { z } from "zod";
import { loginClient } from "@/services/auth.service";

const schema = z.object({
  email: z
    .string()
    .min(1, "Email wajib diisi")
    .email("Format email tidak valid"),
  password: z.string().min(8, "Password minimal 8 karakter"),
});

type LoginValues = z.infer<typeof schema>;

export default function LoginClient() {
  const router = useRouter();
  const searchParams = useSearchParams();

  const redirect = useMemo(() => {
    const raw = searchParams.get("redirect");
    if (!raw) return "/";
    if (!raw.startsWith("/")) return "/";
    return raw;
  }, [searchParams]);

  const [showPassword, setShowPassword] = useState(false);

  const {
    register,
    handleSubmit,
    formState: { errors, isValid },
  } = useForm<LoginValues>({
    resolver: zodResolver(schema),
    mode: "onChange", // UX: Validasi secara real-time saat mengetik
    defaultValues: {
      email: "",
      password: "",
    },
  });

  const mutation = useMutation({
    mutationFn: loginClient,
    onSuccess: () => {
      toast.success("Login berhasil", {
        description: "Selamat datang kembali di myLynn.",
      });
      router.replace(redirect);
    },
    onError: (error) => {
      console.log(error.message);
      toast.error("Gagal masuk", {
        description:
          error instanceof Error
            ? error.message
            : "Periksa kembali email dan password Anda.",
      });
    },
  });

  const onSubmit = async (values: LoginValues) => {
    if (mutation.isPending) return;
    await mutation.mutateAsync(values);
  };

  return (
    <main className="relative min-h-screen overflow-hidden bg-[#050810] selection:bg-[#c4a661]/30 selection:text-white">
      {/* BACKGROUND & AMBIENT */}
      <div className="absolute inset-0">
        <Image
          src="https://images.unsplash.com/photo-1542314831-c6a4d2741528?q=80&w=2000&auto=format&fit=crop"
          alt="Hotel Background"
          fill
          priority
          className="object-cover opacity-40 mix-blend-luminosity transition-transform duration-[20s] ease-linear hover:scale-105"
        />
        <div className="absolute inset-0 bg-gradient-to-r from-[#050810] via-[#050810]/90 to-[#050810]/40" />
        <div className="absolute inset-0 bg-[radial-gradient(ellipse_at_top,_var(--tw-gradient-stops))] from-transparent via-[#050810]/80 to-[#050810]" />

        {/* LIGHT FLARES */}
        <div className="absolute left-[-10%] top-0 h-[600px] w-[600px] rounded-full bg-[#c4a661]/10 blur-[120px]" />
      </div>

      {/* NAVBAR */}
      <header className="relative z-20 border-b border-white/5 bg-transparent">
        <div className="mx-auto flex h-24 max-w-7xl items-center justify-between px-6 lg:px-8">
          <Link
            href="/"
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

          <nav className="hidden items-center gap-10 text-xs font-semibold uppercase tracking-widest text-white/50 lg:flex">
            {["Home", "Destination", "Offers", "Journal"].map((item) => (
              <Link
                key={item}
                href={`/${item.toLowerCase()}`}
                className="transition-all hover:text-white hover:drop-shadow-[0_0_8px_rgba(255,255,255,0.5)]"
              >
                {item}
              </Link>
            ))}
          </nav>

          <Link
            href={`/register?redirect=${encodeURIComponent(redirect)}`}
            className="hidden items-center gap-2 rounded-full border border-white/10 bg-white/5 px-6 py-2.5 text-xs font-semibold uppercase tracking-widest text-white backdrop-blur-sm transition-all hover:bg-white/10 hover:border-white/30 hover:shadow-[0_0_20px_rgba(196,166,97,0.1)] lg:flex"
          >
            Register
          </Link>
        </div>
      </header>

      {/* CONTENT */}
      <div className="relative z-10 mx-auto grid min-h-[calc(100vh-96px)] max-w-7xl items-center gap-16 px-6 py-12 lg:grid-cols-[1fr_480px] lg:px-8">
        {/* LEFT SECTION */}
        <section className="hidden flex-col justify-center lg:flex">
          <div className="max-w-xl">
            <h1 className="text-5xl font-light leading-[1.15] tracking-tight text-white xl:text-6xl">
              Curated luxury, <br />
              <span className="font-serif italic text-[#c4a661]">
                tailored for you.
              </span>
            </h1>

            <p className="mt-8 text-lg font-light leading-relaxed text-white/60">
              Akses portal pribadi Anda untuk mengelola masa inap, preferensi
              kamar, dan menikmati layanan pramutamu (concierge) eksklusif 24/7.
            </p>

            {/* FLOATING TESTIMONIAL (UI Detail) */}
            <div className="mt-16 inline-block rounded-2xl border border-white/10 bg-white/[0.02] p-6 backdrop-blur-md relative">
              <Quote
                className="absolute -top-3 -left-3 text-[#c4a661]/40"
                size={32}
              />
              <div className="flex gap-1 text-[#c4a661] mb-3">
                {[1, 2, 3, 4, 5].map((star) => (
                  <Star key={star} size={14} fill="currentColor" />
                ))}
              </div>
              <p className="text-sm font-light italic text-white/80 max-w-sm">
                &quot;Pengalaman reservasi yang paling mulus yang pernah saya
                rasakan. Detail kecil yang mereka berikan benar-benar
                mendefinisikan ulang makna kemewahan.&quot;
              </p>
              <div className="mt-4 flex items-center gap-3">
                <div className="h-8 w-8 rounded-full bg-gradient-to-tr from-[#c4a661] to-[#fcf0d5] p-[1px]">
                  <div className="h-full w-full rounded-full border border-[#050810] bg-[url('https://images.unsplash.com/photo-1573496359142-b8d87734a5a2?q=80&w=200&auto=format&fit=crop')] bg-cover" />
                </div>
                <div>
                  <p className="text-xs font-semibold text-white">
                    Eleanor Richards
                  </p>
                  <p className="text-[10px] text-white/40 uppercase tracking-widest">
                    Elite Member
                  </p>
                </div>
              </div>
            </div>
          </div>
        </section>

        {/* RIGHT SECTION - LOGIN CARD */}
        <section className="w-full">
          <div className="relative overflow-hidden rounded-[2.5rem] border border-white/[0.08] bg-[#0a0d17]/80 p-8 shadow-[0_0_80px_-20px_rgba(0,0,0,0.8)] backdrop-blur-2xl sm:p-12">
            <div className="mb-10">
              <h2 className="text-3xl font-light tracking-tight text-white">
                Sign{" "}
                <span className="font-serif italic text-[#c4a661]">In</span>
              </h2>
              <p className="mt-2 text-sm font-light text-white/50">
                Lanjutkan perjalanan eksklusif Anda bersama kami.
              </p>
            </div>

            <form onSubmit={handleSubmit(onSubmit)}>
              {/* UX: Fieldset disables all inputs smoothly when loading */}
              <fieldset
                disabled={mutation.isPending}
                className="space-y-6 group/form disabled:opacity-70 disabled:cursor-not-allowed"
              >
                {/* EMAIL INPUT */}
                <div className="space-y-2">
                  <label
                    htmlFor="email"
                    className="cursor-pointer text-[11px] font-semibold tracking-widest text-white/60 uppercase transition-colors hover:text-white"
                  >
                    Email Address
                  </label>
                  <div className="group relative">
                    <Mail
                      size={18}
                      className={`absolute left-4 top-1/2 -translate-y-1/2 transition-colors ${errors.email ? "text-red-400" : "text-white/30 group-focus-within:text-[#c4a661]"}`}
                    />
                    <input
                      id="email"
                      type="email"
                      autoComplete="email"
                      placeholder="name@example.com"
                      className={`h-14 w-full rounded-xl border bg-white/[0.03] pl-11 pr-4 text-sm text-white placeholder:text-white/20 outline-none backdrop-blur-sm transition-all shadow-[inset_0_2px_4px_rgba(0,0,0,0.2)] focus:bg-white/[0.05] focus:ring-4 ${
                        errors.email
                          ? "border-red-500/50 focus:border-red-500 focus:ring-red-500/10"
                          : "border-white/10 focus:border-[#c4a661] focus:ring-[#c4a661]/10"
                      }`}
                      {...register("email")}
                    />
                  </div>
                  {errors.email && (
                    <p className="text-xs font-medium text-red-400 animate-in fade-in slide-in-from-top-1">
                      {errors.email.message}
                    </p>
                  )}
                </div>

                {/* PASSWORD INPUT */}
                <div className="space-y-2">
                  <div className="flex items-center justify-between">
                    <label
                      htmlFor="password"
                      className="cursor-pointer text-[11px] font-semibold tracking-widest text-white/60 uppercase transition-colors hover:text-white"
                    >
                      Password
                    </label>
                    <Link
                      href="/forgot-password"
                      className="text-[11px] font-semibold text-[#c4a661] transition-colors hover:text-white hover:underline underline-offset-4"
                    >
                      Forgot Password?
                    </Link>
                  </div>
                  <div className="group relative">
                    <LockKeyhole
                      size={18}
                      className={`absolute left-4 top-1/2 -translate-y-1/2 transition-colors ${errors.password ? "text-red-400" : "text-white/30 group-focus-within:text-[#c4a661]"}`}
                    />
                    <input
                      id="password"
                      type={showPassword ? "text" : "password"}
                      autoComplete="current-password"
                      placeholder="••••••••"
                      className={`h-14 w-full rounded-xl border bg-white/[0.03] pl-11 pr-12 text-sm text-white placeholder:text-white/20 outline-none backdrop-blur-sm transition-all shadow-[inset_0_2px_4px_rgba(0,0,0,0.2)] focus:bg-white/[0.05] focus:ring-4 ${
                        errors.password
                          ? "border-red-500/50 focus:border-red-500 focus:ring-red-500/10"
                          : "border-white/10 focus:border-[#c4a661] focus:ring-[#c4a661]/10"
                      }`}
                      {...register("password")}
                    />
                    <button
                      type="button"
                      tabIndex={-1} // UX: Prevent tab stopping on the eye icon
                      onClick={() => setShowPassword((prev) => !prev)}
                      className="absolute right-4 top-1/2 -translate-y-1/2 text-white/30 transition-colors hover:text-white"
                      aria-label={
                        showPassword
                          ? "Sembunyikan password"
                          : "Tampilkan password"
                      }
                    >
                      {showPassword ? <EyeOff size={18} /> : <Eye size={18} />}
                    </button>
                  </div>
                  {errors.password && (
                    <p className="text-xs font-medium text-red-400 animate-in fade-in slide-in-from-top-1">
                      {errors.password.message}
                    </p>
                  )}
                </div>

                {/* REMEMBER ME */}
                <div className="flex items-center pt-1">
                  <label
                    htmlFor="remember"
                    className="group flex cursor-pointer items-center gap-3 text-sm text-white/50 transition-colors hover:text-white"
                  >
                    <div className="relative flex h-4 w-4 items-center justify-center rounded-[4px] border border-white/20 bg-white/5 transition-colors group-hover:border-[#c4a661]">
                      <input
                        id="remember"
                        type="checkbox"
                        className="peer absolute h-full w-full cursor-pointer opacity-0"
                      />
                      <svg
                        className="h-2.5 w-2.5 text-[#c4a661] opacity-0 transition-all scale-50 peer-checked:scale-100 peer-checked:opacity-100 peer-focus-visible:ring-2 peer-focus-visible:ring-[#c4a661] peer-focus-visible:ring-offset-2 peer-focus-visible:ring-offset-[#0a0d17]"
                        fill="none"
                        viewBox="0 0 24 24"
                        stroke="currentColor"
                        strokeWidth="3"
                      >
                        <path
                          strokeLinecap="round"
                          strokeLinejoin="round"
                          d="M5 13l4 4L19 7"
                        />
                      </svg>
                    </div>
                    <span>Tetap masuk</span>
                  </label>
                </div>

                {/* SUBMIT BUTTON */}
                <button
                  type="submit"
                  className={`group relative mt-2 flex h-14 w-full items-center justify-center gap-2 rounded-xl text-sm font-semibold transition-all overflow-hidden shadow-[inset_0_1px_1px_rgba(255,255,255,0.2)] ${
                    isValid
                      ? "bg-[#c4a661] text-[#050810] hover:bg-[#d4b671] hover:shadow-[0_0_20px_rgba(196,166,97,0.3)]"
                      : "bg-white/10 text-white/40 cursor-not-allowed"
                  }`}
                >
                  {isValid && !mutation.isPending && (
                    <div className="absolute inset-0 flex h-full w-full justify-center [transform:skew(-12deg)_translateX(-150%)] group-hover:duration-1000 group-hover:[transform:skew(-12deg)_translateX(150%)]">
                      <div className="relative h-full w-8 bg-white/20" />
                    </div>
                  )}

                  {mutation.isPending ? (
                    <>
                      <Loader2
                        size={18}
                        className="animate-spin text-[#050810]"
                      />
                      Authenticating...
                    </>
                  ) : (
                    <>
                      Login
                      <ArrowRight
                        size={18}
                        className={`transition-transform ${isValid ? "group-hover:translate-x-1" : ""}`}
                      />
                    </>
                  )}
                </button>
              </fieldset>
            </form>

            <div className="mt-8 border-t border-white/[0.05] pt-8 text-center">
              <p className="text-[13px] text-white/40">
                Anggota baru di myLynn?{" "}
                <Link
                  href={`/register?redirect=${encodeURIComponent(redirect)}`}
                  className="font-semibold text-[#c4a661] transition-colors hover:text-white hover:underline underline-offset-4"
                >
                  Daftar sekarang
                </Link>
              </p>
            </div>
          </div>
        </section>
      </div>
    </main>
  );
}
