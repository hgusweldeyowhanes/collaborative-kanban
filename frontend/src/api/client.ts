import axios, { AxiosError, InternalAxiosRequestConfig } from "axios";
import { useAuthStore } from "../store/authStore";
import type { AuthResponse } from "../types";

export const API_BASE_URL = import.meta.env.VITE_API_URL ?? "http://localhost:5000";

export const apiClient = axios.create({
  baseURL: API_BASE_URL,
});

// Attach the current access token to every outgoing request.
apiClient.interceptors.request.use((config) => {
  const token = useAuthStore.getState().accessToken;
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Queue concurrent requests that hit a 401 while a single refresh call is
// in flight, instead of firing off N parallel refresh requests.
let isRefreshing = false;
let pendingQueue: Array<(token: string | null) => void> = [];

function resolveQueue(token: string | null) {
  pendingQueue.forEach((cb) => cb(token));
  pendingQueue = [];
}

apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean };

    if (error.response?.status !== 401 || originalRequest._retry) {
      return Promise.reject(error);
    }

    const refreshToken = useAuthStore.getState().refreshToken;
    if (!refreshToken) {
      useAuthStore.getState().clear();
      return Promise.reject(error);
    }

    originalRequest._retry = true;

    if (isRefreshing) {
      // Wait for the in-flight refresh to finish, then retry with new token.
      return new Promise((resolve, reject) => {
        pendingQueue.push((token) => {
          if (!token) return reject(error);
          originalRequest.headers.Authorization = `Bearer ${token}`;
          resolve(apiClient(originalRequest));
        });
      });
    }

    isRefreshing = true;
    try {
      const { data } = await axios.post<AuthResponse>(`${API_BASE_URL}/api/auth/refresh`, {
        refreshToken,
      });
      useAuthStore.getState().setTokens(data.accessToken, data.refreshToken);
      resolveQueue(data.accessToken);
      originalRequest.headers.Authorization = `Bearer ${data.accessToken}`;
      return apiClient(originalRequest);
    } catch (refreshError) {
      resolveQueue(null);
      useAuthStore.getState().clear();
      return Promise.reject(refreshError);
    } finally {
      isRefreshing = false;
    }
  }
);
