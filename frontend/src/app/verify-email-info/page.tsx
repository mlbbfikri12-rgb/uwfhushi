import { Suspense } from "react";

import VerifyEmailInfoClient from "./VerifyEmailInfoClient";

export default function Page() {
  return (
    <Suspense
      fallback={
        <main className="min-h-screen flex items-center justify-center bg-slate-100 px-4">
          <div className="max-w-md w-full bg-white p-6 rounded-xl shadow space-y-5 text-center">
            <div className="text-4xl">📧</div>

            <h1 className="text-xl font-semibold text-slate-900">Loading...</h1>

            <p className="text-sm text-slate-500">Mohon tunggu sebentar</p>
          </div>
        </main>
      }
    >
      <VerifyEmailInfoClient />
    </Suspense>
  );
}
