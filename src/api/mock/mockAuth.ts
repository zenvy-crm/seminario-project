import { AuthResponse, AuthUser } from "../../types";

const delay = (ms = 600) => new Promise((r) => setTimeout(r, ms));

const CREDENTIALS: Record<string, { password: string; user: AuthUser }> = {
  "admin@bowpi.com": { password: "Admin123!", user: { id: 1, username: "admin", firstName: "Admin", lastName: "User", role: "ADMIN" } },
  "jperez@bowpi.com": { password: "User123!", user: { id: 2, username: "jperez", firstName: "Juan", lastName: "Pérez", role: "USER" } },
};

let pendingMfa: { token: string; user: AuthUser } | null = null;

export const mockAuth = {
  async login(username: string, password: string): Promise<AuthResponse> {
    await delay();
    const entry = CREDENTIALS[username.toLowerCase()];
    if (!entry || entry.password !== password) {
      return { success: false, message: "Usuario o contraseña incorrectos.", data: {} };
    }
    const mfaToken = `mfa-${entry.user.id}-${Date.now()}`;
    pendingMfa = { token: mfaToken, user: entry.user };
    return { success: true, requiresMfa: true, data: { mfaToken } };
  },

  async verifyMfa(mfaToken: string, code: string): Promise<AuthResponse> {
    await delay();
    if (!pendingMfa || pendingMfa.token !== mfaToken) {
      return { success: false, message: "La sesión de verificación expiró. Inicia sesión nuevamente.", data: {} };
    }
    if (code !== "123456") {
      return { success: false, message: "Código inválido. Intenta nuevamente.", data: {} };
    }
    const accessToken = `mock-jwt-${pendingMfa.user.id}-${Date.now()}`;
    const user = pendingMfa.user;
    pendingMfa = null;
    return { success: true, data: { accessToken, expiresAt: new Date(Date.now() + 1000 * 60 * 30).toISOString(), user } };
  },

  async register(_payload: FormData): Promise<AuthResponse> {
    await delay(900);
    return { success: true, data: {} };
  },

  async refreshSession(): Promise<AuthResponse> {
    await delay(300);
    return { success: false, data: {} };
  },
};
