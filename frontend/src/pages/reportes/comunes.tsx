import { Box } from "@mui/material";
import { useAuth } from "../../auth/AuthContext";
import { industryColors } from "../../theme/industryTheme";
import { NIVEL_ROL, NOMBRE_ROL, OrdenEstado } from "../../types";

/** Ejercicio fiscal en curso. */
export const EJERCICIO = new Date().getFullYear();
export const ANIOS = [2024, 2025, 2026, 2027];

export const ESTADOS: OrdenEstado[] = [
  "CREADA",
  "EMITIDA",
  "RECIBIDA",
  "PAGADA",
  "ANULADA",
];

export const BODEGAS = [
  "Bodega Central Zona 12",
  "Centro de Distribucion Villa Nueva",
  "Bodega Norte Chimaltenango",
  "Bodega Regional Quetzaltenango",
  "Cross-dock Puerto Barrios",
];

export const colorEstado: Record<
  OrdenEstado,
  "default" | "info" | "success" | "error" | "warning"
> = {
  CREADA: "warning",
  EMITIDA: "info",
  RECIBIDA: "default",
  PAGADA: "success",
  ANULADA: "error",
};

export function dinero(n: number | undefined | null, simbolo = "Q") {
  return `${simbolo} ${(n ?? 0).toLocaleString("es-GT", { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
}

export const entero = (n: number | undefined | null) =>
  (n ?? 0).toLocaleString("es-GT");

/** yyyy-MM-dd en hora local (toISOString devolveria la fecha UTC). */
export function fechaLocal(d: Date) {
  const mes = String(d.getMonth() + 1).padStart(2, "0");
  const dia = String(d.getDate()).padStart(2, "0");
  return `${d.getFullYear()}-${mes}-${dia}`;
}

/** Fecha (sin hora) como dd/MM/yyyy, sin pasar por Date para no desplazarla por zona horaria. */
export function formatoFecha(iso: string | undefined | null) {
  if (!iso) return "-";
  const [a, m, d] = iso.slice(0, 10).split("-");
  return d && m && a ? `${d}/${m}/${a}` : iso;
}

/** Fechas de baja: el backend las guarda en UTC (SYSUTCDATETIME) y las envia sin zona. */
export function formatoFechaHora(iso: string | undefined | null) {
  if (!iso) return "-";
  const f = new Date(/(?:[zZ]|[+-]\d{2}:\d{2})$/.test(iso) ? iso : `${iso}Z`);
  return Number.isNaN(f.getTime())
    ? iso
    : f.toLocaleString("es-GT", { dateStyle: "short", timeStyle: "short" });
}

export const statusHttp = (e: unknown) =>
  (e as { response?: { status?: number } })?.response?.status;

/** Cabecera de contexto global (usuario, rol, nivel y ejercicio), igual a la de Orden de Compra. */
export function BarraContexto() {
  const { user, role } = useAuth();
  const nivel = role ? NIVEL_ROL[role] : 1;
  return (
    <Box
      sx={{
        bgcolor: industryColors.accent900,
        color: "#fff",
        px: 2,
        py: 1,
        mb: 2,
        fontFamily: "monospace",
        fontSize: 13,
        letterSpacing: 0.3,
      }}
    >
      [ Usuario: {user?.username ?? "-"} | Rol: {role ? NOMBRE_ROL[role] : "-"}{" "}
      ({role}) | Nivel: {nivel} | Ejercicio: {EJERCICIO} ]
    </Box>
  );
}
