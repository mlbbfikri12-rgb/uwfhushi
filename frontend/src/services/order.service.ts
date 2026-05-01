import { api } from "@/lib/api";
import type { AddOrderItemPayload, OrderCurrent } from "@/types/order";

export async function addOrderItem(payload: AddOrderItemPayload) {
  const { data } = await api.post<OrderCurrent>("/api/order/add", payload);
  return data;
}

export async function getCurrentOrder() {
  const { data } = await api.get<OrderCurrent>("/api/order/current");
  return data;
}

export async function deleteOrderItem(orderItemId: string) {
  const { data } = await api.delete<OrderCurrent>(`/api/order/item/${orderItemId}`);
  return data;
}
