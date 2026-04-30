"use client";

import { useQuery } from "@tanstack/react-query";
import { useRouter } from "next/navigation";
import { useEffect, useMemo, useState } from "react";

import { AdminLayout } from "@/components/admin/layout/AdminLayout";
import { RoomTable } from "@/components/admin/modules/rooms/RoomTable";

import { getCurrentStaff } from "@/services/auth.service";
import { getRoomsForBranch } from "@/services/admin.service";

export default function RoomsPage() {
  const router = useRouter();

  const [selectedBranch, setSelectedBranch] = useState("");

  // ============================
  // 🔐 AUTH CHECK
  // ============================
  const staffQuery = useQuery({
    queryKey: ["staff-me"],
    queryFn: getCurrentStaff,
    retry: false,
  });

  const staff = staffQuery.data;

  useEffect(() => {
    if (staffQuery.isError) {
      router.replace("/admin/login");
    }
  }, [staffQuery.isError, router]);

  // ============================
  // 🏨 BRANCH LOGIC
  // ============================
  const firstBranch = staff?.allowedBranches?.[0]?.code ?? "";
  const branchCode = selectedBranch || firstBranch;

  useEffect(() => {
    if (!selectedBranch && firstBranch) {
      setSelectedBranch(firstBranch);
    }
  }, [firstBranch, selectedBranch]);

  // ============================
  // 🔥 STABILKAN QUERY (ANTI REFETCH)
  // ============================
  const stableBranch = useMemo(() => branchCode, [branchCode]);

  // ============================
  // 📦 ROOMS QUERY
  // ============================
  const roomsQuery = useQuery({
    queryKey: ["rooms", stableBranch],
    queryFn: () => getRoomsForBranch(stableBranch),
    enabled: Boolean(staff && stableBranch),
  });

  // ============================
  // ⛔ LOADING STATE
  // ============================
  if (staffQuery.isLoading) {
    return <div className="p-6">Loading session...</div>;
  }

  if (!staff) return null;

  return (
    <AdminLayout role={staff.role}>
      <div className="space-y-6">
        {/* HEADER */}
        <div className="flex flex-wrap items-end justify-between gap-3">
          <div>
            <h1 className="text-2xl font-bold text-slate-900">
              Room Management
            </h1>
            <p className="text-sm text-slate-500">
              Manage room availability & status
            </p>
          </div>

          {/* 🔥 BRANCH SELECT */}
          {staff.role !== "SUPER_ADMIN" && (
            <select
              value={branchCode}
              onChange={(e) => setSelectedBranch(e.target.value)}
              className="rounded border bg-white px-3 py-2 text-sm"
            >
              {staff.allowedBranches.map((branch) => (
                <option key={branch.id} value={branch.code}>
                  {branch.code} - {branch.name}
                </option>
              ))}
            </select>
          )}
        </div>

        {/* TABLE */}
        <RoomTable
          rooms={(roomsQuery.data ?? []) as any}
          isLoading={roomsQuery.isLoading}
        />
      </div>
    </AdminLayout>
  );
}
