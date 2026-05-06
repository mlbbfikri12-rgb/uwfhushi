"use client";

import { useSearchParams, useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import axios from "axios";

type Status = "loading" | "success" | "error";

export default function VerifyEmailPage() {
  const params = useSearchParams();
  const router = useRouter();

  const token = params.get("token");

  const [status, setStatus] = useState<Status>("loading");
  const [message, setMessage] = useState("");

  useEffect(() => {
    if (!token) {
      setStatus("error");
      setMessage("Token tidak ditemukan");
      return;
    }

    const verify = async () => {
      try {
        await axios.post(`http://localhost:5000/auth/verify?token=${token}`);
        setStatus("success");

        // auto redirect
        setTimeout(() => {
          router.push("/login");
        }, 2000);
      } catch (err: any) {
        setStatus("error");
        setMessage(
          err?.response?.data?.message ||
            "Link verifikasi tidak valid atau sudah expired",
        );
      }
    };

    verify();
  }, [token, router]);

  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-50">
      <div className="w-full max-w-md rounded-xl border bg-white p-6 text-center shadow-sm">
        {status === "loading" && (
          <>
            <h2 className="text-lg font-semibold text-slate-800">
              Verifying your email...
            </h2>
            <p className="mt-2 text-sm text-slate-500">Mohon tunggu sebentar</p>
          </>
        )}

        {status === "success" && (
          <>
            <h2 className="text-lg font-semibold text-green-600">
              Email verified 🎉
            </h2>
            <p className="mt-2 text-sm text-slate-500">
              Kamu akan diarahkan ke login...
            </p>

            <button
              onClick={() => router.push("/login")}
              className="mt-4 rounded-lg bg-[#1a1f3c] px-4 py-2 text-sm font-semibold text-white"
            >
              Go to Login
            </button>
          </>
        )}

        {status === "error" && (
          <>
            <h2 className="text-lg font-semibold text-red-600">
              Verification Failed
            </h2>
            <p className="mt-2 text-sm text-slate-500">{message}</p>

            <button
              onClick={() => router.push("/resend-verification")}
              className="mt-4 rounded-lg bg-[#1a1f3c] px-4 py-2 text-sm font-semibold text-white"
            >
              Resend Verification
            </button>
          </>
        )}
      </div>
    </div>
  );
}
