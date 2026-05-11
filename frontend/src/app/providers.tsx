"use client";

import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { PropsWithChildren, useState } from "react";
import { Toaster } from "sonner";
import { BranchHydrator } from "@/features/tenant/components/BranchHydrator";

export function Providers({ children }: PropsWithChildren) {
  const [queryClient] = useState(
    () =>
      new QueryClient({
        defaultOptions: {
          queries: {
            refetchOnWindowFocus: false,
            staleTime: 1000 * 60 * 2,
            gcTime: 1000 * 60 * 15,
            retry: 1,
          },
        },
      })
  );

  return (
    <QueryClientProvider client={queryClient}>
      <BranchHydrator />
      {children}
      <Toaster richColors position="top-right" />
    </QueryClientProvider>
  );
}
