"use client";

import { useSearchParams, useRouter } from "next/navigation";
import { useState, useEffect } from "react";
import { useMutation } from "@tanstack/react-query";
import axios from "axios";

export default function VerifyEmailInfoPage() {
  const params = useSearchParams();
  const router = useRouter();

  const email = params.get("email");

  const [cooldown, setCooldown] = useState(60);
  const [canResend, setCanResend] = useState(false);

  // =========================
  // ⏱️ COOLDOWN TIMER
  // =========================
  useEffect(() => {
    if (cooldown <= 0) {
      setCanResend(true);
      return;
    }

    const timer = setTimeout(() => {
      setCooldown((prev) => prev - 1);
    }, 1000);

    return () => clearTimeout(timer);
  }, [cooldown]);

  // =========================
  // 🔁 RESEND
  // =========================
  const resendMutation = useMutation({
    mutationFn: async () => {
      if (!email) throw new Error("Email tidak ditemukan");
      return axios.post("/api/auth/resend-verification", { email });
    },
    onSuccess: () => {
      setCooldown(60);
      setCanResend(false);
    },
  });

  // =========================
  // 📬 OPEN GMAIL (UX BOOST)
  // =========================
  const openGmail = () => {
    window.open("https://mail.google.com", "_blank");
  };

  // =========================
  // 🧱 EDGE CASE
  // =========================
  if (!email) {
    return (
      <div className="flex h-screen items-center justify-center">
        <div className="text-center space-y-3">
          <h2 className="text-lg font-semibold">Email tidak ditemukan</h2>
          <button
            onClick={() => router.push("/register")}
            className="text-sm text-blue-600 underline"
          >
            Kembali ke Register
          </button>
        </div>
      </div>
    );
  }

  // =========================
  // 🎨 UI
  // =========================
  return (
    <main className="min-h-screen flex items-center justify-center bg-slate-100 px-4">
      <div className="max-w-md w-full bg-white p-6 rounded-xl shadow space-y-5 text-center">
        {/* ICON */}
        <div className="text-4xl">📧</div>

        {/* TITLE */}
        <h1 className="text-xl font-semibold text-slate-900">
          Verify your email
        </h1>

        {/* DESCRIPTION */}
        <p className="text-sm text-slate-600">
          Kami telah mengirim link verifikasi ke:
        </p>

        <p className="text-sm font-semibold text-slate-900">{email}</p>

        <p className="text-xs text-slate-500">
          Klik link di email untuk mengaktifkan akun kamu.
        </p>

        {/* ACTIONS */}
        <div className="space-y-3 pt-2">
          <button
            onClick={openGmail}
            className="w-full bg-[#1a1f3c] text-white py-2 rounded-lg text-sm font-semibold hover:bg-[#252c52]"
          >
            Buka Email
          </button>

          <button
            onClick={() => resendMutation.mutate()}
            disabled={!canResend || resendMutation.isPending}
            className="w-full border py-2 rounded-lg text-sm font-semibold disabled:opacity-50"
          >
            {resendMutation.isPending
              ? "Mengirim..."
              : canResend
                ? "Kirim Ulang Email"
                : `Kirim ulang dalam ${cooldown}s`}
          </button>
        </div>

        {/* FOOTER */}
        <div className="text-xs text-slate-500 pt-3">
          Tidak menerima email? Cek folder spam atau kirim ulang.
        </div>
      </div>
    </main>
  );
}
