import { create } from "zustand";

interface AuthState {
  accessToken: string | null;
  refreshToken: string | null;
  setTokens: (accessToken: string, refreshToken: string) => void;
  clear: () => void;
}

// Refresh token persisted so a page reload doesn't force a re-login;
// access token deliberately kept in memory only (shorter-lived, less
// exposure if something reads localStorage).
export const useAuthStore = create<AuthState>((set) => ({
  accessToken: null,
  refreshToken: localStorage.getItem("taskflow_refresh_token"),
  setTokens: (accessToken, refreshToken) => {
    localStorage.setItem("taskflow_refresh_token", refreshToken);
    set({ accessToken, refreshToken });
  },
  clear: () => {
    localStorage.removeItem("taskflow_refresh_token");
    set({ accessToken: null, refreshToken: null });
  },
}));
