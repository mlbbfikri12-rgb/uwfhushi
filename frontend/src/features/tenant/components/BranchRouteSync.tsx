"use client";

import { useEffect } from "react";
import { setBranchCookie } from "@/lib/branch-cookie";
import { useBranchStore } from "@/store/useBranchStore";

type Props = {
  branch: string;
};

export function BranchRouteSync({ branch }: Props) {
  const normalized = branch.toUpperCase();
  const activeBranch = useBranchStore((state) => state.activeBranch);
  const setActiveBranch = useBranchStore((state) => state.setActiveBranch);

  useEffect(() => {
    if (activeBranch !== normalized) {
      setActiveBranch(normalized);
    }
    setBranchCookie(normalized);
  }, [activeBranch, normalized, setActiveBranch]);

  return null;
}
