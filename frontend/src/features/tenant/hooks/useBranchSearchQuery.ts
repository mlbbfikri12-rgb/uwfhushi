"use client";

import { searchBranches } from "@/services/client/public.client";
import { useQuery } from "@tanstack/react-query";

export function useBranchSearchQuery(keyword: string) {
  return useQuery({
    queryKey: ["public-branches", keyword],
    queryFn: () => searchBranches(keyword),
    enabled: keyword.trim().length >= 2,
    staleTime: 60_000,
    gcTime: 5 * 60_000,
    retry: 1,
  });
}
