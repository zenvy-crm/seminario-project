import { describe, expect, it } from "vitest";
import {
  fechaLocal,
  formatoFecha,
  formatoFechaHora,
  statusHttp,
} from "./comunes";

describe("utilidades de reportes", () => {
  it("FE-REG-01 [Regresion] formatoFechaHora trata como UTC las fechas sin zona y respeta Z/offset", () => {
    const sinZona = formatoFechaHora("2026-11-03T15:30:00");
    const conZ = formatoFechaHora("2026-11-03T15:30:00Z");
    expect(sinZona).toBe(conZ);
    expect(formatoFechaHora("2026-11-03T09:30:00-06:00")).toBe(conZ);
    expect(formatoFechaHora("no-es-fecha-z")).toBe("no-es-fecha-z");
  });

  it("FE-REG-02 formatoFecha no desplaza el dia por la zona horaria", () => {
    expect(formatoFecha("2026-01-31T00:00:00")).toBe("31/01/2026");
    expect(formatoFecha(null)).toBe("-");
    expect(formatoFechaHora(undefined)).toBe("-");
    expect(fechaLocal(new Date(2026, 0, 5))).toBe("2026-01-05");
  });

  it("FE-REG-03 statusHttp lee el codigo HTTP de un error de axios", () => {
    expect(statusHttp({ response: { status: 403 } })).toBe(403);
    expect(statusHttp(new Error("red"))).toBeUndefined();
  });
});
