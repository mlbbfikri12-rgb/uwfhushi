"use client";

import { useEffect, useState } from "react";
import { useMutation, useQuery } from "@tanstack/react-query";
import { useRouter } from "next/navigation";
import { AdminLayout } from "@/components/admin/layout/AdminLayout";
import { createBrand, deleteBrand, getBrands, updateBrand, uploadImage } from "@/services/admin.service";
import { getCurrentStaff } from "@/services/auth.service";

export default function AdminMasterBrandsPage() {
  const router = useRouter();
  const [q, setQ] = useState("");
  const [name, setName] = useState("");
  const [logoUrl, setLogoUrl] = useState("");

  const staffQuery = useQuery({ queryKey: ["staff-me"], queryFn: getCurrentStaff, retry: false });
  useEffect(() => {
    if (staffQuery.isError) router.replace("/admin/login");
  }, [staffQuery.isError, router]);

  const brandsQuery = useQuery({ queryKey: ["admin-brands", q], queryFn: () => getBrands(q), enabled: !!staffQuery.data });

  const createMutation = useMutation({
    mutationFn: () => createBrand({ name, logoUrl: logoUrl || undefined }),
    onSuccess: () => {
      setName("");
      setLogoUrl("");
      brandsQuery.refetch();
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteBrand(id),
    onSuccess: () => brandsQuery.refetch(),
  });

  if (!staffQuery.data) return null;

  return (
    <AdminLayout role={staffQuery.data.role}>
      <h1 className="text-xl font-semibold text-slate-900">Master Brands</h1>
      <div className="mt-4 rounded-lg border bg-white p-4">
        <div className="mb-4 flex flex-wrap gap-2">
          <input value={q} onChange={(e) => setQ(e.target.value)} placeholder="Search brand" className="rounded border px-3 py-2 text-sm" />
          <input value={name} onChange={(e) => setName(e.target.value)} placeholder="Brand name" className="rounded border px-3 py-2 text-sm" />
          <input value={logoUrl} onChange={(e) => setLogoUrl(e.target.value)} placeholder="Logo URL" className="rounded border px-3 py-2 text-sm" />
          <label className="rounded border px-3 py-2 text-sm">
            Upload Logo
            <input
              type="file"
              className="hidden"
              onChange={async (e) => {
                const file = e.target.files?.[0];
                if (!file) return;
                const uploaded = await uploadImage(file, "brands");
                setLogoUrl(uploaded.url);
              }}
            />
          </label>
          <button onClick={() => createMutation.mutate()} className="rounded bg-[#1a1f3c] px-3 py-2 text-sm text-white">Create</button>
        </div>

        <table className="w-full text-sm">
          <thead>
            <tr className="text-left text-slate-500">
              <th className="py-2">Name</th>
              <th className="py-2">Logo</th>
              <th className="py-2">Action</th>
            </tr>
          </thead>
          <tbody>
            {(brandsQuery.data ?? []).map((brand) => (
              <tr key={brand.id} className="border-t">
                <td className="py-2">{brand.name}</td>
                <td className="py-2">{brand.logoUrl ? <a href={brand.logoUrl} target="_blank" className="text-blue-600">View</a> : "-"}</td>
                <td className="py-2">
                  <button
                    className="mr-3 text-blue-600"
                    onClick={() => {
                      const nextName = window.prompt("Brand name", brand.name);
                      if (!nextName) return;
                      updateBrand(brand.id, { name: nextName, logoUrl: brand.logoUrl ?? undefined }).then(() => brandsQuery.refetch());
                    }}
                  >
                    Edit
                  </button>
                  <button onClick={() => deleteMutation.mutate(brand.id)} className="text-red-600">Delete</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </AdminLayout>
  );
}
