import { api } from "./axios";
import { AuthResponse } from "../types";

export const authApi = {
  async login(username: string, password: string): Promise<AuthResponse> {
    const { data } = await api.post<AuthResponse>("/auth/login", {
      username,
      password,
    });
    return data;
  },
  async verifyMfa(mfaToken: string, code: string): Promise<AuthResponse> {
    const { data } = await api.post<AuthResponse>("/auth/mfa/verify", {
      mfaToken,
      code,
    });
    return data;
  },
  async register(payload: FormData): Promise<AuthResponse> {
    const { data } = await api.post<AuthResponse>("/auth/register", payload, {
      headers: { "Content-Type": "multipart/form-data" },
    });
    return data;
  },
  async refreshSession(): Promise<AuthResponse> {
    const { data } = await api.post<AuthResponse>("/auth/refresh");
    return data;
  },
  async logout(): Promise<void> {
    await api.post("/auth/logout");
  },
};
