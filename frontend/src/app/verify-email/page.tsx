import { Suspense } from "react";

import VerifyEmailClient from "./VerifyEmailClient";

export default function Page() {
  return (
    <Suspense
      fallback={
        <div className="flex min-h-screen items-center justify-center bg-slate-50">
          <div className="w-full max-w-md rounded-xl border bg-white p-6 text-center shadow-sm">
            <h2 className="text-lg font-semibold text-slate-800">Loading...</h2>

            <p className="mt-2 text-sm text-slate-500">Mohon tunggu sebentar</p>
          </div>
        </div>
      }
    >
      <VerifyEmailClient />
    </Suspense>
  );
}
