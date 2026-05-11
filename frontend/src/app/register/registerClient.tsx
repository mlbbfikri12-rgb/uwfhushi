"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation } from "@tanstack/react-query";
import Image from "next/image";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useMemo, useState } from "react";
import { useForm } from "react-hook-form";
import { toast } from "sonner";
import {
  ArrowRight,
  Check,
  Eye,
  EyeOff,
  Loader2,
  LockKeyhole,
  Mail,
  Phone,
  User2,
  Sparkles,
} from "lucide-react";
import { z } from "zod";
import { registerClient } from "@/services/auth.service";

const schema = z.object({
  name: z.string().min(2, "Nama minimal 2 karakter"),
  email: z
    .string()
    .min(1, "Email wajib diisi")
    .email("Format email tidak valid"),
  phone: z.string().min(8, "Nomor telepon terlalu pendek"),
  password: z.string().min(8, "Password minimal 8 karakter"),
});

type RegisterValues = z.infer<typeof schema>;

export default function RegisterClient() {
  const router = useRouter();
  const [showPassword, setShowPassword] = useState(false);

  const {
    register,
    handleSubmit,
    watch,
    formState: { errors, isValid },
  } = useForm<RegisterValues>({
    resolver: zodResolver(schema),
    mode: "onChange",

    defaultValues: {
      name: "",
      email: "",
      phone: "",
      password: "",
    },
  });

  const password = watch("password");

  const passwordStrength = useMemo(() => {
    if (!password) return 0;
    let score = 0;
    if (password.length >= 8) score++;
    if (/[A-Z]/.test(password)) score++;
    if (/[0-9]/.test(password)) score++;
    if (/[^A-Za-z0-9]/.test(password)) score++;
    return score;
  }, [password]);

  const passwordLabel = useMemo(() => {
    if (passwordStrength <= 1) return "Weak";
    if (passwordStrength <= 3) return "Medium";
    return "Strong";
  }, [passwordStrength]);

  const mutation = useMutation({
    mutationFn: registerClient,
    onSuccess: (_, variables) => {
      toast.success("Registrasi berhasil", {
        description: "Silakan periksa kotak masuk untuk verifikasi email Anda.",
      });
      router.push(
        `/verify-email-info?email=${encodeURIComponent(variables.email)}`,
      );
    },
    onError: (error) => {
      toast.error("Gagal mendaftar", {
        description:
          error instanceof Error
            ? error.message
            : "Terjadi kesalahan, silakan coba lagi.",
      });
    },
  });

  const onSubmit = async (values: RegisterValues) => {
    if (mutation.isPending) return;
    await mutation.mutateAsync(values);
  };

  return (
    <main className="relative min-h-screen overflow-hidden bg-[#050810] selection:bg-[#c4a661]/30 selection:text-white">
      {/* BACKGROUND & AMBIENT */}
      <div className="absolute inset-0">
        <Image
          src="https://images.unsplash.com/photo-1506744038136-46273834b3fb?q=80&w=2000&auto=format&fit=crop"
          alt="Resort"
          fill
          priority
          className="object-cover opacity-30 mix-blend-luminosity transition-transform duration-[30s] ease-linear hover:scale-110"
        />
        <div className="absolute inset-0 bg-gradient-to-r from-[#050810] via-[#050810]/90 to-[#050810]/40" />
        <div className="absolute left-[-10%] top-[10%] h-[600px] w-[600px] rounded-full bg-[#c4a661]/10 blur-[120px]" />
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
            {["Home", "Destination", "Offers", "Brands"].map((item) => (
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
            href="/login"
            className="hidden items-center gap-2 rounded-full border border-white/10 bg-white/5 px-6 py-2.5 text-xs font-semibold uppercase tracking-widest text-white backdrop-blur-sm transition-all hover:bg-white/10 hover:border-white/30 hover:shadow-[0_0_20px_rgba(196,166,97,0.1)] lg:flex"
          >
            Sign In
          </Link>
        </div>
      </header>

      {/* CONTENT */}
      <div className="relative z-10 mx-auto grid min-h-[calc(100vh-96px)] max-w-7xl items-center gap-12 px-6 py-12 lg:grid-cols-[1.1fr_540px] lg:px-8">
        {/* LEFT SECTION (Teks dinormalkan) */}
        <section className="hidden flex-col justify-center lg:flex">
          <div className="max-w-2xl">
            <div className="mb-8 inline-flex items-center rounded-full border border-white/20 bg-white/5 px-4 py-1.5 text-xs font-medium tracking-widest text-white/80 backdrop-blur-md">
              Create an Account
            </div>

            <h1 className="text-5xl font-light leading-[1.2] tracking-tight text-white xl:text-6xl">
              Gabung bersama <br />
              <span className="font-semibold text-[#c4a661]">myLynn.</span>
            </h1>

            <p className="mt-6 max-w-xl text-lg font-light leading-relaxed text-white/60">
              Buat akun sekarang untuk mempermudah proses booking penginapan dan
              mengelola riwayat perjalanan Anda di satu tempat.
            </p>

            {/* BENEFITS (Realistis) */}
            <div className="mt-10 space-y-4">
              {[
                "Proses booking lebih cepat",
                "Kelola riwayat pesanan dengan mudah",
                "Simpan daftar penginapan favorit",
              ].map((item) => (
                <div key={item} className="flex items-center gap-4">
                  <div className="flex h-8 w-8 items-center justify-center rounded-full bg-white/5 border border-white/10 text-white/80">
                    <Check size={14} strokeWidth={2} />
                  </div>
                  <p className="text-sm font-light text-white/80">{item}</p>
                </div>
              ))}
            </div>
          </div>
        </section>

        {/* RIGHT SECTION - REGISTER CARD */}
        <section className="w-full">
          <div className="relative overflow-hidden rounded-[2.5rem] border border-white/[0.08] bg-[#0a0d17]/80 shadow-[0_0_80px_-20px_rgba(0,0,0,0.8)] backdrop-blur-2xl">
            {/* CARD IMAGE HEADER */}
            <div className="relative h-32 overflow-hidden sm:h-36">
              <Image
                src="https://images.unsplash.com/photo-1520250497591-112f2f40a3f4?q=80&w=1400&auto=format&fit=crop"
                alt="Hotel"
                fill
                className="object-cover opacity-60 mix-blend-screen"
              />
              <div className="absolute inset-0 bg-gradient-to-t from-[#0a0d17] via-[#0a0d17]/60 to-transparent" />
              <div className="absolute bottom-6 left-8 sm:bottom-8 sm:left-10">
                <h2 className="text-2xl font-semibold text-white">Register</h2>
                <p className="mt-1 text-sm text-white/60">
                  Isi data diri untuk melanjutkan
                </p>
              </div>
            </div>

            {/* FORM */}
            <div className="px-8 pb-10 pt-4 sm:px-10 sm:pb-12">
              <form onSubmit={handleSubmit(onSubmit)}>
                <fieldset
                  disabled={mutation.isPending}
                  className="space-y-5 group/form disabled:opacity-70 disabled:cursor-not-allowed"
                >
                  {/* FULL NAME */}
                  <div className="space-y-1.5">
                    <label
                      htmlFor="name"
                      className="cursor-pointer text-[11px] font-semibold tracking-widest text-white/60 uppercase transition-colors hover:text-white"
                    >
                      Nama Lengkap
                    </label>
                    <div className="group relative">
                      <User2
                        size={18}
                        className={`absolute left-4 top-1/2 -translate-y-1/2 transition-colors ${errors.name ? "text-red-400" : "text-white/30 group-focus-within:text-[#c4a661]"}`}
                      />
                      <input
                        id="name"
                        type="text"
                        placeholder="John Doe"
                        className={`h-12 w-full rounded-xl border bg-white/[0.03] pl-11 pr-4 text-sm text-white placeholder:text-white/20 outline-none backdrop-blur-sm transition-all focus:bg-white/[0.05] focus:ring-4 ${
                          errors.name
                            ? "border-red-500/50 focus:border-red-500 focus:ring-red-500/10"
                            : "border-white/10 focus:border-[#c4a661] focus:ring-[#c4a661]/10"
                        }`}
                        {...register("name")}
                      />
                    </div>
                    {errors.name && (
                      <p className="text-[11px] font-medium text-red-400">
                        {errors.name.message}
                      </p>
                    )}
                  </div>

                  {/* EMAIL */}
                  <div className="space-y-1.5">
                    <label
                      htmlFor="email"
                      className="cursor-pointer text-[11px] font-semibold tracking-widest text-white/60 uppercase transition-colors hover:text-white"
                    >
                      Alamat Email
                    </label>
                    <div className="group relative">
                      <Mail
                        size={18}
                        className={`absolute left-4 top-1/2 -translate-y-1/2 transition-colors ${errors.email ? "text-red-400" : "text-white/30 group-focus-within:text-[#c4a661]"}`}
                      />
                      <input
                        id="email"
                        type="email"
                        placeholder="anda@email.com"
                        className={`h-12 w-full rounded-xl border bg-white/[0.03] pl-11 pr-4 text-sm text-white placeholder:text-white/20 outline-none backdrop-blur-sm transition-all focus:bg-white/[0.05] focus:ring-4 ${
                          errors.email
                            ? "border-red-500/50 focus:border-red-500 focus:ring-red-500/10"
                            : "border-white/10 focus:border-[#c4a661] focus:ring-[#c4a661]/10"
                        }`}
                        {...register("email")}
                      />
                    </div>
                    {errors.email && (
                      <p className="text-[11px] font-medium text-red-400">
                        {errors.email.message}
                      </p>
                    )}
                  </div>

                  {/* PHONE */}
                  <div className="space-y-1.5">
                    <label
                      htmlFor="phone"
                      className="cursor-pointer text-[11px] font-semibold tracking-widest text-white/60 uppercase transition-colors hover:text-white"
                    >
                      Nomor Telepon
                    </label>
                    <div className="group relative">
                      <Phone
                        size={18}
                        className={`absolute left-4 top-1/2 -translate-y-1/2 transition-colors ${errors.phone ? "text-red-400" : "text-white/30 group-focus-within:text-[#c4a661]"}`}
                      />
                      <input
                        id="phone"
                        type="text"
                        placeholder="08123456789"
                        className={`h-12 w-full rounded-xl border bg-white/[0.03] pl-11 pr-4 text-sm text-white placeholder:text-white/20 outline-none backdrop-blur-sm transition-all focus:bg-white/[0.05] focus:ring-4 ${
                          errors.phone
                            ? "border-red-500/50 focus:border-red-500 focus:ring-red-500/10"
                            : "border-white/10 focus:border-[#c4a661] focus:ring-[#c4a661]/10"
                        }`}
                        {...register("phone")}
                      />
                    </div>
                    {errors.phone && (
                      <p className="text-[11px] font-medium text-red-400">
                        {errors.phone.message}
                      </p>
                    )}
                  </div>

                  {/* PASSWORD */}
                  <div className="space-y-1.5">
                    <label
                      htmlFor="password"
                      className="cursor-pointer text-[11px] font-semibold tracking-widest text-white/60 uppercase transition-colors hover:text-white"
                    >
                      Password
                    </label>
                    <div className="group relative">
                      <LockKeyhole
                        size={18}
                        className={`absolute left-4 top-1/2 -translate-y-1/2 transition-colors ${errors.password ? "text-red-400" : "text-white/30 group-focus-within:text-[#c4a661]"}`}
                      />
                      <input
                        id="password"
                        type={showPassword ? "text" : "password"}
                        placeholder="••••••••"
                        className={`h-12 w-full rounded-xl border bg-white/[0.03] pl-11 pr-12 text-sm text-white placeholder:text-white/20 outline-none backdrop-blur-sm transition-all focus:bg-white/[0.05] focus:ring-4 ${
                          errors.password
                            ? "border-red-500/50 focus:border-red-500 focus:ring-red-500/10"
                            : "border-white/10 focus:border-[#c4a661] focus:ring-[#c4a661]/10"
                        }`}
                        {...register("password")}
                      />
                      <button
                        type="button"
                        tabIndex={-1}
                        onClick={() => setShowPassword((prev) => !prev)}
                        className="absolute right-4 top-1/2 -translate-y-1/2 text-white/30 transition-colors hover:text-white"
                      >
                        {showPassword ? (
                          <EyeOff size={18} />
                        ) : (
                          <Eye size={18} />
                        )}
                      </button>
                    </div>

                    {/* STRENGTH INDICATOR */}
                    {password && (
                      <div className="pt-2 animate-in fade-in slide-in-from-top-1">
                        <div className="flex items-center justify-between mb-1.5">
                          <p className="text-[10px] uppercase tracking-wider text-white/40">
                            Kekuatan Password
                          </p>
                          <p
                            className={`text-[10px] font-bold uppercase tracking-wider ${passwordStrength <= 1 ? "text-red-400" : passwordStrength <= 3 ? "text-yellow-400" : "text-green-400"}`}
                          >
                            {passwordLabel}
                          </p>
                        </div>
                        <div className="flex gap-1.5">
                          {[1, 2, 3, 4].map((i) => (
                            <div
                              key={i}
                              className={`h-1 flex-1 rounded-full transition-all duration-300 ${
                                passwordStrength >= i
                                  ? passwordStrength <= 1
                                    ? "bg-red-500 shadow-[0_0_8px_rgba(239,68,68,0.5)]"
                                    : passwordStrength <= 3
                                      ? "bg-yellow-500 shadow-[0_0_8px_rgba(234,179,8,0.5)]"
                                      : "bg-green-500 shadow-[0_0_8px_rgba(34,197,94,0.5)]"
                                  : "bg-white/10"
                              }`}
                            />
                          ))}
                        </div>
                      </div>
                    )}
                    {errors.password && (
                      <p className="text-[11px] font-medium text-red-400 mt-1">
                        {errors.password.message}
                      </p>
                    )}
                  </div>

                  {/* SUBMIT BUTTON */}
                  <button
                    type="submit"
                    className={`group relative mt-4 flex h-14 w-full items-center justify-center gap-2 rounded-xl text-sm font-semibold transition-all overflow-hidden shadow-[inset_0_1px_1px_rgba(255,255,255,0.2)] ${
                      isValid
                        ? "bg-[#c4a661] text-[#050810] hover:bg-[#d4b671] hover:shadow-[0_0_20px_rgba(196,166,97,0.3)]"
                        : "bg-white/10 text-white/40 cursor-not-allowed"
                    }`}
                  >
                    {mutation.isPending ? (
                      <>
                        <Loader2
                          size={18}
                          className="animate-spin text-[#050810]"
                        />
                        Memproses...
                      </>
                    ) : (
                      <>
                        Daftar Sekarang
                        <ArrowRight
                          size={18}
                          className={`transition-transform ${isValid ? "group-hover:translate-x-1" : ""}`}
                        />
                      </>
                    )}
                  </button>
                </fieldset>
              </form>

              {/* FOOTER */}
              <div className="mt-8 border-t border-white/[0.05] pt-6 text-center">
                <p className="text-[13px] text-white/40">
                  Sudah punya akun?{" "}
                  <Link
                    href="/login"
                    className="font-semibold text-[#c4a661] transition-colors hover:text-white hover:underline underline-offset-4"
                  >
                    Masuk di sini
                  </Link>
                </p>
              </div>
            </div>
          </div>
        </section>
      </div>
    </main>
  );
}
