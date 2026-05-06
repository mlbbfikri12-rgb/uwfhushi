"use client";

import { useEffect, useMemo, useState } from "react";
import { useQuery, useMutation } from "@tanstack/react-query";
import { useRouter } from "next/navigation";

import { AdminLayout } from "@/components/admin/layout/AdminLayout";
import { RatePlanFormModal } from "@/components/admin/modules/rate-plans/RatePlanFormModal";

import { getCurrentStaff } from "@/services/auth.service";
import { deleteRatePlan, getRoomsForBranch } from "@/services/admin.service";
import {
  getRatePlansByRoomType,
  createRatePlan,
  updateRatePlan,
} from "@/services/adminRatePlan.service";

import type {
  Staff,
  Room,
  RoomType,
  RatePlan,
  RatePlanForm,
} from "@/types/admin-rateplan";

export default function RatePlansPage() {
  const router = useRouter();

  const [selectedBranch, setSelectedBranch] = useState<string>("");
  const [selectedRoomTypeId, setSelectedRoomTypeId] = useState<string>("");

  // 🔥 MODAL STATE
  const [openModal, setOpenModal] = useState<boolean>(false);
  const [editing, setEditing] = useState<RatePlan | null>(null);

  // =========================
  // 🔐 AUTH
  // =========================
  const staffQuery = useQuery<Staff>({
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

  // =========================
  // 🏨 BRANCH
  // =========================
  const firstBranch = staff?.allowedBranches?.[0]?.code ?? "";
  const branchCode = selectedBranch || firstBranch;

  useEffect(() => {
    if (!selectedBranch && firstBranch) {
      setSelectedBranch(firstBranch);
    }
  }, [firstBranch, selectedBranch]);

  // =========================
  // 📦 ROOMS → ROOM TYPES
  // =========================
  const roomsQuery = useQuery<Room[]>({
    queryKey: ["rooms", branchCode],
    queryFn: () => getRoomsForBranch(branchCode),
    enabled: !!branchCode,
  });

  const roomTypes = useMemo<RoomType[]>(() => {
    const map = new Map<string, RoomType>();

    (roomsQuery.data ?? []).forEach((room) => {
      if (!room.roomType) return;

      map.set(room.roomType.id, {
        id: room.roomType.id,
        name: room.roomType.name,
      });
    });

    return Array.from(map.values());
  }, [roomsQuery.data]);

  useEffect(() => {
    if (!selectedRoomTypeId && roomTypes.length > 0) {
      setSelectedRoomTypeId(roomTypes[0].id);
    }
  }, [roomTypes, selectedRoomTypeId]);

  // =========================
  // 📦 RATE PLANS
  // =========================
  const ratePlansQuery = useQuery<RatePlan[]>({
    queryKey: ["rate-plans", selectedRoomTypeId],
    queryFn: () => getRatePlansByRoomType(selectedRoomTypeId, branchCode),
    enabled: !!selectedRoomTypeId,
  });

  // =========================
  // 🚀 MUTATIONS
  // =========================
  const createMutation = useMutation({
    mutationFn: (data: RatePlanForm) =>
      createRatePlan(selectedRoomTypeId, branchCode, data),
    onSuccess: () => ratePlansQuery.refetch(),
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, plan }: { id: string; plan: RatePlanForm }) =>
      updateRatePlan(id, branchCode, plan),
    onSuccess: () => ratePlansQuery.refetch(),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteRatePlan(id),
    onSuccess: () => ratePlansQuery.refetch(),
  });

  // =========================
  // ⛔ LOADING
  // =========================
  if (staffQuery.isLoading) {
    return <div className="p-6">Loading...</div>;
  }

  if (!staff) return null;

  // =========================
  // 🎨 UI
  // =========================
  return (
    <AdminLayout role={staff.role}>
      <div className="space-y-6">
        {/* HEADER */}
        <div className="flex justify-between items-center">
          <h1 className="text-2xl font-bold">Rate Plans</h1>

          <button
            onClick={() => {
              setEditing(null);
              setOpenModal(true);
            }}
            className="bg-black text-white px-4 py-2 rounded text-sm"
          >
            + Add Rate Plan
          </button>
        </div>

        {/* BRANCH SELECT */}
        <select
          value={branchCode}
          onChange={(e) => setSelectedBranch(e.target.value)}
          className="border px-3 py-2 rounded"
        >
          {staff.allowedBranches.map((b) => (
            <option key={b.id} value={b.code}>
              {b.code} - {b.name}
            </option>
          ))}
        </select>

        {/* ROOM TYPE SELECT */}
        <select
          value={selectedRoomTypeId}
          onChange={(e) => setSelectedRoomTypeId(e.target.value)}
          className="border px-3 py-2 rounded"
        >
          {roomTypes.map((rt) => (
            <option key={rt.id} value={rt.id}>
              {rt.name}
            </option>
          ))}
        </select>

        {/* LIST */}
        <div className="bg-white rounded shadow divide-y">
          {(ratePlansQuery.data ?? []).map((plan) => (
            <div
              key={plan.id}
              className="flex justify-between items-center p-4"
            >
              <div>
                <p className="font-semibold">{plan.name}</p>
                <p className="text-sm text-gray-500">
                  Rp {plan.price.toLocaleString("id-ID")} • {plan.paymentType}
                </p>
              </div>

              <div className="flex gap-3">
                <button
                  onClick={() => {
                    setEditing(plan);
                    setOpenModal(true);
                  }}
                  className="text-blue-600 text-sm"
                >
                  Edit
                </button>

                <button
                  onClick={() => deleteMutation.mutate(plan.id)}
                  className="text-red-600 text-sm"
                >
                  Delete
                </button>
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* MODAL */}
      <RatePlanFormModal
        open={openModal}
        onClose={() => setOpenModal(false)}
        initialData={editing}
        onSubmit={(data: RatePlanForm) => {
          if (editing) {
            updateMutation.mutate({
              id: editing.id,
              plan: data,
            });
          } else {
            createMutation.mutate(data);
          }
        }}
      />
    </AdminLayout>
  );
}
