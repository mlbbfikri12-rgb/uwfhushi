"use client";

import { useState, useEffect } from "react";
import axios from "axios";

export default function ResendVerificationPage() {
  const [email, setEmail] = useState("");
  const [status, setStatus] = useState<
    "idle" | "loading" | "success" | "error"
  >("idle");

  const [cooldown, setCooldown] = useState(0);
  const [error, setError] = useState("");

  // countdown timer
  useEffect(() => {
    if (cooldown <= 0) return;

    const timer = setInterval(() => {
      setCooldown((prev) => prev - 1);
    }, 1000);

    return () => clearInterval(timer);
  }, [cooldown]);

  const handleSubmit = async () => {
    if (!email.trim()) {
      setError("Email wajib diisi");
      return;
    }

    try {
      setStatus("loading");
      setError("");

      await axios.post("/api/auth/resend-verification", { email });

      setStatus("success");
      setCooldown(60); // 🔥 anti spam
    } catch (err: any) {
      setStatus("error");
      setError(
        err?.response?.data?.message || "Gagal mengirim email verifikasi",
      );
    }
  };

  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-50">
      <div className="w-full max-w-md rounded-xl border bg-white p-6 shadow-sm">
        <h2 className="text-lg font-semibold text-slate-800 text-center">
          Resend Verification Email
        </h2>

        <div className="mt-4 space-y-3">
          {status === "success" ? (
            <p className="text-sm text-green-600 text-center">
              Email berhasil dikirim. Silakan cek inbox kamu.
            </p>
          ) : (
            <>
              <input
                type="email"
                placeholder="Masukkan email kamu"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                className="w-full rounded-lg border px-3 py-2 text-sm"
              />

              {error && <p className="text-xs text-red-500">{error}</p>}

              <button
                onClick={handleSubmit}
                disabled={status === "loading" || cooldown > 0}
                className="w-full rounded-lg bg-[#1a1f3c] px-4 py-2 text-sm font-semibold text-white disabled:opacity-50"
              >
                {status === "loading"
                  ? "Sending..."
                  : cooldown > 0
                    ? `Retry in ${cooldown}s`
                    : "Send Verification Email"}
              </button>
            </>
          )}
        </div>
      </div>
    </div>
  );
}
