import { adminApi } from "@/lib/admin-api";
import { useBranchStore } from "@/store/useBranchStore";
import type { PublicBranch } from "@/types/branch";
import type { Room } from "@/types/room";

export type StaffRow = {
  id: string;
  name: string;
  email: string;
  role: "SUPER_ADMIN" | "SPV" | "FO";
  isActive: boolean;
};

export type BannerRow = {
  id: string;
  title: string;
  subtitle: string;
  imageUrl: string;
  linkUrl: string;
  sortOrder: number;
};

export async function getBranches() {
  const { data } = await adminApi.get<PublicBranch[]>("/api/branches");
  return data;
}

export async function getStaffRows() {
  const { data } = await adminApi.get<StaffRow[]>("/api/staff");
  return data;
}

export async function getActiveBanners() {
  const { data } = await adminApi.get<BannerRow[]>("/api/banners/active");
  return data;
}

export async function getRoomsForBranch(branchCode: string) {
  const { data } = await adminApi.get<Room[]>("/api/rooms", {
    headers: {
      "X-Branch-Code": branchCode,
    },
  });
  return data;
}

export async function updateRoomStatus(
  roomId: string,
  status: string
) {
  const branch = useBranchStore.getState().activeBranch;
  const { data } = await adminApi.patch(
    `/api/rooms/${roomId}/status`,
    { status },
    {
      headers: {
        "X-Branch-Code": branch || "",
      },
    }
  );

  return data;
}