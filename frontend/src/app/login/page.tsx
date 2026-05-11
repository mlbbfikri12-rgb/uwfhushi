import { Suspense } from "react";
import { Sparkles } from "lucide-react";
import LoginClient from "./loginClient";

function LoginFallback() {
  return (
    <main className="relative flex min-h-screen items-center justify-center overflow-hidden bg-[#050810] px-6">
      {/* AMBIENT LIGHTING MATCHING THE MAIN PAGE */}
      <div className="absolute inset-0 flex items-center justify-center">
        <div className="h-[400px] w-[400px] animate-pulse rounded-full bg-[#c4a661]/5 blur-[100px]" />
      </div>

      {/* SKELETON CARD */}
      <div className="relative w-full max-w-md overflow-hidden rounded-[2.5rem] border border-white/[0.05] bg-[#0a0d17]/50 p-8 shadow-2xl backdrop-blur-sm sm:p-12">
        {/* LOGO SKELETON */}
        <div className="mb-12 flex justify-center">
          <div className="flex items-center gap-2 opacity-50">
            <Sparkles className="text-[#c4a661] animate-pulse" size={24} />
            <div className="h-6 w-24 animate-pulse rounded-md bg-white/10" />
          </div>
        </div>

        {/* TITLE SKELETON */}
        <div className="mb-10 space-y-4">
          <div className="h-8 w-32 animate-pulse rounded-lg bg-white/10" />
          <div className="h-4 w-48 animate-pulse rounded-md bg-white/5" />
        </div>

        {/* FORM SKELETON */}
        <div className="space-y-6">
          {/* Input 1 */}
          <div className="space-y-3">
            <div className="h-3 w-24 animate-pulse rounded bg-white/10" />
            <div className="h-14 w-full animate-pulse rounded-xl bg-white/[0.03] border border-white/5" />
          </div>

          {/* Input 2 */}
          <div className="space-y-3">
            <div className="flex justify-between">
              <div className="h-3 w-20 animate-pulse rounded bg-white/10" />
              <div className="h-3 w-24 animate-pulse rounded bg-white/5" />
            </div>
            <div className="h-14 w-full animate-pulse rounded-xl bg-white/[0.03] border border-white/5" />
          </div>

          {/* Remember Me */}
          <div className="flex items-center gap-3 pt-2">
            <div className="h-4 w-4 animate-pulse rounded-[4px] bg-white/10" />
            <div className="h-3 w-24 animate-pulse rounded bg-white/5" />
          </div>

          {/* Button */}
          <div className="mt-4 h-14 w-full animate-pulse rounded-xl bg-[#c4a661]/20 border border-[#c4a661]/10" />
        </div>
      </div>
    </main>
  );
}

export default function Page() {
  return (
    <Suspense fallback={<LoginFallback />}>
      <LoginClient />
    </Suspense>
  );
}
