import {
  createContext,
  useContext,
  useEffect,
  useState,
  ReactNode,
  useCallback,
  useRef,
} from "react";
import { authApi } from "../api/authApi";
import { AuthUser, Role } from "../types";

interface AuthContextValue {
  isAuthenticated: boolean;
  user: AuthUser | null;
  role: Role | null;
  accessToken: string | null;
  isLoading: boolean;
  login: (
    username: string,
    password: string
  ) => Promise<{ requiresMfa: boolean; mfaToken?: string; error?: string }>;
  verifyMfa: (
    mfaToken: string,
    code: string
  ) => Promise<{ success: boolean; error?: string }>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);
const STORAGE_KEY = "industry_session";

interface StoredSession {
  accessToken: string;
  expiresAt: string;
  user: AuthUser;
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [accessToken, setAccessToken] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const expiryTimer = useRef<ReturnType<typeof setTimeout>>();

  const clearSession = useCallback(() => {
    localStorage.removeItem(STORAGE_KEY);
    setUser(null);
    setAccessToken(null);
    if (expiryTimer.current) clearTimeout(expiryTimer.current);
  }, []);

  const scheduleExpiry = useCallback(
    (expiresAt: string) => {
      if (expiryTimer.current) clearTimeout(expiryTimer.current);
      const ms = new Date(expiresAt).getTime() - Date.now();
      if (ms <= 0) {
        clearSession();
        return;
      }
      expiryTimer.current = setTimeout(clearSession, ms);
    },
    [clearSession]
  );

  useEffect(() => {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (raw) {
      try {
        const stored: StoredSession = JSON.parse(raw);
        if (new Date(stored.expiresAt).getTime() > Date.now()) {
          setUser(stored.user);
          setAccessToken(stored.accessToken);
          scheduleExpiry(stored.expiresAt);
        } else {
          localStorage.removeItem(STORAGE_KEY);
        }
      } catch {
        localStorage.removeItem(STORAGE_KEY);
      }
    }
    setIsLoading(false);

    const onUnauthorized = () => clearSession();
    window.addEventListener("auth:unauthorized", onUnauthorized);
    return () =>
      window.removeEventListener("auth:unauthorized", onUnauthorized);
  }, [clearSession, scheduleExpiry]);

  const persist = useCallback(
    (token: string, expiresAt: string, u: AuthUser) => {
      localStorage.setItem(
        STORAGE_KEY,
        JSON.stringify({ accessToken: token, expiresAt, user: u })
      );
      setAccessToken(token);
      setUser(u);
      scheduleExpiry(expiresAt);
    },
    [scheduleExpiry]
  );

  const login = useCallback(
    async (username: string, password: string) => {
      const res = await authApi.login(username, password);
      if (!res.success)
        return {
          requiresMfa: false,
          error: res.message ?? "No fue posible iniciar sesión.",
        };
      if (res.requiresMfa)
        return { requiresMfa: true, mfaToken: res.data.mfaToken };
      if (res.data.accessToken && res.data.user && res.data.expiresAt) {
        persist(res.data.accessToken, res.data.expiresAt, res.data.user);
      }
      return { requiresMfa: false };
    },
    [persist]
  );

  const verifyMfa = useCallback(
    async (mfaToken: string, code: string) => {
      const res = await authApi.verifyMfa(mfaToken, code);
      if (
        !res.success ||
        !res.data.accessToken ||
        !res.data.user ||
        !res.data.expiresAt
      ) {
        return { success: false, error: res.message ?? "Código inválido." };
      }
      persist(res.data.accessToken, res.data.expiresAt, res.data.user);
      return { success: true };
    },
    [persist]
  );

  const logout = useCallback(() => {
    authApi.logout().catch(() => {});
    clearSession();
  }, [clearSession]);

  return (
    <AuthContext.Provider
      value={{
        isAuthenticated: !!accessToken,
        user,
        role: user?.role ?? null,
        accessToken,
        isLoading,
        login,
        verifyMfa,
        logout,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within AuthProvider");
  return ctx;
}
