"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation } from "@tanstack/react-query";
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { useForm } from "react-hook-form";
import { toast } from "sonner";
import { z } from "zod";
import { registerClient } from "@/services/auth.service";

const schema = z.object({
  name: z.string().min(2, "Nama minimal 2 karakter"),
  email: z.string().email("Email tidak valid"),
  phone: z.string().min(8, "Nomor telepon terlalu pendek"),
  password: z.string().min(8, "Password minimal 8 karakter"),
});

type RegisterValues = z.infer<typeof schema>;

export default function RegisterClient() {
  const router = useRouter();
  const searchParams = useSearchParams();

  // 🔐 Safe redirect (hindari open redirect)
  const redirectParam = searchParams.get("redirect");
  const redirect =
    redirectParam && redirectParam.startsWith("/") ? redirectParam : "/";

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<RegisterValues>({
    resolver: zodResolver(schema),
  });

  const mutation = useMutation({
    mutationFn: registerClient,
    onSuccess: () => {
      toast.success("Register berhasil");
      router.push(redirect);
    },
    onError: (error) => {
      toast.error(error instanceof Error ? error.message : "Register gagal");
    },
  });

  return (
    <main className="min-h-screen bg-slate-100 px-4 py-16">
      <form
        onSubmit={handleSubmit((values) => mutation.mutate(values))}
        className="mx-auto max-w-md space-y-4 rounded-lg bg-white p-6 shadow-sm"
      >
        <div>
          <h1 className="text-xl font-semibold text-slate-900">
            Register Customer
          </h1>
          <p className="text-sm text-slate-500">
            Buat akun untuk melakukan booking.
          </p>
        </div>

        <div>
          <input
            className="w-full rounded border px-3 py-2 text-sm"
            placeholder="Nama lengkap"
            {...register("name")}
          />
          {errors.name && (
            <p className="text-xs text-red-600">{errors.name.message}</p>
          )}
        </div>

        <div>
          <input
            className="w-full rounded border px-3 py-2 text-sm"
            placeholder="Email"
            type="email"
            {...register("email")}
          />
          {errors.email && (
            <p className="text-xs text-red-600">{errors.email.message}</p>
          )}
        </div>

        <div>
          <input
            className="w-full rounded border px-3 py-2 text-sm"
            placeholder="Nomor telepon"
            {...register("phone")}
          />
          {errors.phone && (
            <p className="text-xs text-red-600">{errors.phone.message}</p>
          )}
        </div>

        <div>
          <input
            className="w-full rounded border px-3 py-2 text-sm"
            placeholder="Password"
            type="password"
            {...register("password")}
          />
          {errors.password && (
            <p className="text-xs text-red-600">{errors.password.message}</p>
          )}
        </div>

        <button
          type="submit"
          disabled={mutation.isPending}
          className="w-full rounded bg-slate-900 px-4 py-2 text-sm font-semibold text-white disabled:opacity-60"
        >
          {mutation.isPending ? "Memproses..." : "Register"}
        </button>

        <p className="text-center text-sm text-slate-600">
          Sudah punya akun?{" "}
          <Link
            className="font-semibold text-slate-900"
            href={`/login?redirect=${encodeURIComponent(redirect)}`}
          >
            Login
          </Link>
        </p>
      </form>
    </main>
  );
}
