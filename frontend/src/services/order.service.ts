import { api } from "@/lib/api";
import type { AddOrderItemPayload, OrderCurrent } from "@/types/order";
import axios from "axios";

const EMPTY_ORDER: OrderCurrent = {
  orderDraftId: "",
  items: [],
  grandTotal: 0,
};

export async function addOrderItem(payload: AddOrderItemPayload) {
  const { data } = await api.post<OrderCurrent>("/api/order/add", payload);
  return data;
}

export async function getCurrentOrder() {
  try {
    const { data } = await api.get<OrderCurrent>("/api/order/current");
    return data;
  } catch (error) {
    if (axios.isAxiosError(error) && error.response?.status === 400) {
      const message =
        typeof error.response.data?.error === "string"
          ? error.response.data.error
          : "";

      if (message.toLowerCase().includes("customer not found in this branch")) {
        return EMPTY_ORDER;
      }
    }

    throw error;
  }
}

export async function deleteOrderItem(orderItemId: string) {
  const { data } = await api.delete<OrderCurrent>(`/api/order/item/${orderItemId}`);
  return data;
}
