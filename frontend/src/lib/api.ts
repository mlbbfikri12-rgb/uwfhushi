import axios from "axios";
import { useBranchStore } from "@/store/useBranchStore";

const baseURL = process.env.NEXT_PUBLIC_API_URL;

if (!baseURL) {
  throw new Error("NEXT_PUBLIC_API_URL is not defined");
}

export const api = axios.create({
  baseURL,
  withCredentials: true,
  headers: {
    "Content-Type": "application/json",
  },
});

api.interceptors.request.use((config) => {
  const branch = useBranchStore.getState().activeBranch;

  if (!branch) {
    return Promise.reject(
      new Error("Missing X-Branch-Code. Select a hotel branch first.")
    );
  }

  config.headers["X-Branch-Code"] = branch;

  // ❌ JANGAN pakai Authorization header
  // backend kamu pakai cookie-based auth

  return config;
});

api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error?.response?.status === 401 && typeof window !== "undefined") {
      const redirect = encodeURIComponent(
        window.location.pathname + window.location.search
      );
      window.location.href = `/login?redirect=${redirect}`;
    }

    return Promise.reject(error);
  }
);