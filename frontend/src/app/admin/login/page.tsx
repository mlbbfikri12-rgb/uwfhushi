"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation } from "@tanstack/react-query";
import { useRouter } from "next/navigation";
import { useForm } from "react-hook-form";
import { toast } from "sonner";
import { z } from "zod";
import { loginStaff } from "@/services/auth.service";

const schema = z.object({
  email: z.string().email("Email tidak valid"),
  password: z.string().min(8, "Password minimal 8 karakter"),
});

type StaffLoginValues = z.infer<typeof schema>;

export default function AdminLoginPage() {
  const router = useRouter();
  const { register, handleSubmit, formState: { errors } } = useForm<StaffLoginValues>({
    resolver: zodResolver(schema),
  });

  const mutation = useMutation({
    mutationFn: loginStaff,
    onSuccess: () => {
      toast.success("Login staff berhasil");
      router.push("/admin");
    },
    onError: (error) => {
      toast.error(error instanceof Error ? error.message : "Login staff gagal");
    },
  });

  return (
    <main className="min-h-screen bg-slate-100 px-4 py-16">
      <form
        onSubmit={handleSubmit((values) => mutation.mutate(values))}
        className="mx-auto max-w-md space-y-4 rounded-lg bg-white p-6 shadow-sm"
      >
        <h1 className="text-xl font-semibold text-slate-900">Login Admin</h1>
        <input className="w-full rounded border px-3 py-2 text-sm" placeholder="Email" type="email" {...register("email")} />
        {errors.email && <p className="text-xs text-red-600">{errors.email.message}</p>}
        <input className="w-full rounded border px-3 py-2 text-sm" placeholder="Password" type="password" {...register("password")} />
        {errors.password && <p className="text-xs text-red-600">{errors.password.message}</p>}
        <button className="w-full rounded bg-slate-900 px-4 py-2 text-sm font-semibold text-white" disabled={mutation.isPending}>
          {mutation.isPending ? "Memproses..." : "Login Admin"}
        </button>
      </form>
    </main>
  );
}
