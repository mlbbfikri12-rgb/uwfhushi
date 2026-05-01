"use client";

import { useForm } from "react-hook-form";

type Props = {
  roomId: string;
  checkIn: string;
  checkOut: string;
  adult?: number;
  child?: number;
};

type BookingFormValues = {
  email: string;
  firstName: string;
  lastName: string;
  phone: string;
  adult: number;
  child: number;
  notes?: string;
};

export function BookingForm({
  roomId,
  checkIn,
  checkOut,
  adult,
  child,
}: Props) {
  const { register, handleSubmit } = useForm<BookingFormValues>();

  const onSubmit = (data: BookingFormValues) => {
    console.log("BOOKING:", {
      ...data,
      roomId,
      checkIn,
      checkOut,
      adult,
      child,
    });
  };

  return (
    <form
      onSubmit={handleSubmit(onSubmit)}
      className="space-y-6 rounded-2xl bg-white p-6 shadow-sm"
    >
      {/* CONTACT */}
      <div>
        <h3 className="font-semibold mb-3">Contact Detail</h3>

        <input
          {...register("email")}
          placeholder="Email Address"
          className="w-full mb-3 rounded-lg border px-3 py-2"
        />

        <div className="grid grid-cols-2 gap-3">
          <input
            {...register("firstName")}
            placeholder="First Name"
            className="rounded-lg border px-3 py-2"
          />
          <input
            {...register("lastName")}
            placeholder="Last Name"
            className="rounded-lg border px-3 py-2"
          />
        </div>

        <input
          {...register("phone")}
          placeholder="Phone Number"
          className="w-full mt-3 rounded-lg border px-3 py-2"
        />
      </div>

      {/* GUEST */}
      <div>
        <h3 className="font-semibold mb-2">Guest Detail</h3>

        <div className="grid grid-cols-2 gap-3">
          <input
            defaultValue={adult ?? 1}
            {...register("adult", { valueAsNumber: true })}
            type="number"
          />

          <input
            defaultValue={child ?? 0}
            {...register("child", { valueAsNumber: true })}
            type="number"
          />
        </div>
      </div>

      {/* NOTES */}
      <textarea
        {...register("notes")}
        placeholder="Special request (optional)"
        className="w-full rounded-lg border px-3 py-2"
      />

      {/* CTA */}
      <button
        type="submit"
        className="w-full rounded-lg bg-[#1a1f3c] py-3 text-white font-semibold hover:bg-[#2a2f4c]"
      >
        Review Reservation
      </button>
    </form>
  );
}
