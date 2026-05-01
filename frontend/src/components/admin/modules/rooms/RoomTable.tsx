"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import { updateRoomStatus } from "@/services/admin.service";
import { toast } from "sonner";
import type { Room } from "@/types/room";

export function RoomTable({
  rooms,
  isLoading,
}: {
  rooms: Room[];
  isLoading: boolean;
}) {
  const queryClient = useQueryClient();

  const mutation = useMutation({
    mutationFn: ({ id, status }: { id: string; status: NonNullable<Room["status"]> }) =>
      updateRoomStatus(id, status),

    // 🔥 OPTIMISTIC UPDATE
    onMutate: async ({ id, status }) => {
      await queryClient.cancelQueries({ queryKey: ["rooms"] });

      const prev = queryClient.getQueryData<Room[]>(["rooms"]);

      queryClient.setQueryData(["rooms"], (old: Room[] | undefined) =>
        old?.map((room) => (room.id === id ? { ...room, status } : room)),
      );

      return { prev };
    },

    onError: (_err, _vars, context) => {
      if (context?.prev) {
        queryClient.setQueryData(["rooms"], context.prev);
      }
      toast.error("Gagal update status");
    },

    onSuccess: () => {
      toast.success("Status berhasil diupdate");
    },

    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: ["rooms"] });
    },
  });

  const badge = (status: NonNullable<Room["status"]>) => {
    if (status === "available") return "bg-green-100 text-green-700";
    if (status === "maintenance") return "bg-yellow-100 text-yellow-700";
    return "bg-red-100 text-red-700";
  };

  if (isLoading) {
    return <div className="p-4 text-sm">Loading rooms...</div>;
  }

  return (
    <div className="rounded-xl bg-white shadow-sm overflow-hidden">
      <table className="w-full text-sm">
        <thead className="bg-slate-50 text-left">
          <tr>
            <th className="px-4 py-3">Room</th>
            <th className="px-4 py-3">Type</th>
            <th className="px-4 py-3">Price</th>
            <th className="px-4 py-3">Status</th>
            <th className="px-4 py-3">Action</th>
          </tr>
        </thead>

        <tbody>
          {rooms.map((room) => (
            <tr key={room.id} className="border-t">
              <td className="px-4 py-3 font-medium">{room.roomNumber}</td>

              <td className="px-4 py-3">{room.roomType.name}</td>

              <td className="px-4 py-3 text-[#c4a661] font-semibold">
                Rp {room.roomType.basePrice.toLocaleString("id-ID")}
              </td>

              <td className="px-4 py-3">
                <span
                  className={`px-2 py-1 rounded text-xs ${badge(room.status!)}`}
                >
                  {room.status ?? "available"}
                </span>
              </td>

              <td className="px-4 py-3">
                <select
                  value={room.status ?? "available"}
                  onChange={(e) =>
                    mutation.mutate({
                      id: room.id,
                      status: e.target.value as NonNullable<Room["status"]>,
                    })
                  }
                  className="border rounded px-2 py-1 text-xs"
                >
                  <option value="available">Available</option>
                  <option value="maintenance">Maintenance</option>
                  <option value="occupied">Occupied</option>
                </select>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
