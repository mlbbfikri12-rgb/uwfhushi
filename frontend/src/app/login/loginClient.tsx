"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation } from "@tanstack/react-query";
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { useForm } from "react-hook-form";
import { toast } from "sonner";
import { z } from "zod";
import { loginClient } from "@/services/auth.service";

const schema = z.object({
  email: z.string().email("Email tidak valid"),
  password: z.string().min(8, "Password minimal 8 karakter"),
});

type LoginValues = z.infer<typeof schema>;

export default function LoginClient() {
  const router = useRouter();
  const searchParams = useSearchParams();

  const redirect = searchParams.get("redirect") || "/";

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<LoginValues>({
    resolver: zodResolver(schema),
  });

  const mutation = useMutation({
    mutationFn: loginClient,
    onSuccess: () => {
      toast.success("Login berhasil");
      router.push(redirect);
    },
    onError: (error) => {
      toast.error(error instanceof Error ? error.message : "Login gagal");
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
            Login Customer
          </h1>
          <p className="text-sm text-slate-500">
            Masuk untuk melanjutkan booking.
          </p>
        </div>

        <div>
          <input
            className="w-full rounded border px-3 py-2 text-sm"
            placeholder="Email"
            type="email"
            {...register("email")}
          />
          {errors.email && (
            <p className="mt-1 text-xs text-red-600">{errors.email.message}</p>
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
            <p className="mt-1 text-xs text-red-600">
              {errors.password.message}
            </p>
          )}
        </div>

        <button
          type="submit"
          disabled={mutation.isPending}
          className="w-full rounded bg-slate-900 px-4 py-2 text-sm font-semibold text-white disabled:opacity-60"
        >
          {mutation.isPending ? "Memproses..." : "Login"}
        </button>

        <p className="text-center text-sm text-slate-600">
          Belum punya akun?{" "}
          <Link
            className="font-semibold text-slate-900"
            href={`/register?redirect=${encodeURIComponent(redirect)}`}
          >
            Register
          </Link>
        </p>
      </form>
    </main>
  );
}
