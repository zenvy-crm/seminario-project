export type Role = "ADMIN" | "INTERMEDIO" | "USER";
export type UserStatus = "ACTIVE" | "INACTIVE";

export interface AppDocument {
  id: number;
  fileName: string;
  mimeType: string;
  sizeBytes: number;
  uploadedAt: string;
  url: string;
}

export interface User {
  id: number;
  firstName: string;
  lastName: string;
  username: string;
  email: string;
  phone?: string;
  birthDate?: string;
  identification?: string;
  role: Role;
  status: UserStatus;
  createdAt: string;
  lastLoginAt?: string;
  isLocked?: boolean;
  lockedUntil?: string;
  documents?: AppDocument[];
}

export interface AuthUser {
  id: number;
  username: string;
  firstName: string;
  lastName: string;
  role: Role;
}

export interface AuthResponse {
  success: boolean;
  requiresMfa?: boolean;
  message?: string;
  data: {
    accessToken?: string;
    expiresAt?: string;
    user?: AuthUser;
    mfaToken?: string;
  };
}

export interface PaginatedResponse<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

// ---------------------------------------------------------------------------
// Modulo de Operaciones: ordenes de compra sobre tabla particionada
// ---------------------------------------------------------------------------

export type OrdenEstado =
  "CREADA" | "EMITIDA" | "RECIBIDA" | "PAGADA" | "ANULADA";
export type Moneda = "GTQ" | "USD";

/** Nivel del RBAC de la Fase 2: USER = Estandar, INTERMEDIO = Intermedio, ADMIN = Administrador. */
export const NIVEL_ROL: Record<Role, 1 | 2 | 3> = {
  USER: 1,
  INTERMEDIO: 2,
  ADMIN: 3,
};
export const NOMBRE_ROL: Record<Role, string> = {
  USER: "Estándar",
  INTERMEDIO: "Intermedio",
  ADMIN: "Administrador",
};

export interface CreateLineaOrden {
  descripcion: string;
  cantidad: number;
  precioUnitario: number;
}

export interface CreateOrdenCompra {
  proveedorNit: string;
  proveedorNombre: string;
  /** Solo se usa si no se envian lineas: el backend crea una linea "Monto global". */
  monto?: number;
  moneda: Moneda;
  fechaEmision: string;
  bodegaDestino: string;
  detalle?: string;
  lineas?: CreateLineaOrden[];
}

export interface LineaOrden {
  linea: number;
  descripcion: string;
  cantidad: number;
  precioUnitario: number;
  importe: number;
  estadoLinea: boolean;
  usuarioBajaId?: number | null;
  fechaBaja?: string | null;
  motivoBaja?: string | null;
  rowVer: string;
}

export interface PresupuestoBodega {
  anio: number;
  bodega: string;
  montoAsignado: number;
  montoComprometido: number;
  montoDisponible: number;
}

export interface OrdenCompra {
  id: string;
  numeroOrden: string;
  fechaEmision: string;
  userId: number;
  proveedorNit: string;
  proveedorNombre: string;
  monto: number;
  moneda: Moneda;
  estado: OrdenEstado;
  bodegaDestino: string;
  detalle?: string;
  createdAt: string;
  usuarioAnulaId?: number | null;
  fechaAnulacion?: string | null;
  motivoAnulacion?: string | null;
  rowVer?: string;
  lineas?: LineaOrden[];
  presupuesto?: PresupuestoBodega | null;
}

export interface OrdenCompraQuery {
  desde?: string;
  hasta?: string;
  estado?: string;
  proveedorNit?: string;
  soloMias?: boolean;
  /** Solo ordenes maestro-detalle (excluye las filas del stress test). */
  conDetalle?: boolean;
  /** Busqueda exacta por numero de orden (ej. OC-2026-000001). */
  numeroOrden?: string;
  page?: number;
  pageSize?: number;
}

export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

export interface ParticionInfo {
  numeroParticion: number;
  archivoFisico: string;
  rangoDesde: string;
  rangoHasta: string;
  registrosEnEsteFragmento: number;
}

export interface SeedDataRequest {
  cantidad: number;
  anioInicial: number;
  anioFinal: number;
}

export interface SeedDataResult {
  registrosInsertados: number;
  duracionMs: number;
  lotes: number;
  registrosPorSegundo: number;
  particiones: ParticionInfo[];
}

// ---------------------------------------------------------------------------
// Fase 3: reportes (ranking con RANK(), reporte con rango obligatorio, auditoria de soft delete)
// ---------------------------------------------------------------------------

export interface RankingProveedor {
  /** Posicion calculada por SQL Server con RANK(): la UI no reordena ni recalcula. */
  posicionRanking: number;
  proveedorNit: string;
  proveedorNombre: string;
  totalComprometido: number;
  ordenes: number;
  lineas: number;
  participacionPct: number;
}

export interface ReporteOrdenesQuery {
  /** yyyy-MM-dd, obligatoria. */
  fechaDesde: string;
  /** yyyy-MM-dd, obligatoria. */
  fechaHasta: string;
  proveedorNit?: string;
  estado?: string;
  bodega?: string;
  page?: number;
  pageSize?: number;
}

export interface ReporteOrdenItem {
  id: string;
  numeroOrden: string;
  fechaEmision: string;
  proveedorNit: string;
  proveedorNombre: string;
  bodegaDestino: string;
  estado: OrdenEstado;
  moneda: Moneda;
  monto: number;
  lineasVigentes: number;
  lineasAnuladas: number;
}

export interface ReporteOrdenesResult extends PagedResult<ReporteOrdenItem> {
  montoTotal: number;
}

export type TipoSoftDelete = "ordenes" | "lineas" | "documentos";

export interface AuditoriaSoftDeleteQuery {
  tipo: TipoSoftDelete;
  fechaDesde?: string;
  fechaHasta?: string;
  page?: number;
  pageSize?: number;
}

export interface AuditoriaSoftDeleteItem {
  tipo: string;
  referencia: string;
  detalle: string;
  monto: number | null;
  fechaBaja: string;
  usuarioBajaId: number;
  usuarioBaja: string;
  motivo: string;
  entidadId: string;
}

export interface AuditoriaSoftDeleteResult extends PagedResult<AuditoriaSoftDeleteItem> {
  resumen: { ordenes: number; lineas: number; documentos: number };
}
