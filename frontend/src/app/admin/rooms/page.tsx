"use client";

import { useQuery } from "@tanstack/react-query";
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
import { AdminRoom } from "@/types/admin-room";

import type { Staff, RatePlanAdminRow } from "@/types/admin-rateplan";

// 🔥 FORM TYPE
type RatePlanFormState = {
  name: string;
  price: number;
  includesBreakfast: boolean;
  isRefundable: boolean;
  paymentType: "online" | "pay_at_hotel";
  termsConditions: string;
  isActive: boolean;
};

export default function RoomsPage() {
  const router = useRouter();

  const [selectedBranch, setSelectedBranch] = useState<string>("");
  const [selectedRoomTypeId, setSelectedRoomTypeId] = useState<string>("");

  // 🔥 MODAL STATE
  const [isModalOpen, setIsModalOpen] = useState<boolean>(false);
  const [editingPlan, setEditingPlan] = useState<RatePlanAdminRow | null>(null);

  const [form, setForm] = useState<RatePlanFormState>({
    name: "",
    price: 0,
    includesBreakfast: false,
    isRefundable: false,
    paymentType: "online",
    termsConditions: "",
    isActive: true,
  });

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

  const stableBranch = useMemo(() => branchCode, [branchCode]);

  // =========================
  // 📦 ROOMS
  // =========================
  const roomsQuery = useQuery<AdminRoom[]>({
    queryKey: ["rooms", stableBranch],
    queryFn: () => getRoomsForBranch(stableBranch),
    enabled: Boolean(staff && stableBranch),
  });

  const roomTypes = useMemo(() => {
    const map = new Map<string, { id: string; name: string }>();

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
  // 📦 RATE PLAN
  // =========================
  const ratePlansQuery = useQuery<RatePlanAdminRow[]>({
    queryKey: ["rate-plans", selectedRoomTypeId],
    queryFn: () => getRatePlansByRoomType(selectedRoomTypeId),
    enabled: selectedRoomTypeId.length > 0,
  });

  // =========================
  // 🔥 ACTIONS
  // =========================
  const openCreate = () => {
    setEditingPlan(null);
    setForm({
      name: "",
      price: 0,
      includesBreakfast: false,
      isRefundable: false,
      paymentType: "online",
      termsConditions: "",
      isActive: true,
    });
    setIsModalOpen(true);
  };

  const openEdit = (plan: RatePlanAdminRow) => {
    setEditingPlan(plan);

    setForm({
      name: plan.name,
      price: plan.price,
      includesBreakfast: plan.includesBreakfast ?? false,
      isRefundable: plan.isRefundable ?? false,
      paymentType: plan.paymentType,
      termsConditions: plan.termsConditions ?? "",
      isActive: plan.isActive ?? true,
    });

    setIsModalOpen(true);
  };

  const handleSubmit = async () => {
    if (!form.name || form.price <= 0) {
      alert("Invalid form");
      return;
    }

    if (editingPlan) {
      await updateRatePlan(editingPlan.id, form);
    } else {
      await createRatePlan(selectedRoomTypeId, form);
    }

    setIsModalOpen(false);
    ratePlansQuery.refetch();
  };

  const handleDelete = async (id: string) => {
    if (confirm("Delete this rate plan?")) {
      await deleteRatePlan(id);
      ratePlansQuery.refetch();
    }
  };

  // =========================
  // ⛔ LOADING
  // =========================
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
            <p className="text-sm text-slate-500">Manage room & rate plans</p>
          </div>

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
        </div>

        {/* ROOM TABLE */}
        <RoomTable
          rooms={roomsQuery.data ?? []}
          isLoading={roomsQuery.isLoading}
        />

        {/* RATE PLAN */}
        <div className="rounded-xl bg-white p-5 shadow-sm">
          <div className="mb-4 flex items-center justify-between">
            <h2 className="text-lg font-semibold text-slate-900">Rate Plans</h2>
            <button
              onClick={openCreate}
              className="rounded bg-[#1a1f3c] px-3 py-2 text-sm text-white"
            >
              Add Rate Plan
            </button>
          </div>

          <div className="mb-4">
            <select
              value={selectedRoomTypeId}
              onChange={(e) => setSelectedRoomTypeId(e.target.value)}
              className="rounded border bg-white px-3 py-2 text-sm"
            >
              {roomTypes.map((rt) => (
                <option key={rt.id} value={rt.id}>
                  {rt.name}
                </option>
              ))}
            </select>
          </div>

          <div className="space-y-2">
            {(ratePlansQuery.data ?? []).map((plan) => (
              <div
                key={plan.id}
                className="flex items-center justify-between rounded border px-3 py-2 text-sm"
              >
                <div>
                  <p className="font-semibold">{plan.name}</p>
                  <p className="text-slate-500">
                    Rp {plan.price.toLocaleString("id-ID")} • {plan.paymentType}
                  </p>
                </div>

                <div className="flex gap-2">
                  <button
                    onClick={() => openEdit(plan)}
                    className="text-blue-600"
                  >
                    Edit
                  </button>
                  <button
                    onClick={() => handleDelete(plan.id)}
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

      {/* MODAL */}
      {isModalOpen && (
        <div className="fixed inset-0 flex items-center justify-center bg-black/40">
          <div className="w-[400px] rounded bg-white p-4 space-y-3">
            <h3 className="text-lg font-semibold">
              {editingPlan ? "Edit Rate Plan" : "Create Rate Plan"}
            </h3>

            <input
              value={form.name}
              onChange={(e) => setForm({ ...form, name: e.target.value })}
              className="w-full border p-2"
            />

            <input
              type="number"
              value={form.price}
              onChange={(e) =>
                setForm({ ...form, price: Number(e.target.value) })
              }
              className="w-full border p-2"
            />

            <select
              value={form.paymentType}
              onChange={(e) =>
                setForm({
                  ...form,
                  paymentType: e.target.value as "online" | "pay_at_hotel",
                })
              }
              className="w-full border p-2"
            >
              <option value="online">Online</option>
              <option value="pay_at_hotel">Pay at Hotel</option>
            </select>

            <label className="flex gap-2">
              <input
                type="checkbox"
                checked={form.isRefundable}
                onChange={(e) =>
                  setForm({ ...form, isRefundable: e.target.checked })
                }
              />
              Refundable
            </label>

            <label className="flex gap-2">
              <input
                type="checkbox"
                checked={form.includesBreakfast}
                onChange={(e) =>
                  setForm({
                    ...form,
                    includesBreakfast: e.target.checked,
                  })
                }
              />
              Breakfast
            </label>

            <textarea
              value={form.termsConditions}
              onChange={(e) =>
                setForm({
                  ...form,
                  termsConditions: e.target.value,
                })
              }
              className="w-full border p-2"
            />

            <div className="flex justify-end gap-2">
              <button onClick={() => setIsModalOpen(false)}>Cancel</button>
              <button
                onClick={handleSubmit}
                className="bg-[#1a1f3c] text-white px-3 py-1 rounded"
              >
                Save
              </button>
            </div>
          </div>
        </div>
      )}
    </AdminLayout>
  );
}
