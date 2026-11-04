import { act, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { describe, expect, it, vi } from "vitest";
import { authApi } from "../api/authApi";
import { AuthProvider, useAuth } from "./AuthContext";
import ProtectedRoute from "./ProtectedRoute";
import RoleRoute from "./RoleRoute";
import type { Role } from "../types";

const STORAGE_KEY = "industry_session";
const enUnaHora = () => new Date(Date.now() + 3_600_000).toISOString();

function guardarSesion(role: Role, expiresAt = enUnaHora()) {
  localStorage.setItem(
    STORAGE_KEY,
    JSON.stringify({
      accessToken: "jwt",
      expiresAt,
      user: { id: 1, username: "u", firstName: "U", lastName: "Test", role },
    })
  );
}

let auth: ReturnType<typeof useAuth>;
function Sonda() {
  auth = useAuth();
  return <span data-testid="estado">{auth.isAuthenticated ? "si" : "no"}</span>;
}

const renderConProveedor = () =>
  render(
    <AuthProvider>
      <Sonda />
    </AuthProvider>
  );

describe("AuthContext", () => {
  it("FE-AUTH-01 restaura una sesion vigente desde localStorage", async () => {
    guardarSesion("ADMIN");
    renderConProveedor();
    await waitFor(() => expect(auth.isLoading).toBe(false));
    expect(auth.isAuthenticated).toBe(true);
    expect(auth.role).toBe("ADMIN");
  });

  it("FE-AUTH-02 descarta una sesion expirada o corrupta", async () => {
    guardarSesion("USER", new Date(Date.now() - 1000).toISOString());
    const { unmount } = renderConProveedor();
    await waitFor(() => expect(auth.isLoading).toBe(false));
    expect(auth.isAuthenticated).toBe(false);
    expect(localStorage.getItem(STORAGE_KEY)).toBeNull();
    unmount();

    localStorage.setItem(STORAGE_KEY, "{corrupta");
    renderConProveedor();
    await waitFor(() => expect(auth.isLoading).toBe(false));
    expect(localStorage.getItem(STORAGE_KEY)).toBeNull();
  });

  it("FE-AUTH-03 login con MFA devuelve el desafio sin abrir sesion", async () => {
    vi.spyOn(authApi, "login").mockResolvedValue({
      success: true,
      requiresMfa: true,
      data: { mfaToken: "mfa-1" },
    });
    renderConProveedor();

    let r!: Awaited<ReturnType<typeof auth.login>>;
    await act(async () => {
      r = await auth.login("admin", "x");
    });

    expect(r).toEqual({ requiresMfa: true, mfaToken: "mfa-1" });
    expect(auth.isAuthenticated).toBe(false);
  });

  it("FE-AUTH-04 login fallido devuelve el mensaje del backend o uno por defecto", async () => {
    const login = vi.spyOn(authApi, "login");
    renderConProveedor();

    login.mockResolvedValueOnce({
      success: false,
      message: "Bloqueada",
      data: {},
    });
    await act(async () => {
      expect(await auth.login("a", "b")).toEqual({
        requiresMfa: false,
        error: "Bloqueada",
      });
    });

    login.mockResolvedValueOnce({ success: false, data: {} });
    await act(async () => {
      expect((await auth.login("a", "b")).error).toMatch(/No fue posible/);
    });
  });

  it("FE-AUTH-05 login sin MFA abre la sesion directamente", async () => {
    vi.spyOn(authApi, "login").mockResolvedValue({
      success: true,
      data: {
        accessToken: "jwt-directo",
        expiresAt: enUnaHora(),
        user: {
          id: 2,
          username: "u",
          firstName: "U",
          lastName: "L",
          role: "USER",
        },
      },
    });
    renderConProveedor();

    await act(async () => {
      await auth.login("u", "p");
    });

    expect(auth.accessToken).toBe("jwt-directo");
    expect(JSON.parse(localStorage.getItem(STORAGE_KEY)!).accessToken).toBe(
      "jwt-directo"
    );
  });

  it("FE-AUTH-06 verificar MFA guarda la sesion; un codigo invalido no", async () => {
    const verify = vi.spyOn(authApi, "verifyMfa");
    renderConProveedor();

    verify.mockResolvedValueOnce({
      success: false,
      message: "Codigo incorrecto",
      data: {},
    });
    await act(async () => {
      expect(await auth.verifyMfa("t", "000000")).toEqual({
        success: false,
        error: "Codigo incorrecto",
      });
    });
    expect(auth.isAuthenticated).toBe(false);

    verify.mockResolvedValueOnce({ success: false, data: {} });
    await act(async () => {
      expect((await auth.verifyMfa("t", "000000")).error).toMatch(/inv/);
    });

    verify.mockResolvedValueOnce({
      success: true,
      data: {
        accessToken: "jwt-mfa",
        expiresAt: enUnaHora(),
        user: {
          id: 1,
          username: "admin",
          firstName: "A",
          lastName: "S",
          role: "ADMIN",
        },
      },
    });
    await act(async () => {
      expect(await auth.verifyMfa("t", "123456")).toEqual({ success: true });
    });
    expect(screen.getByTestId("estado")).toHaveTextContent("si");
  });

  it("FE-AUTH-07 logout y el evento auth:unauthorized cierran la sesion", async () => {
    const logout = vi
      .spyOn(authApi, "logout")
      .mockRejectedValue(new Error("red"));
    guardarSesion("USER");
    renderConProveedor();
    await waitFor(() => expect(auth.isAuthenticated).toBe(true));

    act(() => auth.logout());
    expect(logout).toHaveBeenCalled();
    expect(auth.isAuthenticated).toBe(false);

    guardarSesion("USER");
    act(() => {
      window.dispatchEvent(new Event("auth:unauthorized"));
    });
    expect(localStorage.getItem(STORAGE_KEY)).toBeNull();
  });

  it("FE-AUTH-08 la sesion se cierra sola al expirar el token", async () => {
    vi.useFakeTimers();
    guardarSesion("USER", new Date(Date.now() + 5000).toISOString());
    renderConProveedor();
    expect(auth.isAuthenticated).toBe(true);

    act(() => {
      vi.advanceTimersByTime(6000);
    });

    expect(auth.isAuthenticated).toBe(false);
    vi.useRealTimers();
  });

  it("FE-AUTH-09 useAuth fuera del proveedor lanza un error claro", () => {
    vi.spyOn(console, "error").mockImplementation(() => {});
    expect(() => render(<Sonda />)).toThrow(/AuthProvider/);
  });
});

function Rutas({ allowed }: { allowed: Role[] }) {
  return (
    <AuthProvider>
      <MemoryRouter initialEntries={["/admin"]}>
        <Routes>
          <Route path="/login" element={<span>pantalla login</span>} />
          <Route path="/403" element={<span>prohibido</span>} />
          <Route element={<ProtectedRoute />}>
            <Route element={<RoleRoute allowed={allowed} />}>
              <Route path="/admin" element={<span>zona admin</span>} />
            </Route>
          </Route>
        </Routes>
      </MemoryRouter>
    </AuthProvider>
  );
}

describe("rutas protegidas", () => {
  it("FE-AUTH-10 sin sesion redirige a /login", async () => {
    render(<Rutas allowed={["ADMIN"]} />);
    expect(await screen.findByText("pantalla login")).toBeInTheDocument();
  });

  it("FE-AUTH-11 con un rol no permitido redirige a /403", async () => {
    guardarSesion("USER");
    render(<Rutas allowed={["ADMIN"]} />);
    expect(await screen.findByText("prohibido")).toBeInTheDocument();
  });

  it("FE-AUTH-12 con el rol permitido muestra la pagina", async () => {
    guardarSesion("ADMIN");
    render(<Rutas allowed={["ADMIN", "INTERMEDIO"]} />);
    expect(await screen.findByText("zona admin")).toBeInTheDocument();
  });
});
