import axios from "axios";

const baseURL = process.env.NEXT_PUBLIC_API_URL;

if (!baseURL) {
  throw new Error("NEXT_PUBLIC_API_URL is not defined");
}

export const adminApi = axios.create({
  baseURL,
  withCredentials: true,
  headers: {
    "Content-Type": "application/json",
    "ngrok-skip-browser-warning": "true",
  },
});

adminApi.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error?.response?.status === 401 && typeof window !== "undefined") {
      window.location.href = "/admin/login";
    }

    return Promise.reject(error);
  }
);
