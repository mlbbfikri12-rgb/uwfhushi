import { z } from "zod";

export const bookingSchema = z
  .object({
    useAccountData: z.boolean().default(false),
    customerName: z.string().optional(),
    customerEmail: z.string().optional(),
    customerPhone: z.string().optional(),
    dateRange: z.object({
      from: z.date({ required_error: "Check-in wajib diisi" }),
      to: z.date({ required_error: "Check-out wajib diisi" }),
    }),
    adultCount: z.number().int().min(1).max(8),
    childCount: z.number().int().min(0).max(8),
    notes: z.string().optional(),
  })
  .refine((value) => value.dateRange.to > value.dateRange.from, {
    path: ["dateRange"],
    message: "Check-out harus setelah check-in",
  })
  .superRefine((value, ctx) => {
    if (value.useAccountData) return;

    if (!value.customerName?.trim()) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        path: ["customerName"],
        message: "Nama wajib diisi",
      });
    }

    if (!value.customerEmail?.trim()) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        path: ["customerEmail"],
        message: "Email wajib diisi",
      });
    } else if (!z.string().email().safeParse(value.customerEmail).success) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        path: ["customerEmail"],
        message: "Email tidak valid",
      });
    }

    if (!value.customerPhone?.trim()) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        path: ["customerPhone"],
        message: "Nomor telepon wajib diisi",
      });
    }
  });

export type BookingFormValues = z.infer<typeof bookingSchema>;
