"use client";

import { useEffect, useState } from "react";
import { useMutation, useQuery } from "@tanstack/react-query";
import { useRouter } from "next/navigation";
import { AdminLayout } from "@/components/admin/layout/AdminLayout";
import { createCity, deleteCity, getCities, updateCity } from "@/services/admin.service";
import { getCurrentStaff } from "@/services/auth.service";

export default function AdminMasterCitiesPage() {
  const router = useRouter();
  const [q, setQ] = useState("");
  const [name, setName] = useState("");
  const [editingId, setEditingId] = useState<string | null>(null);

  const staffQuery = useQuery({ queryKey: ["staff-me"], queryFn: getCurrentStaff, retry: false });
  useEffect(() => {
    if (staffQuery.isError) router.replace("/admin/login");
  }, [staffQuery.isError, router]);

  const citiesQuery = useQuery({ queryKey: ["admin-cities", q], queryFn: () => getCities(q), enabled: !!staffQuery.data });

  const createMutation = useMutation({
    mutationFn: () => createCity({ name }),
    onSuccess: () => {
      setName("");
      citiesQuery.refetch();
    },
  });

  const updateMutation = useMutation({
    mutationFn: (payload: { id: string; name: string }) => updateCity(payload.id, { name: payload.name }),
    onSuccess: () => {
      setEditingId(null);
      citiesQuery.refetch();
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteCity(id),
    onSuccess: () => citiesQuery.refetch(),
  });

  if (!staffQuery.data) return null;

  return (
    <AdminLayout role={staffQuery.data.role}>
      <h1 className="text-xl font-semibold text-slate-900">Master Cities</h1>
      <div className="mt-4 rounded-lg border bg-white p-4">
        <div className="mb-4 flex gap-2">
          <input value={q} onChange={(e) => setQ(e.target.value)} placeholder="Search city" className="rounded border px-3 py-2 text-sm" />
          <input value={name} onChange={(e) => setName(e.target.value)} placeholder="New city" className="rounded border px-3 py-2 text-sm" />
          <button onClick={() => createMutation.mutate()} className="rounded bg-[#1a1f3c] px-3 py-2 text-sm text-white">Create</button>
        </div>
        <table className="w-full text-sm">
          <thead>
            <tr className="text-left text-slate-500">
              <th className="py-2">Name</th>
              <th className="py-2">Action</th>
            </tr>
          </thead>
          <tbody>
            {(citiesQuery.data ?? []).map((city) => (
              <tr key={city.id} className="border-t">
                <td className="py-2">
                  {editingId === city.id ? (
                    <input defaultValue={city.name} onBlur={(e) => updateMutation.mutate({ id: city.id, name: e.target.value })} className="rounded border px-2 py-1" />
                  ) : (
                    city.name
                  )}
                </td>
                <td className="py-2">
                  <button onClick={() => setEditingId(city.id)} className="mr-3 text-blue-600">Edit</button>
                  <button onClick={() => deleteMutation.mutate(city.id)} className="text-red-600">Delete</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </AdminLayout>
  );
}
