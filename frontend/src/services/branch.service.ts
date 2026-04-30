import { publicApi } from "@/lib/public-api";
import type { PublicBranch } from "@/types/branch";

export async function searchBranches(keyword: string) {
  const { data } = await publicApi.get<PublicBranch[]>("/api/public/branches", {
    params: {
      q: keyword,
      limit: 10,
    },
  });
  return data;
}
