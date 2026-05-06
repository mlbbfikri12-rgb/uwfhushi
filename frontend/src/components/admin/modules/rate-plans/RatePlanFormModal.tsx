"use client";

import { RatePlanAdmin, RatePlanForm } from "@/types/admin-rateplan";
import { useState, useEffect } from "react";

type Props = {
  open: boolean;
  onClose: () => void;
  onSubmit: (data: RatePlanForm) => void;
  initialData?: RatePlanAdmin | null;
};

export function RatePlanFormModal({
  open,
  onClose,
  onSubmit,
  initialData,
}: Props) {
  const [form, setForm] = useState<RatePlanForm>({
    name: "",
    price: 0,
    includesBreakfast: false,
    isRefundable: false,
    paymentType: "online",
    termsConditions: "",
    isActive: true,
  });

  const [errors, setErrors] = useState<Record<string, string>>({});

  useEffect(() => {
    if (initialData) {
      setForm({
        name: initialData.name,
        price: initialData.price,
        includesBreakfast: initialData.includesBreakfast,
        isRefundable: initialData.isRefundable,
        paymentType: initialData.paymentType,
        termsConditions: initialData.termsConditions,
        isActive: initialData.isActive,
      });
    }
  }, [initialData]);

  if (!open) return null;

  const validate = () => {
    const err: Record<string, string> = {};

    if (!form.name.trim()) err.name = "Name is required";
    if (form.price <= 0) err.price = "Price must be > 0";
    if (!form.paymentType) err.paymentType = "Payment type is required";

    setErrors(err);
    return Object.keys(err).length === 0;
  };

  const handleSubmit = () => {
    if (!validate()) return;
    onSubmit(form);
    onClose();
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
      <div className="w-full max-w-lg rounded-xl bg-white p-6 shadow-lg space-y-4">
        <h2 className="text-lg font-semibold">
          {initialData ? "Edit Rate Plan" : "Create Rate Plan"}
        </h2>

        <input
          value={form.name}
          onChange={(e) => setForm({ ...form, name: e.target.value })}
          className="w-full border px-3 py-2 rounded"
          placeholder="Name"
        />
        {errors.name && (
          <p className="mt-1 text-xs text-red-500">{errors.name}</p>
        )}

        <input
          type="number"
          value={form.price}
          onChange={(e) => setForm({ ...form, price: Number(e.target.value) })}
          className="w-full border px-3 py-2 rounded"
        />
        {errors.price && (
          <p className="mt-1 text-xs text-red-500">{errors.price}</p>
        )}

        <select
          value={form.paymentType}
          onChange={(e) =>
            setForm({
              ...form,
              paymentType: e.target.value as RatePlanForm["paymentType"],
            })
          }
          className="w-full border px-3 py-2 rounded"
        >
          <option value="online">Online</option>
          <option value="pay_at_hotel">Pay at Hotel</option>
        </select>
        {errors.paymentType && (
          <p className="mt-1 text-xs text-red-500">{errors.paymentType}</p>
        )}

        <div className="flex gap-4">
          <label>
            <input
              type="checkbox"
              checked={form.isRefundable}
              onChange={(e) =>
                setForm({ ...form, isRefundable: e.target.checked })
              }
            />
            Refundable
          </label>

          <label>
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
        </div>

        <textarea
          value={form.termsConditions}
          onChange={(e) =>
            setForm({ ...form, termsConditions: e.target.value })
          }
          className="w-full border px-3 py-2 rounded"
        />
        {errors.termsConditions && (
          <p className="mt-1 text-xs text-red-500">{errors.termsConditions}</p>
        )}

        <div className="flex justify-end gap-2">
          <button onClick={onClose}>Cancel</button>
          <button onClick={handleSubmit}>Save</button>
        </div>
      </div>
    </div>
  );
}
