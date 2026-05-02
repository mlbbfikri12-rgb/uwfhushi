"use client";

import { useQuery } from "@tanstack/react-query";
import { useMutation } from "@tanstack/react-query";
import { useRouter } from "next/navigation";
import { useEffect, useMemo, useState } from "react";

import { AdminLayout } from "@/components/admin/layout/AdminLayout";
import { RoomTable } from "@/components/admin/modules/rooms/RoomTable";

import { getCurrentStaff } from "@/services/auth.service";
import {
  createRatePlan,
  deleteRatePlan,
  getRatePlansByRoomType,
  getRoomsForBranch,
  updateRatePlan,
} from "@/services/admin.service";

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
  const [selectedRoomTypeId, setSelectedRoomTypeId] = useState("");

  // ============================
  // 📦 ROOMS QUERY
  // ============================
  const roomsQuery = useQuery({
    queryKey: ["rooms", stableBranch],
    queryFn: () => getRoomsForBranch(stableBranch),
    enabled: Boolean(staff && stableBranch),
  });

  const roomTypes = useMemo(() => {
    const map = new Map<string, { id: string; name: string }>();
    (roomsQuery.data ?? []).forEach((room) => {
      map.set(room.roomType.id, { id: room.roomType.id, name: room.roomType.name });
    });
    return Array.from(map.values());
  }, [roomsQuery.data]);

  useEffect(() => {
    if (!selectedRoomTypeId && roomTypes.length > 0) {
      setSelectedRoomTypeId(roomTypes[0].id);
    }
  }, [roomTypes, selectedRoomTypeId]);

  const ratePlansQuery = useQuery({
    queryKey: ["rate-plans", selectedRoomTypeId],
    queryFn: () => getRatePlansByRoomType(selectedRoomTypeId),
    enabled: selectedRoomTypeId.length > 0,
  });

  const createRatePlanMutation = useMutation({
    mutationFn: () =>
      createRatePlan(selectedRoomTypeId, {
        name: "New Rate Plan",
        price: 500000,
        includesBreakfast: false,
        isRefundable: false,
        paymentType: "online",
        termsConditions: "Standard terms",
        isActive: true,
      }),
    onSuccess: () => ratePlansQuery.refetch(),
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
          rooms={roomsQuery.data ?? []}
          isLoading={roomsQuery.isLoading}
        />

        <div className="rounded-xl bg-white p-5 shadow-sm">
          <div className="mb-4 flex items-center justify-between">
            <h2 className="text-lg font-semibold text-slate-900">Rate Plans</h2>
            <button onClick={() => createRatePlanMutation.mutate()} className="rounded bg-[#1a1f3c] px-3 py-2 text-sm text-white">
              Add Rate Plan
            </button>
          </div>

          <div className="mb-4">
            <select
              value={selectedRoomTypeId}
              onChange={(e) => setSelectedRoomTypeId(e.target.value)}
              className="rounded border bg-white px-3 py-2 text-sm"
            >
              {roomTypes.map((roomType) => (
                <option key={roomType.id} value={roomType.id}>
                  {roomType.name}
                </option>
              ))}
            </select>
          </div>

          <div className="space-y-2">
            {(ratePlansQuery.data ?? []).map((plan) => (
              <div key={plan.id} className="flex items-center justify-between rounded border px-3 py-2 text-sm">
                <div>
                  <p className="font-semibold">{plan.name}</p>
                  <p className="text-slate-500">Rp {plan.price.toLocaleString("id-ID")} • {plan.paymentType}</p>
                </div>
                <div className="flex gap-2">
                  <button
                    onClick={() =>
                      updateRatePlan(plan.id, {
                        name: `${plan.name} Updated`,
                        price: plan.price,
                        includesBreakfast: plan.includesBreakfast,
                        isRefundable: plan.isRefundable,
                        paymentType: plan.paymentType,
                        termsConditions: plan.termsConditions,
                        isActive: plan.isActive,
                      }).then(() => ratePlansQuery.refetch())
                    }
                    className="text-blue-600"
                  >
                    Edit
                  </button>
                  <button
                    onClick={() => deleteRatePlan(plan.id).then(() => ratePlansQuery.refetch())}
                    className="text-red-600"
                  >
                    Delete
                  </button>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </AdminLayout>
  );
}
