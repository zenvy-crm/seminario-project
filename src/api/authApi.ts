import { api } from "./axios";
import { AuthResponse } from "../types";
import { mockAuth } from "./mock/mockAuth";

const USE_MOCK = import.meta.env.VITE_USE_MOCK_API !== "false";

export const authApi = {
  async login(username: string, password: string): Promise<AuthResponse> {
    if (USE_MOCK) return mockAuth.login(username, password);
    const { data } = await api.post<AuthResponse>("/auth/login", { username, password });
    return data;
  },
  async verifyMfa(mfaToken: string, code: string): Promise<AuthResponse> {
    if (USE_MOCK) return mockAuth.verifyMfa(mfaToken, code);
    const { data } = await api.post<AuthResponse>("/auth/mfa/verify", { mfaToken, code });
    return data;
  },
  async register(payload: FormData): Promise<AuthResponse> {
    if (USE_MOCK) return mockAuth.register(payload);
    const { data } = await api.post<AuthResponse>("/auth/register", payload, { headers: { "Content-Type": "multipart/form-data" } });
    return data;
  },
  async refreshSession(): Promise<AuthResponse> {
    if (USE_MOCK) return mockAuth.refreshSession();
    const { data } = await api.post<AuthResponse>("/auth/refresh");
    return data;
  },
  async logout(): Promise<void> {
    if (USE_MOCK) return;
    await api.post("/auth/logout");
  },
};
