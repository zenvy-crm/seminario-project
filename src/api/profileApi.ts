import { api } from "./axios";
import { User } from "../types";
import { db } from "./mock/db";

const USE_MOCK = import.meta.env.VITE_USE_MOCK_API !== "false";
const delay = (ms = 500) => new Promise((r) => setTimeout(r, ms));

export const profileApi = {
  async getProfile(userId: number): Promise<User> {
    if (USE_MOCK) {
      await delay();
      const found = db.users.find((u) => u.id === userId);
      if (!found) throw new Error("Perfil no encontrado.");
      return found;
    }
    const { data } = await api.get<User>("/profile");
    return data;
  },

  async updateProfile(userId: number, payload: Partial<User>): Promise<User> {
    if (USE_MOCK) {
      await delay(600);
      const idx = db.users.findIndex((u) => u.id === userId);
      if (idx === -1) throw new Error("Perfil no encontrado.");
      db.users[idx] = { ...db.users[idx], ...payload };
      return db.users[idx];
    }
    const { data } = await api.put<User>("/profile", payload);
    return data;
  },

  async changePassword(currentPassword: string, newPassword: string): Promise<void> {
    if (USE_MOCK) {
      await delay(600);
      return;
    }
    await api.post("/profile/change-password", { currentPassword, newPassword });
  },
};
