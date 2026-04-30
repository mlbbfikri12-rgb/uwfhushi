"use client";

import { useEffect } from "react";
import { getBranchCookie } from "@/lib/branch-cookie";
import { useBranchStore } from "@/store/useBranchStore";

export function BranchHydrator() {
  const setActiveBranch = useBranchStore((state) => state.setActiveBranch);

  useEffect(() => {
    const branch = getBranchCookie();
    if (branch) {
      setActiveBranch(branch);
    }
  }, [setActiveBranch]);

  return null;
}
