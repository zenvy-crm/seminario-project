import { api } from "./axios";
import { User, PaginatedResponse, Role, UserStatus } from "../types";

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
    const { data } = await api.get<PaginatedResponse<User>>("/users", {
      params: {
        ...query,
        role: query.role && query.role !== "ALL" ? query.role : undefined,
        status:
          query.status && query.status !== "ALL" ? query.status : undefined,
      },
    });
    return data;
  },

  async getUser(id: number): Promise<User> {
    const { data } = await api.get<User>(`/users/${id}`);
    return data;
  },

  async createUser(
    payload: Partial<User> & { password: string }
  ): Promise<User> {
    const { data } = await api.post<User>("/users", payload);
    return data;
  },

  async updateUser(id: number, payload: Partial<User>): Promise<User> {
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
    const { data } = await api.patch<User>(`/users/${id}/unlock`);
    return data;
  },
};
