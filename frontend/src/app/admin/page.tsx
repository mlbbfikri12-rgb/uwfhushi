"use client";

import { useQuery } from "@tanstack/react-query";
import { useRouter } from "next/navigation";
import { useEffect } from "react";

import { AdminLayout } from "@/components/admin/layout/AdminLayout";
import { SummaryCard } from "@/components/admin/ui/SummaryCard";

import { getCurrentStaff } from "@/services/auth.service";
import {
  getActiveBanners,
  getBranches,
  getStaffRows,
} from "@/services/admin.service";

export default function AdminPage() {
  const router = useRouter();

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

  const branchesQuery = useQuery({
    queryKey: ["branches"],
    queryFn: getBranches,
    enabled: staff?.role === "SUPER_ADMIN",
  });

  const staffRowsQuery = useQuery({
    queryKey: ["staff"],
    queryFn: getStaffRows,
    enabled: staff?.role === "SUPER_ADMIN",
  });

  const bannersQuery = useQuery({
    queryKey: ["banners"],
    queryFn: getActiveBanners,
    enabled: staff?.role === "SUPER_ADMIN",
  });

  if (staffQuery.isLoading) {
    return <div className="p-6">Loading session...</div>;
  }

  if (!staff) return null;

  return (
    <AdminLayout role={staff.role}>
      <div className="space-y-6">
        <div>
          <h1 className="text-2xl font-bold text-slate-900">Dashboard</h1>
          <p className="text-sm text-slate-500">Welcome back, {staff.role}</p>
        </div>

        {/* SUPER ADMIN */}
        {staff.role === "SUPER_ADMIN" && (
          <div className="grid gap-4 lg:grid-cols-3">
            <SummaryCard
              title="Total Branch"
              value={branchesQuery.data?.length ?? 0}
              isLoading={branchesQuery.isLoading}
            />

            <SummaryCard
              title="Total Staff"
              value={staffRowsQuery.data?.length ?? 0}
              isLoading={staffRowsQuery.isLoading}
            />

            <SummaryCard
              title="Active Banner"
              value={bannersQuery.data?.length ?? 0}
              isLoading={bannersQuery.isLoading}
            />
          </div>
        )}
      </div>
    </AdminLayout>
  );
}
