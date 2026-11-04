import { describe, expect, it, vi } from "vitest";
import type { AxiosError, InternalAxiosRequestConfig } from "axios";
import { AxiosHeaders } from "axios";
import { api } from "./axios";

type Handler<T> = { fulfilled: (v: T) => unknown; rejected?: (e: unknown) => unknown };
const interceptoresPeticion = (
  api.interceptors.request as unknown as {
    handlers: Handler<InternalAxiosRequestConfig>[];
  }
).handlers;
const interceptoresRespuesta = (
  api.interceptors.response as unknown as { handlers: Handler<unknown>[] }
).handlers;

const config = (): InternalAxiosRequestConfig => ({
  headers: new AxiosHeaders(),
});

describe("cliente HTTP (axios)", () => {
  it("FE-API-01 agrega el JWT de la sesion guardada en la cabecera Authorization", () => {
    localStorage.setItem(
      "industry_session",
      JSON.stringify({ accessToken: "jwt-123" })
    );

    const resultado = interceptoresPeticion[0].fulfilled(
      config()
    ) as InternalAxiosRequestConfig;

    expect(resultado.headers.Authorization).toBe("Bearer jwt-123");
  });

  it("FE-API-02 sin sesion o con sesion corrupta no agrega Authorization", () => {
    const sinSesion = interceptoresPeticion[0].fulfilled(
      config()
    ) as InternalAxiosRequestConfig;
    expect(sinSesion.headers.Authorization).toBeUndefined();

    localStorage.setItem("industry_session", "{no es json");
    const corrupta = interceptoresPeticion[0].fulfilled(
      config()
    ) as InternalAxiosRequestConfig;
    expect(corrupta.headers.Authorization).toBeUndefined();
  });

  it("FE-API-03 un 401 borra la sesion, avisa a la app y rechaza la promesa", async () => {
    localStorage.setItem("industry_session", "{}");
    const listener = vi.fn();
    window.addEventListener("auth:unauthorized", listener);
    window.history.pushState({}, "", "/login");

    const error = { response: { status: 401 } } as AxiosError;
    await expect(interceptoresRespuesta[0].rejected!(error)).rejects.toBe(
      error
    );

    expect(localStorage.getItem("industry_session")).toBeNull();
    expect(listener).toHaveBeenCalledTimes(1);
    window.removeEventListener("auth:unauthorized", listener);
  });

  it("FE-API-04 otros errores se propagan sin cerrar la sesion", async () => {
    localStorage.setItem("industry_session", "{}");
    const error = { response: { status: 500 } } as AxiosError;

    await expect(interceptoresRespuesta[0].rejected!(error)).rejects.toBe(
      error
    );
    expect(localStorage.getItem("industry_session")).toBe("{}");
    expect(interceptoresRespuesta[0].fulfilled("ok")).toBe("ok");
  });
});
