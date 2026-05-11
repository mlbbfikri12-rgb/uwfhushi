import { Suspense } from "react";
import RegisterClient from "./registerClient";
import { Sparkles } from "lucide-react";

function LoadingFallback() {
  return (
    <main className="relative flex min-h-screen items-center justify-center overflow-hidden bg-[#050810] px-6 lg:justify-end lg:px-8">
      {/* AMBIENT LIGHTING MATCHING MAIN PAGE */}
      <div className="absolute inset-0 flex items-center justify-center">
        <div className="h-[400px] w-[400px] animate-pulse rounded-full bg-[#c4a661]/5 blur-[100px]" />
      </div>

      {/* SKELETON CARD (Match standard width of right column) */}
      <div className="relative w-full max-w-[540px] overflow-hidden rounded-[2.5rem] border border-white/[0.05] bg-[#0a0d17]/50 shadow-2xl backdrop-blur-sm lg:mr-[10%]">
        {/* IMAGE HEADER SKELETON */}
        <div className="relative h-32 w-full bg-white/5 animate-pulse sm:h-40">
          <div className="absolute bottom-6 left-8 sm:bottom-8 sm:left-10 space-y-2">
            <div className="h-3 w-32 rounded bg-white/10" />
            <div className="h-6 w-48 rounded bg-white/20" />
          </div>
        </div>

        {/* FORM FIELDS SKELETON */}
        <div className="space-y-5 px-8 pb-10 pt-8 sm:px-10 sm:pb-12">
          {/* Input 1: Name */}
          <div className="space-y-2">
            <div className="h-3 w-20 animate-pulse rounded bg-white/10" />
            <div className="h-12 w-full animate-pulse rounded-xl bg-white/[0.03] border border-white/5" />
          </div>

          {/* Input 2: Email */}
          <div className="space-y-2">
            <div className="h-3 w-24 animate-pulse rounded bg-white/10" />
            <div className="h-12 w-full animate-pulse rounded-xl bg-white/[0.03] border border-white/5" />
          </div>

          {/* Input 3: Phone */}
          <div className="space-y-2">
            <div className="h-3 w-28 animate-pulse rounded bg-white/10" />
            <div className="h-12 w-full animate-pulse rounded-xl bg-white/[0.03] border border-white/5" />
          </div>

          {/* Input 4: Password */}
          <div className="space-y-2">
            <div className="h-3 w-20 animate-pulse rounded bg-white/10" />
            <div className="h-12 w-full animate-pulse rounded-xl bg-white/[0.03] border border-white/5" />
          </div>

          {/* Button */}
          <div className="mt-6 h-14 w-full animate-pulse rounded-xl bg-[#c4a661]/20 border border-[#c4a661]/10 flex items-center justify-center gap-2">
            <Sparkles className="text-[#c4a661]/50" size={18} />
          </div>
        </div>
      </div>
    </main>
  );
}

export default function Page() {
  return (
    <Suspense fallback={<LoadingFallback />}>
      <RegisterClient />
    </Suspense>
  );
}
