"use client";

import { useEffect, useState } from "react";
import { useMutation, useQuery } from "@tanstack/react-query";
import { useRouter } from "next/navigation";
import * as Icons from "lucide-react";
import type { LucideIcon } from "lucide-react";

import { AdminLayout } from "@/components/admin/layout/AdminLayout";
import {
  createFacility,
  deleteFacility,
  getFacilities,
  updateFacility,
} from "@/services/admin.service";
import { getCurrentStaff } from "@/services/auth.service";
import { IconAutocomplete } from "@/utils/AutoCompleteIcon";

function getIconComponent(name: string) {
  if (!name) return null;

  return Icons[name as keyof typeof Icons] as LucideIcon | null;
}

export default function AdminMasterFacilitiesPage() {
  const router = useRouter();

  const [q, setQ] = useState("");
  const [name, setName] = useState("");
  const [icon, setIcon] = useState("");

  const [editingId, setEditingId] = useState<string | null>(null);
  const [editName, setEditName] = useState("");
  const [editIcon, setEditIcon] = useState("");

  const staffQuery = useQuery({
    queryKey: ["staff-me"],
    queryFn: getCurrentStaff,
    retry: false,
  });

  useEffect(() => {
    if (staffQuery.isError) router.replace("/admin/login");
  }, [staffQuery.isError, router]);

  const facilitiesQuery = useQuery({
    queryKey: ["admin-facilities", q],
    queryFn: () => getFacilities(q),
    enabled: !!staffQuery.data,
  });

  const createMutation = useMutation({
    mutationFn: () => createFacility({ name, icon: icon || undefined }),
    onSuccess: () => {
      setName("");
      setIcon("");
      facilitiesQuery.refetch();
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteFacility(id),
    onSuccess: () => facilitiesQuery.refetch(),
  });

  if (!staffQuery.data) return null;

  return (
    <AdminLayout role={staffQuery.data.role}>
      <h1 className="text-xl font-semibold text-[#0f172a]">
        Master Facilities
      </h1>

      <div className="mt-4 rounded-2xl border border-[#e2e8f0] bg-white p-6 shadow-sm">
        {/* FILTER & CREATE */}
        <div className="mb-6 flex flex-wrap items-center gap-3">
          <input
            value={q}
            onChange={(e) => setQ(e.target.value)}
            placeholder="Search facility"
            className="rounded-xl border border-[#e2e8f0] px-3 py-2 text-sm text-[#0f172a] focus:outline-none focus:ring-2 focus:ring-[#1a1f3c]/20"
          />

          <input
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder="Facility name"
            className="rounded-xl border border-[#e2e8f0] px-3 py-2 text-sm text-[#0f172a] focus:outline-none focus:ring-2 focus:ring-[#1a1f3c]/20"
          />

          {/* 🔥 ICON INPUT + PREVIEW */}
          <div className="flex items-center gap-2">
            <IconAutocomplete
              value={icon}
              onChange={setIcon}
              placeholder="Search icon (e.g. Wifi)"
            />
          </div>

          <button
            onClick={() => createMutation.mutate()}
            className="rounded-xl bg-[#1a1f3c] px-4 py-2 text-sm font-medium text-white transition hover:bg-[#252c52]"
          >
            Create
          </button>
        </div>

        {/* TABLE */}
        <table className="w-full text-sm">
          <thead>
            <tr className="text-left text-[#64748b]">
              <th className="py-3">Name</th>
              <th className="py-3">Icon</th>
              <th className="py-3">Action</th>
            </tr>
          </thead>

          <tbody>
            {(facilitiesQuery.data ?? []).map((facility) => {
              const TableIcon = getIconComponent(facility.icon || "");

              if (editingId === facility.id) {
                return (
                  <tr
                    key={facility.id}
                    className="border-t border-[#e2e8f0] bg-[#f8fafc]"
                  >
                    <td className="py-2">
                      <input
                        value={editName}
                        onChange={(e) => setEditName(e.target.value)}
                        className="w-full rounded-lg border border-[#e2e8f0] px-2 py-1"
                      />
                    </td>

                    <td className="py-2">
                      <div className="flex items-center gap-2 px-2">
                        <IconAutocomplete
                          value={editIcon}
                          onChange={setEditIcon}
                        />
                      </div>
                    </td>

                    <td className="py-2">
                      <button
                        onClick={() => {
                          updateFacility(facility.id, {
                            name: editName,
                            icon: editIcon || undefined,
                          }).then(() => {
                            setEditingId(null);
                            facilitiesQuery.refetch();
                          });
                        }}
                        className="mr-2 rounded-lg bg-[#1a1f3c] px-3 py-1 text-white hover:bg-[#252c52]"
                      >
                        Save
                      </button>

                      <button
                        onClick={() => setEditingId(null)}
                        className="text-[#64748b]"
                      >
                        Cancel
                      </button>
                    </td>
                  </tr>
                );
              }

              return (
                <tr
                  key={facility.id}
                  className="border-t border-[#e2e8f0] transition hover:bg-[#f8fafc]"
                >
                  <td className="py-3 text-[#0f172a]">{facility.name}</td>

                  <td className="py-3 text-[#64748b]">
                    <div className="flex items-center gap-2">
                      {TableIcon && <TableIcon size={16} />}
                      {facility.icon || "-"}
                    </div>
                  </td>

                  <td className="py-3">
                    <button
                      className="mr-3 text-[#1a1f3c] hover:underline"
                      onClick={() => {
                        setEditingId(facility.id);
                        setEditName(facility.name);
                        setEditIcon(facility.icon || "");
                      }}
                    >
                      Edit
                    </button>

                    <button
                      onClick={() => deleteMutation.mutate(facility.id)}
                      className="text-[#dc2626] hover:underline"
                    >
                      Delete
                    </button>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </AdminLayout>
  );
}
