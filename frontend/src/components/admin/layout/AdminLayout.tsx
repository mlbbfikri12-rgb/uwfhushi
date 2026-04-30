"use client";

import { AdminSidebar } from "./AdminSidebar";

export function AdminLayout({
  children,
  role,
}: {
  children: React.ReactNode;
  role: "SUPER_ADMIN" | "SPV" | "FO";
}) {
  return (
    <div className="flex min-h-screen bg-slate-100">
      {/* SIDEBAR */}
      <AdminSidebar role={role} />

      {/* MAIN */}
      <div className="flex-1 flex flex-col">
        {/* HEADER */}
        <header className="bg-white border-b px-6 py-4 flex justify-between items-center">
          <h1 className="text-sm text-slate-600">Admin Panel</h1>

          <div className="text-xs text-slate-500">
            Logged in as <span className="font-semibold">{role}</span>
          </div>
        </header>

        {/* CONTENT */}
        <main className="p-6">{children}</main>
      </div>
    </div>
  );
}
