import { api } from "./axios";
import {
  AuditoriaSoftDeleteQuery,
  AuditoriaSoftDeleteResult,
  RankingProveedor,
  ReporteOrdenesQuery,
  ReporteOrdenesResult,
} from "../types";

export { mensajeError } from "./operacionesApi";

/** Quita los filtros vacios para no enviar `estado=&bodega=` al backend. */
const sinVacios = <T extends object>(params: T): Partial<T> =>
  Object.fromEntries(Object.entries(params).filter(([, v]) => v !== undefined && v !== null && v !== "")) as Partial<T>;

export const reportesApi = {
  /** Ranking calculado por SQL Server con RANK(): se muestra en el orden recibido. */
  async rankingProveedores(anio: number, top: number): Promise<RankingProveedor[]> {
    const { data } = await api.get<RankingProveedor[]>("/reportes/ranking-proveedores", { params: { anio, top } });
    return data;
  },

  /** fechaDesde y fechaHasta son obligatorias (el backend responde 400 si faltan o el rango supera 366 dias). */
  async ordenes(query: ReporteOrdenesQuery): Promise<ReporteOrdenesResult> {
    const { data } = await api.get<ReporteOrdenesResult>("/reportes/ordenes", { params: sinVacios(query) });
    return data;
  },

  /** Solo Administrador: el resto de roles recibe 403. */
  async auditoriaSoftDelete(query: AuditoriaSoftDeleteQuery): Promise<AuditoriaSoftDeleteResult> {
    const { data } = await api.get<AuditoriaSoftDeleteResult>("/reportes/auditoria/soft-delete", { params: sinVacios(query) });
    return data;
  },
};
