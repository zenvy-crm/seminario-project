import { api } from "./axios";
import { User, PaginatedResponse, Role, UserStatus } from "../types";
import { db } from "./mock/db";

const USE_MOCK = import.meta.env.VITE_USE_MOCK_API !== "false";
const delay = (ms = 500) => new Promise((r) => setTimeout(r, ms));

export interface UsersQuery {
  search?: string;
  role?: Role | "ALL";
  status?: UserStatus | "ALL";
  page?: number;
  pageSize?: number;
  sortBy?: keyof User;
  sortDir?: "asc" | "desc";
}

export const usersApi = {
  async getUsers(query: UsersQuery = {}): Promise<PaginatedResponse<User>> {
    if (USE_MOCK) {
      await delay();
      let items = [...db.users];
      if (query.search) {
        const s = query.search.toLowerCase();
        items = items.filter((u) => `${u.firstName} ${u.lastName} ${u.username} ${u.email}`.toLowerCase().includes(s));
      }
      if (query.role && query.role !== "ALL") items = items.filter((u) => u.role === query.role);
      if (query.status && query.status !== "ALL") items = items.filter((u) => u.status === query.status);
      if (query.sortBy) {
        const dir = query.sortDir === "desc" ? -1 : 1;
        items.sort((a, b) => String(a[query.sortBy!] ?? "").localeCompare(String(b[query.sortBy!] ?? "")) * dir);
      }
      const page = query.page ?? 1;
      const pageSize = query.pageSize ?? 10;
      const start = (page - 1) * pageSize;
      return { items: items.slice(start, start + pageSize), total: items.length, page, pageSize };
    }
    const { data } = await api.get<PaginatedResponse<User>>("/users", {
      params: {
        ...query,
        role: query.role && query.role !== "ALL" ? query.role : undefined,
        status: query.status && query.status !== "ALL" ? query.status : undefined,
      },
    });
    return data;
  },

  async getUser(id: number): Promise<User> {
    if (USE_MOCK) {
      await delay();
      const found = db.users.find((u) => u.id === id);
      if (!found) throw new Error("Usuario no encontrado.");
      return found;
    }
    const { data } = await api.get<User>(`/users/${id}`);
    return data;
  },

  async createUser(payload: Partial<User> & { password: string }): Promise<User> {
    if (USE_MOCK) {
      await delay(700);
      const newUser: User = {
        id: Math.max(0, ...db.users.map((u) => u.id)) + 1,
        firstName: payload.firstName ?? "",
        lastName: payload.lastName ?? "",
        username: payload.username ?? "",
        email: payload.email ?? "",
        phone: payload.phone,
        identification: payload.identification,
        role: payload.role ?? "USER",
        status: payload.status ?? "ACTIVE",
        createdAt: new Date().toISOString(),
        documents: [],
      };
      db.users.push(newUser);
      return newUser;
    }
    const { data } = await api.post<User>("/users", payload);
    return data;
  },

  async updateUser(id: number, payload: Partial<User>): Promise<User> {
    if (USE_MOCK) {
      await delay(600);
      const idx = db.users.findIndex((u) => u.id === id);
      if (idx === -1) throw new Error("Usuario no encontrado.");
      db.users[idx] = { ...db.users[idx], ...payload };
      return db.users[idx];
    }
    const { data } = await api.put<User>(`/users/${id}`, payload);
    return data;
  },

  async changeStatus(id: number, status: UserStatus): Promise<User> {
    return usersApi.updateUser(id, { status });
  },

  async changeRole(id: number, role: Role): Promise<User> {
    return usersApi.updateUser(id, { role });
  },

  async unlockUser(id: number): Promise<User> {
    if (USE_MOCK) {
      await delay(400);
      const idx = db.users.findIndex((u) => u.id === id);
      if (idx === -1) throw new Error("Usuario no encontrado.");
      db.users[idx] = { ...db.users[idx], isLocked: false, lockedUntil: undefined };
      return db.users[idx];
    }
    const { data } = await api.patch<User>(`/users/${id}/unlock`);
    return data;
  },
};
