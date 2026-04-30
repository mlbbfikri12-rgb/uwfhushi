import { publicApi } from "@/lib/public-api";
import type {
  ClientAuthResponse,
  CustomerMe,
  LoginPayload,
  RegisterPayload,
  StaffAuthResponse,
} from "@/types/auth";

export async function loginClient(payload: LoginPayload) {
  const { data } = await publicApi.post<ClientAuthResponse>("/api/auth/login", payload);
  return data;
}

export async function registerClient(payload: RegisterPayload) {
  const { data } = await publicApi.post<ClientAuthResponse>("/api/auth/register", payload);
  return data;
}

export async function getCurrentCustomer() {
  const { data } = await publicApi.get<CustomerMe>("/api/auth/me");
  return data;
}

export async function loginStaff(payload: LoginPayload) {
  const { data } = await publicApi.post<StaffAuthResponse>("/api/auth/staff/login", payload);
  return data;
}

export async function getCurrentStaff() {
  const { data } = await publicApi.get<StaffAuthResponse>("/api/auth/staff/me");
  return data;
}
