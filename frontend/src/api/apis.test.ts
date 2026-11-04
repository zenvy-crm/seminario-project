import { beforeEach, describe, expect, it, vi } from "vitest";
import { api } from "./axios";
import { authApi } from "./authApi";
import { documentsApi } from "./documentsApi";
import { profileApi } from "./profileApi";
import { usersApi } from "./usersApi";
import { reportesApi } from "./reportesApi";
import { mensajeError, operacionesApi } from "./operacionesApi";
import type { OrdenCompra } from "../types";

const PDF = new Uint8Array([
  0x25, 0x50, 0x44, 0x46, 0x2d, 0x31, 0x2e, 0x37, 0x0a, 0x25, 0xe2, 0xe3,
  0xcf, 0xd3, 0x0a,
]);

const crearEspias = () => ({
  get: vi.spyOn(api, "get").mockResolvedValue({ data: "GET" }),
  post: vi.spyOn(api, "post").mockResolvedValue({ data: "POST" }),
  put: vi.spyOn(api, "put").mockResolvedValue({ data: "PUT" }),
  patch: vi.spyOn(api, "patch").mockResolvedValue({ data: "PATCH" }),
  del: vi.spyOn(api, "delete").mockResolvedValue({ data: "DELETE" }),
});

describe("capa de acceso a la API", () => {
  let get: ReturnType<typeof crearEspias>["get"];
  let post: ReturnType<typeof crearEspias>["post"];
  let put: ReturnType<typeof crearEspias>["put"];
  let patch: ReturnType<typeof crearEspias>["patch"];
  let del: ReturnType<typeof crearEspias>["del"];

  beforeEach(() => {
    ({ get, post, put, patch, del } = crearEspias());
  });

  it("FE-API-05 authApi llama a los endpoints de autenticacion", async () => {
    await authApi.login("admin", "secreta");
    expect(post).toHaveBeenCalledWith("/auth/login", {
      username: "admin",
      password: "secreta",
    });

    await authApi.verifyMfa("tok", "123456");
    expect(post).toHaveBeenCalledWith("/auth/mfa/verify", {
      mfaToken: "tok",
      code: "123456",
    });

    const fd = new FormData();
    await authApi.register(fd);
    expect(post).toHaveBeenCalledWith("/auth/register", fd, {
      headers: { "Content-Type": "multipart/form-data" },
    });

    await expect(authApi.refreshSession()).resolves.toBe("POST");
    await authApi.logout();
    expect(post).toHaveBeenCalledWith("/auth/logout");
  });

  it("FE-API-06 usersApi no envia el filtro 'ALL' como valor literal (regresion)", async () => {
    await usersApi.getUsers({ role: "ALL", status: "ALL", search: "ana" });
    expect(get).toHaveBeenCalledWith("/users", {
      params: { search: "ana", role: undefined, status: undefined },
    });

    await usersApi.getUsers({ role: "ADMIN", status: "ACTIVE" });
    expect(get).toHaveBeenLastCalledWith("/users", {
      params: { role: "ADMIN", status: "ACTIVE" },
    });

    await usersApi.getUsers();
    expect(get).toHaveBeenLastCalledWith("/users", {
      params: { role: undefined, status: undefined },
    });
  });

  it("FE-API-07 usersApi cubre consultar, crear, editar, estado, rol y desbloqueo", async () => {
    await usersApi.getUser(4);
    expect(get).toHaveBeenCalledWith("/users/4");
    await usersApi.createUser({ username: "x", password: "p" });
    expect(post).toHaveBeenCalledWith("/users", { username: "x", password: "p" });
    await usersApi.changeStatus(4, "INACTIVE");
    expect(put).toHaveBeenCalledWith("/users/4", { status: "INACTIVE" });
    await usersApi.changeRole(4, "INTERMEDIO");
    expect(put).toHaveBeenCalledWith("/users/4", { role: "INTERMEDIO" });
    await expect(usersApi.unlockUser(4)).resolves.toBe("PATCH");
    expect(patch).toHaveBeenCalledWith("/users/4/unlock");
  });

  it("FE-API-08 profileApi consulta, actualiza y cambia la contrasena", async () => {
    await profileApi.getProfile(1);
    expect(get).toHaveBeenCalledWith("/profile");
    await profileApi.updateProfile(1, { firstName: "Ana" });
    expect(put).toHaveBeenCalledWith("/profile", { firstName: "Ana" });
    await profileApi.changePassword("a", "b");
    expect(post).toHaveBeenCalledWith("/profile/change-password", {
      currentPassword: "a",
      newPassword: "b",
    });
  });

  it("FE-API-09 documentsApi rechaza extensiones no permitidas antes de llamar a la API", async () => {
    const exe = new File([PDF], "virus.exe", { type: "application/pdf" });
    await expect(documentsApi.upload(1, exe)).rejects.toThrow(
      "Solo se permiten archivos PDF o JPEG"
    );
    expect(post).not.toHaveBeenCalled();
  });

  it("FE-API-10 documentsApi rechaza un archivo cuyo contenido no es PDF/JPEG", async () => {
    const falso = new File(["texto plano"], "doc.pdf", {
      type: "application/pdf",
    });
    await expect(documentsApi.upload(1, falso)).rejects.toThrow();
    expect(post).not.toHaveBeenCalled();
  });

  it("FE-API-11 documentsApi sube un PDF valido y gestiona descarga y borrado", async () => {
    const pdf = new File([PDF], "doc.pdf", { type: "application/pdf" });
    await expect(documentsApi.upload(7, pdf)).resolves.toBe("POST");
    expect(post).toHaveBeenCalledWith(
      "/users/7/documents",
      expect.any(FormData),
      { headers: { "Content-Type": "multipart/form-data" } }
    );

    await documentsApi.getDocuments(7);
    expect(get).toHaveBeenCalledWith("/users/7/documents");
    await documentsApi.getBlob(7, 3);
    expect(get).toHaveBeenCalledWith("/users/7/documents/3/content", {
      responseType: "blob",
    });
    await documentsApi.remove(7, 3);
    expect(del).toHaveBeenCalledWith("/users/7/documents/3");
  });

  it("FE-API-12 reportesApi elimina filtros vacios antes de consultar", async () => {
    await reportesApi.rankingProveedores(2026, 20);
    expect(get).toHaveBeenCalledWith("/reportes/ranking-proveedores", {
      params: { anio: 2026, top: 20 },
    });

    await reportesApi.ordenes({
      fechaDesde: "2026-01-01",
      fechaHasta: "2026-01-31",
      estado: "",
      bodega: undefined,
    } as never);
    expect(get).toHaveBeenLastCalledWith("/reportes/ordenes", {
      params: { fechaDesde: "2026-01-01", fechaHasta: "2026-01-31" },
    });

    await reportesApi.auditoriaSoftDelete({ tipo: "lineas" } as never);
    expect(get).toHaveBeenLastCalledWith("/reportes/auditoria/soft-delete", {
      params: { tipo: "lineas" },
    });
  });

  it("FE-API-13 operacionesApi envia la fecha de emision para la eliminacion de particiones", async () => {
    const orden = {
      id: "abc",
      fechaEmision: "2026-11-03T00:00:00",
      rowVer: "AAAA",
    } as OrdenCompra;

    await operacionesApi.crearOrden({} as never);
    expect(post).toHaveBeenCalledWith("/operaciones/ordenes", {});
    await operacionesApi.listarOrdenes({ page: 1 } as never);
    expect(get).toHaveBeenCalledWith("/operaciones/ordenes", {
      params: { page: 1 },
    });
    await operacionesApi.obtenerOrden("abc", orden.fechaEmision);
    expect(get).toHaveBeenCalledWith("/operaciones/ordenes/abc", {
      params: { fechaEmision: "2026-11-03" },
    });
    await operacionesApi.obtenerOrden("abc");
    expect(get).toHaveBeenLastCalledWith("/operaciones/ordenes/abc", {
      params: {},
    });
    await operacionesApi.presupuestos(2026);
    await operacionesApi.anularLinea(orden, 2, "motivo valido", "RV");
    expect(put).toHaveBeenCalledWith(
      "/operaciones/ordenes/abc/lineas/2",
      { motivo: "motivo valido", estadoLinea: 0, rowVer: "RV" },
      { params: { fechaEmision: "2026-11-03" } }
    );
    await operacionesApi.anularOrden(orden, "duplicada");
    expect(del).toHaveBeenCalledWith("/operaciones/ordenes/abc", {
      params: { fechaEmision: "2026-11-03" },
      data: { motivo: "duplicada", rowVer: "AAAA" },
    });
    await operacionesApi.getParticiones();
    await operacionesApi.inyectarData({ cantidad: 10 } as never);
    expect(post).toHaveBeenLastCalledWith(
      "/seed-data",
      { cantidad: 10 },
      { timeout: 600000 }
    );
  });

  it("FE-API-14 mensajeError prioriza el primer error de validacion, luego el mensaje", () => {
    expect(
      mensajeError(
        { response: { data: { message: "m", errors: ["e1", "e2"] } } },
        "x"
      )
    ).toBe("e1");
    expect(mensajeError({ response: { data: { message: "m" } } }, "x")).toBe(
      "m"
    );
    expect(mensajeError(new Error("red"), "por defecto")).toBe("por defecto");
  });
});
