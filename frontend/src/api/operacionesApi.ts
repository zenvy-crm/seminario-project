import { api } from "./axios";
import {
  CreateOrdenCompra,
  OrdenCompra,
  OrdenCompraQuery,
  PagedResult,
  ParticionInfo,
  PresupuestoBodega,
  SeedDataRequest,
  SeedDataResult,
} from "../types";

/** fechaEmision como yyyy-MM-dd: habilita la eliminacion de particiones en SQL Server. Sin ella se busca en toda la tabla. */
const porFecha = (fechaEmision?: string) => (fechaEmision ? { fechaEmision: fechaEmision.slice(0, 10) } : {});

export const operacionesApi = {
  async crearOrden(payload: CreateOrdenCompra): Promise<OrdenCompra> {
    // El usuario no viaja en el cuerpo: el backend lo toma del JWT.
    const { data } = await api.post<OrdenCompra>("/operaciones/ordenes", payload);
    return data;
  },

  async listarOrdenes(query: OrdenCompraQuery): Promise<PagedResult<OrdenCompra>> {
    const { data } = await api.get<PagedResult<OrdenCompra>>("/operaciones/ordenes", { params: query });
    return data;
  },

  /** Encabezado + todas las lineas (incluidas las anuladas) + presupuesto de la bodega. */
  async obtenerOrden(id: string, fechaEmision?: string): Promise<OrdenCompra> {
    const { data } = await api.get<OrdenCompra>(`/operaciones/ordenes/${id}`, { params: porFecha(fechaEmision) });
    return data;
  },

  async presupuestos(anio?: number): Promise<PresupuestoBodega[]> {
    const { data } = await api.get<PresupuestoBodega[]>("/operaciones/presupuestos", { params: { anio } });
    return data;
  },

  /** Soft delete de una linea (rol Intermedio o superior). El trigger devuelve el importe al presupuesto. */
  async anularLinea(orden: OrdenCompra, linea: number, motivo: string, rowVer?: string): Promise<OrdenCompra> {
    const { data } = await api.put<OrdenCompra>(
      `/operaciones/ordenes/${orden.id}/lineas/${linea}`,
      { motivo, estadoLinea: 0, rowVer },
      { params: porFecha(orden.fechaEmision) },
    );
    return data;
  },

  /** Soft delete de la orden completa (solo Administrador): Estado = ANULADA. */
  async anularOrden(orden: OrdenCompra, motivo: string): Promise<OrdenCompra> {
    const { data } = await api.delete<OrdenCompra>(`/operaciones/ordenes/${orden.id}`, {
      params: porFecha(orden.fechaEmision),
      data: { motivo, rowVer: orden.rowVer },
    });
    return data;
  },

  async getParticiones(): Promise<ParticionInfo[]> {
    const { data } = await api.get<ParticionInfo[]>("/operaciones/particiones");
    return data;
  },

  async inyectarData(payload: SeedDataRequest): Promise<SeedDataResult> {
    // El stress test puede tardar: se amplia el timeout por encima del default de axios.
    const { data } = await api.post<SeedDataResult>("/seed-data", payload, { timeout: 600000 });
    return data;
  },
};

/** Mensaje legible de un error de la API (ApiErrorResponse: message + errors[]). */
export function mensajeError(error: unknown, porDefecto: string): string {
  const data = (error as { response?: { data?: { message?: string; errors?: string[] } } })?.response?.data;
  return data?.errors?.[0] ?? data?.message ?? porDefecto;
}
