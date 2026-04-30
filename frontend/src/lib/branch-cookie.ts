import Cookies from "js-cookie";

export const BRANCH_COOKIE_KEY = "active_branch_code";

export function setBranchCookie(branch: string | null) {
  if (!branch) {
    Cookies.remove(BRANCH_COOKIE_KEY);
    return;
  }

  Cookies.set(BRANCH_COOKIE_KEY, branch.toUpperCase(), {
    sameSite: "lax",
    expires: 7,
  });
}

export function getBranchCookie(): string | null {
  const value = Cookies.get(BRANCH_COOKIE_KEY);
  return value ? value.toUpperCase() : null;
}
