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
    "ngrok-skip-browser-warning": "true",
  },
});

api.interceptors.request.use((config) => {
  const branch = useBranchStore.getState().activeBranch;

  if (!branch) {
    return Promise.reject(
      new Error("Select a hotel first.")
    );
  }
  config.headers["X-Branch-Code"] = branch;

  return config;
});

api.interceptors.response.use(
  (response) => response,
  (error) => {
    const status = error?.response?.status;
    const url = error?.config?.url ?? "";

    const isAuthEndpoint =
      url.includes("/api/order") ||
      url.includes("/api/auth/me");

    if (status === 401 && typeof window !== "undefined" && !isAuthEndpoint) {
      const redirect = encodeURIComponent(
        window.location.pathname + window.location.search
      );

      window.location.href = `/login?redirect=${redirect}`;
    }

    return Promise.reject(error);
  }
);