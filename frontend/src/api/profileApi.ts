import { api } from "./axios";
import { User } from "../types";

export const profileApi = {
  async getProfile(userId: number): Promise<User> {
    const { data } = await api.get<User>("/profile");
    return data;
  },

  async updateProfile(userId: number, payload: Partial<User>): Promise<User> {
    const { data } = await api.put<User>("/profile", payload);
    return data;
  },

  async changePassword(currentPassword: string, newPassword: string): Promise<void> {
    await api.post("/profile/change-password", { currentPassword, newPassword });
  },
};
