import { create } from "zustand";

interface BranchState {
  activeBranch: string | null;
  setActiveBranch: (code: string | null) => void;
}

export const useBranchStore = create<BranchState>((set) => ({
  activeBranch: null,
  setActiveBranch: (code) =>
    set({ activeBranch: code ? code.toUpperCase() : null }),
}));
