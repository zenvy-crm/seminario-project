import { ReactNode, useCallback, useEffect, useMemo, useState } from "react";
import {
  Alert,
  Box,
  Button,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Grid,
  IconButton,
  LinearProgress,
  MenuItem,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Tooltip,
  Typography,
} from "@mui/material";
import {
  Ban,
  ChevronLeft,
  ChevronRight,
  ChevronsLeft,
  ChevronsRight,
  FilePlus,
  Lock,
  Plus,
  RefreshCw,
  Save,
  Search,
  Trash2,
  Wallet,
  X,
} from "lucide-react";
import { useAuth } from "../../auth/AuthContext";
import { useToast } from "../../components/common/ToastProvider";
import { mensajeError, operacionesApi } from "../../api/operacionesApi";
import { industryColors } from "../../theme/industryTheme";
import Blueprint from "../../theme/Blueprint";
import {
  LineaOrden,
  Moneda,
  NIVEL_ROL,
  NOMBRE_ROL,
  OrdenCompra,
  OrdenEstado,
  PresupuestoBodega,
} from "../../types";

// ---------------------------------------------------------------------------
// Constantes y utilidades
// ---------------------------------------------------------------------------

const PAGE_SIZE = 100;
const ANIOS = [2024, 2025, 2026, 2027];
const ESTADOS_ANULABLES: OrdenEstado[] = ["CREADA", "EMITIDA"];
/** Ejercicio fiscal en curso: el de la sesion, no el de la orden que se esta viendo. */
const EJERCICIO = new Date().getFullYear();

const colorEstado: Record<
  OrdenEstado,
  "default" | "info" | "success" | "error" | "warning"
> = {
  CREADA: "warning",
  EMITIDA: "info",
  RECIBIDA: "default",
  PAGADA: "success",
  ANULADA: "error",
};

const hoy = () => new Date().toISOString().slice(0, 10);
const redondear = (n: number) => Math.round(n * 100) / 100;

function dinero(n: number | undefined | null, moneda: Moneda | "GTQ" = "GTQ") {
  const simbolo = moneda === "USD" ? "$" : "Q";
  return `${simbolo} ${(n ?? 0).toLocaleString("es-GT", { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
}

interface LineaBorrador {
  key: number;
  descripcion: string;
  cantidad: string;
  precioUnitario: string;
}

interface Borrador {
  proveedorNit: string;
  proveedorNombre: string;
  moneda: Moneda;
  fechaEmision: string;
  bodegaDestino: string;
  detalle: string;
  lineas: LineaBorrador[];
}

let siguienteKey = 1;
const lineaVacia = (): LineaBorrador => ({
  key: siguienteKey++,
  descripcion: "",
  cantidad: "",
  precioUnitario: "",
});

const borradorVacio = (bodega: string): Borrador => ({
  proveedorNit: "",
  proveedorNombre: "",
  moneda: "GTQ",
  fechaEmision: hoy(),
  bodegaDestino: bodega,
  detalle: "",
  lineas: [lineaVacia()],
});

const importeBorrador = (l: LineaBorrador) => {
  const c = Number(l.cantidad);
  const p = Number(l.precioUnitario);
  return Number.isFinite(c) && Number.isFinite(p) ? redondear(c * p) : 0;
};

// ---------------------------------------------------------------------------
// Pagina
// ---------------------------------------------------------------------------

type Modo = "ver" | "nuevo";
type DialogoAnulacion =
  { tipo: "linea"; linea: LineaOrden } | { tipo: "orden" } | null;

export default function OrdenCompraPage() {
  const { user, role } = useAuth();
  const { showToast } = useToast();
  const nivel = role ? NIVEL_ROL[role] : 1;

  const [modo, setModo] = useState<Modo>("ver");
  const [paginas, setPaginas] = useState<Record<number, OrdenCompra[]>>({});
  const [total, setTotal] = useState(0);
  const [indice, setIndice] = useState(0);
  const [orden, setOrden] = useState<OrdenCompra | null>(null);
  const [cargando, setCargando] = useState(false);

  const [anio, setAnio] = useState(new Date().getFullYear());
  const [presupuestos, setPresupuestos] = useState<PresupuestoBodega[]>([]);

  const [borrador, setBorrador] = useState<Borrador>(borradorVacio(""));
  const [errores, setErrores] = useState<Record<string, string>>({});
  const [errorServidor, setErrorServidor] = useState<string | null>(null);
  const [guardando, setGuardando] = useState(false);

  const [dialogo, setDialogo] = useState<DialogoAnulacion>(null);

  const [textoBusqueda, setTextoBusqueda] = useState("");
  const [buscando, setBuscando] = useState(false);
  /** true cuando la orden en pantalla vino de una busqueda y no de la navegacion. */
  const [porBusqueda, setPorBusqueda] = useState(false);

  // ------------------------------------------------------------- datos
  const cargarPresupuestos = useCallback(async (a: number) => {
    try {
      setPresupuestos(await operacionesApi.presupuestos(a));
    } catch {
      setPresupuestos([]);
    }
  }, []);

  const cargarPagina = useCallback(
    async (pagina: number, forzar = false) => {
      if (!forzar && paginas[pagina]) return paginas[pagina];
      const res = await operacionesApi.listarOrdenes({
        conDetalle: true,
        page: pagina + 1,
        pageSize: PAGE_SIZE,
      });
      setTotal(res.total);
      setPaginas((p) => ({ ...(forzar ? {} : p), [pagina]: res.items }));
      return res.items;
    },
    [paginas]
  );

  const irA = useCallback(
    async (i: number, forzar = false) => {
      setCargando(true);
      try {
        const items = await cargarPagina(Math.floor(i / PAGE_SIZE), forzar);
        const resumen = items[i % PAGE_SIZE];
        if (!resumen) {
          setOrden(null);
          return;
        }
        const detalle = await operacionesApi.obtenerOrden(
          resumen.id,
          resumen.fechaEmision
        );
        setIndice(i);
        setOrden(detalle);
        setPorBusqueda(false);
        setAnio(new Date(detalle.fechaEmision).getFullYear());
      } catch (e) {
        showToast(mensajeError(e, "No se pudo cargar la orden"), "error");
      } finally {
        setCargando(false);
      }
    },
    [cargarPagina, showToast]
  );

  useEffect(() => {
    void irA(0, true); /* primera carga */
  }, []); // eslint-disable-line react-hooks/exhaustive-deps
  useEffect(() => {
    void cargarPresupuestos(anio);
  }, [anio, cargarPresupuestos]);

  const bodegas = useMemo(
    () => presupuestos.map((p) => p.bodega),
    [presupuestos]
  );

  // ------------------------------------------------------------- nuevo / guardar
  function nuevo() {
    setModo("nuevo");
    setErrores({});
    setErrorServidor(null);
    setAnio(new Date().getFullYear());
    setBorrador(borradorVacio(bodegas[0] ?? ""));
  }

  function cancelarNuevo() {
    setModo("ver");
    setErrorServidor(null);
    if (orden) setAnio(new Date(orden.fechaEmision).getFullYear());
  }

  function validar(): boolean {
    const e: Record<string, string> = {};
    if (!/^[0-9Kk-]{4,20}$/.test(borrador.proveedorNit))
      e.proveedorNit = "NIT invalido (digitos, guion o K)";
    if (borrador.proveedorNombre.trim().length < 3)
      e.proveedorNombre = "Minimo 3 caracteres";
    if (!borrador.bodegaDestino) e.bodegaDestino = "Requerida";
    if (!borrador.fechaEmision) e.fechaEmision = "Requerida";
    else if (borrador.fechaEmision > hoy())
      e.fechaEmision = "No puede ser futura";
    borrador.lineas.forEach((l, i) => {
      if (!l.descripcion.trim()) e[`desc${i}`] = "Requerida";
      if (l.cantidad.trim() === "" || !Number.isFinite(Number(l.cantidad)))
        e[`cant${i}`] = "Numero";
      if (
        l.precioUnitario.trim() === "" ||
        !Number.isFinite(Number(l.precioUnitario))
      )
        e[`precio${i}`] = "Numero";
    });
    if (borrador.lineas.length === 0) e.lineas = "Agregue al menos una linea";
    setErrores(e);
    return Object.keys(e).length === 0;
  }

  async function guardar() {
    if (!validar()) return;
    setGuardando(true);
    setErrorServidor(null);
    try {
      // Las reglas de negocio (cantidad > 0, presupuesto suficiente) las valida el SP: si una
      // sola linea falla, SQL Server hace ROLLBACK y no queda ni el encabezado.
      const creada = await operacionesApi.crearOrden({
        proveedorNit: borrador.proveedorNit.trim(),
        proveedorNombre: borrador.proveedorNombre.trim(),
        moneda: borrador.moneda,
        fechaEmision: borrador.fechaEmision,
        bodegaDestino: borrador.bodegaDestino,
        detalle: borrador.detalle.trim() || undefined,
        lineas: borrador.lineas.map((l) => ({
          descripcion: l.descripcion.trim(),
          cantidad: Number(l.cantidad),
          precioUnitario: Number(l.precioUnitario),
        })),
      });
      showToast(
        `Orden ${creada.numeroOrden} guardada: ${creada.lineas?.length ?? 0} lineas, ${dinero(creada.monto, creada.moneda)}`
      );
      // Se muestra la orden recien creada (la respuesta del POST ya trae lineas y presupuesto),
      // sin depender de su posicion en el listado.
      setModo("ver");
      setOrden(creada);
      setAnio(new Date(creada.fechaEmision).getFullYear());
      const primera = await cargarPagina(0, true);
      const posicion = primera.findIndex((o) => o.id === creada.id);
      setIndice(posicion >= 0 ? posicion : 0);
      setPorBusqueda(posicion < 0);
      await cargarPresupuestos(new Date(creada.fechaEmision).getFullYear());
    } catch (e) {
      setErrorServidor(mensajeError(e, "No se pudo guardar la orden"));
      await cargarPresupuestos(anio); // evidencia: el saldo no cambio
    } finally {
      setGuardando(false);
    }
  }

  // ------------------------------------------------------------- busqueda por numero o Id
  async function buscar() {
    const texto = textoBusqueda.trim();
    if (!texto) return;
    setBuscando(true);
    try {
      let encontrada: OrdenCompra | null = null;
      if (
        /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(
          texto
        )
      ) {
        encontrada = await operacionesApi.obtenerOrden(texto);
      } else {
        const res = await operacionesApi.listarOrdenes({
          numeroOrden: texto,
          page: 1,
          pageSize: 1,
        });
        const resumen = res.items[0];
        if (resumen)
          encontrada = await operacionesApi.obtenerOrden(
            resumen.id,
            resumen.fechaEmision
          );
      }
      if (!encontrada) {
        showToast(`No se encontro la orden ${texto}`, "warning");
        return;
      }
      setModo("ver");
      setOrden(encontrada);
      setAnio(new Date(encontrada.fechaEmision).getFullYear());
      const cargadas = Object.entries(paginas).flatMap(([p, items]) =>
        items.map((o, j) => ({ id: o.id, i: Number(p) * PAGE_SIZE + j }))
      );
      const enNavegacion = cargadas.find((o) => o.id === encontrada!.id);
      if (enNavegacion) setIndice(enNavegacion.i);
      setPorBusqueda(!enNavegacion);
    } catch (e) {
      const status = (e as { response?: { status?: number } })?.response
        ?.status;
      showToast(
        status === 404
          ? `No se encontro la orden ${texto}`
          : mensajeError(e, "No se pudo buscar la orden"),
        status === 404 ? "warning" : "error"
      );
    } finally {
      setBuscando(false);
    }
  }

  // ------------------------------------------------------------- anulaciones (soft delete)
  async function confirmarAnulacion(motivo: string) {
    if (!orden || !dialogo) return;
    try {
      if (dialogo.tipo === "linea") {
        const actualizada = await operacionesApi.anularLinea(
          orden,
          dialogo.linea.linea,
          motivo,
          dialogo.linea.rowVer
        );
        setOrden(actualizada);
        showToast(
          `Linea ${dialogo.linea.linea} anulada: se devolvieron ${dinero(dialogo.linea.importe)} al presupuesto`
        );
      } else {
        const actualizada = await operacionesApi.anularOrden(orden, motivo);
        setOrden(actualizada);
        showToast(`Orden ${actualizada.numeroOrden} anulada.`);
        await cargarPagina(Math.floor(indice / PAGE_SIZE), true);
      }
      setDialogo(null);
      await cargarPresupuestos(anio);
    } catch (e) {
      const status = (e as { response?: { status?: number } })?.response
        ?.status;
      showToast(
        mensajeError(
          e,
          status === 403
            ? "No tiene permisos para esta accion"
            : "No se pudo anular"
        ),
        "error"
      );
      if (status === 409) await irA(indice); // alguien mas la modifico: recargar
    }
  }

  // ------------------------------------------------------------- derivados
  const enNuevo = modo === "nuevo";
  const totalBorrador = useMemo(
    () =>
      redondear(borrador.lineas.reduce((a, l) => a + importeBorrador(l), 0)),
    [borrador.lineas]
  );
  const bodegaActual = enNuevo ? borrador.bodegaDestino : orden?.bodegaDestino;
  const anioActual = enNuevo
    ? Number(borrador.fechaEmision.slice(0, 4))
    : orden
      ? new Date(orden.fechaEmision).getFullYear()
      : anio;
  const saldo =
    presupuestos.find(
      (p) => p.bodega === bodegaActual && p.anio === anioActual
    ) ?? (!enNuevo && orden?.presupuesto ? orden.presupuesto : undefined);
  const ordenAnulable = !!orden && ESTADOS_ANULABLES.includes(orden.estado);

  // El API solo devuelve ids de usuario: el de la sesion se muestra con su nombre.
  const nombreUsuario = (id?: number | null) =>
    id == null
      ? "-"
      : id === user?.id
        ? `${user.username} (#${id})`
        : `usuario #${id}`;

  const set = <K extends keyof Borrador>(k: K, v: Borrador[K]) =>
    setBorrador((b) => ({ ...b, [k]: v }));
  const setLinea = (
    i: number,
    campo: keyof Omit<LineaBorrador, "key">,
    v: string
  ) =>
    setBorrador((b) => ({
      ...b,
      lineas: b.lineas.map((l, j) => (j === i ? { ...l, [campo]: v } : l)),
    }));

  // ------------------------------------------------------------- render
  return (
    <Box>
      <Typography variant="h4" mb={2}>
        Orden de Compra
      </Typography>

      {/* Cabecera del sistema: contexto global que viene del JWT */}
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
        [ Usuario: {user?.username ?? "-"} | Rol:{" "}
        {role ? NOMBRE_ROL[role] : "-"} ({role}) | Nivel: {nivel} | Ejercicio:{" "}
        {EJERCICIO} ]
      </Box>

      {/* Paleta de control transaccional */}
      <Paper
        variant="outlined"
        sx={{
          p: 1.5,
          mb: 2,
          borderColor: industryColors.border,
          "& .MuiButton-root": { whiteSpace: "nowrap" },
        }}
      >
        <Stack
          direction={{ xs: "column", md: "row" }}
          spacing={1}
          alignItems={{ md: "center" }}
        >
          <Stack direction="row" spacing={1}>
            <Button
              variant="outlined"
              size="small"
              startIcon={<ChevronsLeft size={16} />}
              disabled={enNuevo || cargando || indice === 0 || total === 0}
              onClick={() => void irA(0)}
            >
              Primero
            </Button>
            <Button
              variant="outlined"
              size="small"
              startIcon={<ChevronLeft size={16} />}
              disabled={enNuevo || cargando || indice === 0}
              onClick={() => void irA(indice - 1)}
            >
              Anterior
            </Button>
            <Button
              variant="outlined"
              size="small"
              endIcon={<ChevronRight size={16} />}
              disabled={enNuevo || cargando || indice >= total - 1}
              onClick={() => void irA(indice + 1)}
            >
              Siguiente
            </Button>
            <Button
              variant="outlined"
              size="small"
              endIcon={<ChevronsRight size={16} />}
              disabled={enNuevo || cargando || indice >= total - 1}
              onClick={() => void irA(total - 1)}
            >
              Ultimo
            </Button>
          </Stack>
          <Box
            component="form"
            onSubmit={(e) => {
              e.preventDefault();
              void buscar();
            }}
            sx={{ display: "flex", gap: 1, alignItems: "center" }}
          >
            <TextField
              size="small"
              placeholder="No. de orden (OC-2026-000001) o Id"
              value={textoBusqueda}
              onChange={(e) => setTextoBusqueda(e.target.value)}
              disabled={enNuevo}
              sx={{ width: 250 }}
            />
            <Button
              type="submit"
              variant="outlined"
              size="small"
              startIcon={<Search size={16} />}
              disabled={enNuevo || buscando || !textoBusqueda.trim()}
            >
              {buscando ? "Buscando..." : "Buscar"}
            </Button>
          </Box>
          <Box flexGrow={1} />
          <Typography
            variant="body2"
            color="text.secondary"
            sx={{ px: 1, whiteSpace: "nowrap" }}
          >
            {enNuevo
              ? "Orden nueva (sin guardar)"
              : porBusqueda && orden
                ? `Resultado de busqueda: ${orden.numeroOrden}`
                : total > 0
                  ? `Registro ${indice + 1} de ${total}`
                  : "Sin ordenes"}
          </Typography>
          <Stack direction="row" spacing={1}>
            {enNuevo ? (
              <>
                <Button
                  variant="outlined"
                  size="small"
                  startIcon={<X size={16} />}
                  onClick={cancelarNuevo}
                  disabled={guardando}
                >
                  Cancelar
                </Button>
                <Button
                  variant="contained"
                  size="small"
                  startIcon={<Save size={16} />}
                  onClick={() => void guardar()}
                  disabled={guardando}
                >
                  {guardando ? "Guardando..." : "Guardar"}
                </Button>
              </>
            ) : (
              <>
                <Button
                  variant="outlined"
                  size="small"
                  startIcon={<FilePlus size={16} />}
                  onClick={nuevo}
                >
                  Nuevo
                </Button>
                <PermisoBoton permitido={nivel >= 3} requiere="Administrador">
                  <Button
                    variant="contained"
                    color="error"
                    size="small"
                    startIcon={
                      nivel >= 3 ? <Ban size={16} /> : <Lock size={16} />
                    }
                    disabled={nivel < 3 || !ordenAnulable}
                    onClick={() => setDialogo({ tipo: "orden" })}
                  >
                    Anular orden
                  </Button>
                </PermisoBoton>
              </>
            )}
          </Stack>
        </Stack>
      </Paper>

      {cargando && <LinearProgress sx={{ mb: 1 }} />}

      <Grid container spacing={2}>
        {/* ------------------------------ Maestro ------------------------------ */}
        <Grid item xs={12} lg={8}>
          <Paper
            variant="outlined"
            sx={{ p: 2.5, borderColor: industryColors.border, height: "100%" }}
          >
            <Stack direction="row" alignItems="center" spacing={1.5} mb={2}>
              <Typography variant="h6">Encabezado</Typography>
              {!enNuevo && orden && (
                <Chip
                  size="small"
                  label={orden.estado}
                  color={colorEstado[orden.estado]}
                />
              )}
              {enNuevo && (
                <Chip size="small" label="NUEVA" variant="outlined" />
              )}
            </Stack>

            {enNuevo ? (
              <Grid container spacing={1.5}>
                <Grid item xs={12} sm={4}>
                  <TextField
                    label="NIT del proveedor"
                    fullWidth
                    value={borrador.proveedorNit}
                    onChange={(e) => set("proveedorNit", e.target.value)}
                    error={!!errores.proveedorNit}
                    helperText={errores.proveedorNit ?? " "}
                  />
                </Grid>
                <Grid item xs={12} sm={8}>
                  <TextField
                    label="Nombre del proveedor"
                    fullWidth
                    value={borrador.proveedorNombre}
                    onChange={(e) => set("proveedorNombre", e.target.value)}
                    error={!!errores.proveedorNombre}
                    helperText={errores.proveedorNombre ?? " "}
                  />
                </Grid>
                <Grid item xs={12} sm={5}>
                  <TextField
                    label="Bodega destino"
                    select
                    fullWidth
                    value={borrador.bodegaDestino}
                    onChange={(e) => set("bodegaDestino", e.target.value)}
                    error={!!errores.bodegaDestino}
                    helperText={errores.bodegaDestino ?? " "}
                  >
                    {bodegas.map((b) => (
                      <MenuItem key={b} value={b}>
                        {b}
                      </MenuItem>
                    ))}
                  </TextField>
                </Grid>
                <Grid item xs={6} sm={4}>
                  <TextField
                    label="Fecha de emision"
                    type="date"
                    fullWidth
                    InputLabelProps={{ shrink: true }}
                    inputProps={{ max: hoy() }}
                    value={borrador.fechaEmision}
                    onChange={(e) => {
                      set("fechaEmision", e.target.value);
                      if (e.target.value)
                        setAnio(Number(e.target.value.slice(0, 4)));
                    }}
                    error={!!errores.fechaEmision}
                    helperText={errores.fechaEmision ?? " "}
                  />
                </Grid>
                <Grid item xs={6} sm={3}>
                  <TextField
                    label="Moneda"
                    select
                    fullWidth
                    value={borrador.moneda}
                    onChange={(e) => set("moneda", e.target.value as Moneda)}
                    helperText=" "
                  >
                    <MenuItem value="GTQ">GTQ</MenuItem>
                    <MenuItem value="USD">USD</MenuItem>
                  </TextField>
                </Grid>
                <Grid item xs={12}>
                  <TextField
                    label="Observaciones (opcional)"
                    fullWidth
                    value={borrador.detalle}
                    onChange={(e) => set("detalle", e.target.value)}
                  />
                </Grid>
              </Grid>
            ) : orden ? (
              <Grid container spacing={1.5}>
                <Campo
                  label="No. de orden"
                  valor={orden.numeroOrden}
                  xs={6}
                  sm={3}
                />
                <Campo
                  label="Fecha de emision"
                  valor={orden.fechaEmision.slice(0, 10)}
                  xs={6}
                  sm={3}
                />
                <Campo label="Moneda" valor={orden.moneda} xs={6} sm={2} />
                <Campo
                  label="Creada por"
                  valor={nombreUsuario(orden.userId)}
                  xs={6}
                  sm={4}
                />
                <Campo
                  label="Proveedor"
                  valor={`${orden.proveedorNombre} · NIT ${orden.proveedorNit}`}
                  xs={12}
                  sm={8}
                />
                <Campo
                  label="Bodega destino"
                  valor={orden.bodegaDestino}
                  xs={12}
                  sm={4}
                />
                {orden.detalle && (
                  <Campo label="Observaciones" valor={orden.detalle} xs={12} />
                )}
                {orden.estado === "ANULADA" && (
                  <Grid item xs={12}>
                    <Alert severity="error" variant="outlined">
                      <b>Orden anulada</b> el{" "}
                      {orden.fechaAnulacion?.replace("T", " ").slice(0, 16)} por{" "}
                      {nombreUsuario(orden.usuarioAnulaId)}. Motivo:{" "}
                      {orden.motivoAnulacion}.
                    </Alert>
                  </Grid>
                )}
              </Grid>
            ) : (
              <Typography color="text.secondary" py={3}>
                No hay ordenes maestro-detalle todavia. Pulse <b>NUEVO</b> para
                registrar la primera.
              </Typography>
            )}
          </Paper>
        </Grid>

        {/* ------------------------------ Saldo de la bodega ------------------------------ */}
        <Grid item xs={12} lg={4}>
          <PanelSaldo
            saldo={saldo}
            bodega={bodegaActual}
            anio={anioActual}
            proyeccion={enNuevo ? totalBorrador : undefined}
          />
        </Grid>

        {/* ------------------------------ Detalle ------------------------------ */}
        <Grid item xs={12}>
          <Paper
            variant="outlined"
            sx={{ p: 2.5, borderColor: industryColors.border }}
          >
            <Stack
              direction="row"
              alignItems="center"
              justifyContent="space-between"
              mb={1.5}
            >
              <Typography variant="h6">Detalle</Typography>
              <Typography variant="caption" color="text.secondary">
                {enNuevo
                  ? "Capture las lineas; el SP las inserta todas o ninguna"
                  : ""}
              </Typography>
            </Stack>

            {errorServidor && (
              <Alert severity="error" sx={{ mb: 2 }}>
                <b>No se guardo la orden ni ninguna linea (ROLLBACK).</b>{" "}
                {errorServidor}
              </Alert>
            )}
            {errores.lineas && (
              <Alert severity="warning" sx={{ mb: 2 }}>
                {errores.lineas}
              </Alert>
            )}

            <TableContainer>
              <Table size="small">
                <TableHead>
                  <TableRow sx={{ bgcolor: industryColors.accent100 }}>
                    <TableCell width={60}>Linea</TableCell>
                    <TableCell>Descripcion</TableCell>
                    <TableCell align="right" width={140}>
                      Cantidad
                    </TableCell>
                    <TableCell align="right" width={160}>
                      Precio unitario
                    </TableCell>
                    <TableCell align="right" width={150}>
                      Importe
                    </TableCell>
                    <TableCell align="center" width={110}>
                      Estado
                    </TableCell>
                    <TableCell align="center" width={150}>
                      {enNuevo ? "" : "Anular linea"}
                    </TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {enNuevo &&
                    borrador.lineas.map((l, i) => (
                      <TableRow key={l.key}>
                        <TableCell>{i + 1}</TableCell>
                        <TableCell>
                          <TextField
                            fullWidth
                            placeholder="Producto o servicio"
                            value={l.descripcion}
                            onChange={(e) =>
                              setLinea(i, "descripcion", e.target.value)
                            }
                            error={!!errores[`desc${i}`]}
                          />
                        </TableCell>
                        <TableCell>
                          <TextField
                            fullWidth
                            type="number"
                            inputProps={{
                              step: "0.001",
                              style: { textAlign: "right" },
                            }}
                            value={l.cantidad}
                            onChange={(e) =>
                              setLinea(i, "cantidad", e.target.value)
                            }
                            error={!!errores[`cant${i}`]}
                          />
                        </TableCell>
                        <TableCell>
                          <TextField
                            fullWidth
                            type="number"
                            inputProps={{
                              step: "0.01",
                              style: { textAlign: "right" },
                            }}
                            value={l.precioUnitario}
                            onChange={(e) =>
                              setLinea(i, "precioUnitario", e.target.value)
                            }
                            error={!!errores[`precio${i}`]}
                          />
                        </TableCell>
                        <TableCell
                          align="right"
                          sx={{ fontFamily: "monospace" }}
                        >
                          {dinero(importeBorrador(l), borrador.moneda)}
                        </TableCell>
                        <TableCell align="center">
                          <Chip size="small" label="NUEVA" variant="outlined" />
                        </TableCell>
                        <TableCell align="center">
                          <Tooltip title="Quitar del borrador (aun no esta guardada)">
                            <span>
                              <IconButton
                                size="small"
                                disabled={borrador.lineas.length === 1}
                                onClick={() =>
                                  set(
                                    "lineas",
                                    borrador.lineas.filter((_, j) => j !== i)
                                  )
                                }
                              >
                                <Trash2 size={16} />
                              </IconButton>
                            </span>
                          </Tooltip>
                        </TableCell>
                      </TableRow>
                    ))}

                  {!enNuevo &&
                    orden?.lineas?.map((l) => {
                      const tachada = {
                        textDecoration: l.estadoLinea ? "none" : "line-through",
                        color: l.estadoLinea
                          ? "inherit"
                          : industryColors.textMuted,
                      };
                      return (
                        <TableRow
                          key={l.linea}
                          sx={{
                            bgcolor: l.estadoLinea ? undefined : "#f6f6f6",
                          }}
                        >
                          <TableCell sx={tachada}>{l.linea}</TableCell>
                          <TableCell>
                            <Box component="span" sx={tachada}>
                              {l.descripcion}
                            </Box>
                            {!l.estadoLinea && l.motivoBaja && (
                              <Typography
                                variant="caption"
                                display="block"
                                color="error"
                              >
                                Anulada por {nombreUsuario(l.usuarioBajaId)} el{" "}
                                {l.fechaBaja?.replace("T", " ").slice(0, 16)}:{" "}
                                {l.motivoBaja}
                              </Typography>
                            )}
                          </TableCell>
                          <TableCell
                            align="right"
                            sx={{ ...tachada, fontFamily: "monospace" }}
                          >
                            {l.cantidad.toLocaleString("es-GT", {
                              maximumFractionDigits: 3,
                            })}
                          </TableCell>
                          <TableCell
                            align="right"
                            sx={{ ...tachada, fontFamily: "monospace" }}
                          >
                            {dinero(l.precioUnitario, orden.moneda)}
                          </TableCell>
                          <TableCell
                            align="right"
                            sx={{ ...tachada, fontFamily: "monospace" }}
                          >
                            {dinero(l.importe, orden.moneda)}
                          </TableCell>
                          <TableCell align="center">
                            <Chip
                              size="small"
                              label={l.estadoLinea ? "VIGENTE" : "ANULADA"}
                              color={l.estadoLinea ? "success" : "error"}
                              variant="outlined"
                            />
                          </TableCell>
                          <TableCell align="center">
                            {l.estadoLinea && (
                              <PermisoBoton
                                permitido={nivel >= 2}
                                requiere="Intermedio"
                              >
                                <Button
                                  size="small"
                                  variant="outlined"
                                  color="error"
                                  startIcon={
                                    nivel >= 2 ? (
                                      <Ban size={14} />
                                    ) : (
                                      <Lock size={14} />
                                    )
                                  }
                                  disabled={nivel < 2 || !ordenAnulable}
                                  onClick={() =>
                                    setDialogo({ tipo: "linea", linea: l })
                                  }
                                >
                                  Anular
                                </Button>
                              </PermisoBoton>
                            )}
                          </TableCell>
                        </TableRow>
                      );
                    })}

                  {!enNuevo && !orden && (
                    <TableRow>
                      <TableCell colSpan={7} align="center">
                        <Typography
                          variant="body2"
                          color="text.secondary"
                          py={2}
                        >
                          Sin lineas
                        </Typography>
                      </TableCell>
                    </TableRow>
                  )}
                </TableBody>
              </Table>
            </TableContainer>

            <Stack
              direction={{ xs: "column", sm: "row" }}
              justifyContent="space-between"
              alignItems={{ sm: "center" }}
              mt={1.5}
              spacing={1}
            >
              {enNuevo ? (
                <Button
                  size="small"
                  startIcon={<Plus size={16} />}
                  onClick={() =>
                    set("lineas", [...borrador.lineas, lineaVacia()])
                  }
                >
                  Agregar linea
                </Button>
              ) : (
                <Box />
              )}
              <Box textAlign="right">
                <Typography variant="caption" color="text.secondary">
                  {enNuevo ? "Total del borrador" : "Total de la orden"}
                </Typography>
                <Typography variant="h5" sx={{ fontFamily: "monospace" }}>
                  {enNuevo
                    ? dinero(totalBorrador, borrador.moneda)
                    : dinero(orden?.monto, orden?.moneda)}
                </Typography>
              </Box>
            </Stack>
          </Paper>
        </Grid>

        {/* ------------------------------ Saldos de todas las bodegas ------------------------------ */}
        <Grid item xs={12}>
          <TablaSaldos
            anio={anio}
            onAnio={setAnio}
            presupuestos={presupuestos}
            resaltar={bodegaActual}
            onRefrescar={() => void cargarPresupuestos(anio)}
          />
        </Grid>
      </Grid>

      <DialogoMotivo
        abierto={!!dialogo}
        titulo={
          dialogo?.tipo === "orden"
            ? `Anular orden ${orden?.numeroOrden}`
            : `Anular linea ${dialogo?.tipo === "linea" ? dialogo.linea.linea : ""}`
        }
        descripcion={
          dialogo?.tipo === "orden"
            ? `La orden quedará ANULADA y se liberarán ${dinero(orden?.monto)} del presupuesto de ${orden?.bodegaDestino}.`
            : dialogo?.tipo === "linea"
              ? `La línea quedará anulada y se devolverán ${dinero(dialogo.linea.importe)} al presupuesto de ${orden?.bodegaDestino}.`
              : ""
        }
        onCancelar={() => setDialogo(null)}
        onConfirmar={confirmarAnulacion}
      />
    </Box>
  );
}

// ---------------------------------------------------------------------------
// Componentes de apoyo
// ---------------------------------------------------------------------------

function Campo({
  label,
  valor,
  xs,
  sm,
}: {
  label: string;
  valor: string;
  xs: number;
  sm?: number;
}) {
  return (
    <Grid item xs={xs} sm={sm}>
      <Typography variant="caption" color="text.secondary">
        {label}
      </Typography>
      <Box
        sx={{
          bgcolor: industryColors.bg,
          border: `1px solid ${industryColors.border}`,
          px: 1.25,
          py: 0.75,
          fontSize: 14,
          minHeight: 34,
        }}
      >
        {valor}
      </Box>
    </Grid>
  );
}

/** El boton se muestra siempre; si el rol no alcanza queda deshabilitado con candado y tooltip. */
function PermisoBoton({
  permitido,
  requiere,
  children,
}: {
  permitido: boolean;
  requiere: string;
  children: ReactNode;
}) {
  if (permitido) return <>{children}</>;
  return (
    <Tooltip title={`Requiere rol ${requiere} o superior`}>
      <span>{children}</span>
    </Tooltip>
  );
}

function PanelSaldo({
  saldo,
  bodega,
  anio,
  proyeccion,
}: {
  saldo?: PresupuestoBodega;
  bodega?: string;
  anio: number;
  proyeccion?: number;
}) {
  const pct =
    saldo && saldo.montoAsignado > 0
      ? Math.min(100, (saldo.montoComprometido / saldo.montoAsignado) * 100)
      : 0;
  const despues =
    saldo && proyeccion !== undefined
      ? redondear(saldo.montoDisponible - proyeccion)
      : undefined;
  const sobregiro = despues !== undefined && despues < 0;

  return (
    <Blueprint sx={{ bgcolor: "background.paper", p: 2.5, height: "100%" }}>
      <Stack direction="row" alignItems="center" spacing={1} mb={1.5}>
        <Wallet size={18} strokeWidth={1.5} />
        <Typography variant="h6">Saldo de la bodega</Typography>
      </Stack>
      {!bodega ? (
        <Typography variant="body2" color="text.secondary">
          Seleccione una orden o una bodega.
        </Typography>
      ) : !saldo ? (
        <Alert severity="warning">
          Sin presupuesto para {bodega} en {anio}: el SP rechazara la orden.
        </Alert>
      ) : (
        <>
          <Typography variant="body2" color="text.secondary" mb={1}>
            {bodega} · {anio}
          </Typography>
          <Fila label="Asignado" valor={dinero(saldo.montoAsignado)} />
          <Fila label="Comprometido" valor={dinero(saldo.montoComprometido)} />
          <LinearProgress
            variant="determinate"
            value={pct}
            sx={{ my: 1, height: 8, bgcolor: industryColors.accent100 }}
          />
          <Fila
            label="Disponible"
            valor={dinero(saldo.montoDisponible)}
            fuerte
            color={industryColors.success}
          />
          {despues !== undefined && (
            <Box
              mt={1.5}
              p={1.25}
              sx={{ bgcolor: sobregiro ? "#fbecea" : industryColors.accent100 }}
            >
              <Fila label="Esta orden" valor={`- ${dinero(proyeccion)}`} />
              <Fila
                label="Disponible al guardar"
                valor={dinero(despues)}
                fuerte
                color={
                  sobregiro ? industryColors.danger : industryColors.accent700
                }
              />
              {sobregiro && (
                <Typography variant="caption" color="error">
                  Excede el presupuesto disponible de la bodega; la orden no se
                  podrá guardar.
                </Typography>
              )}
            </Box>
          )}
        </>
      )}
    </Blueprint>
  );
}

function Fila({
  label,
  valor,
  fuerte,
  color,
}: {
  label: string;
  valor: string;
  fuerte?: boolean;
  color?: string;
}) {
  return (
    <Stack direction="row" justifyContent="space-between" py={0.25}>
      <Typography variant="body2" color="text.secondary">
        {label}
      </Typography>
      <Typography
        variant="body2"
        sx={{ fontFamily: "monospace", fontWeight: fuerte ? 700 : 400, color }}
      >
        {valor}
      </Typography>
    </Stack>
  );
}

function TablaSaldos({
  anio,
  onAnio,
  presupuestos,
  resaltar,
  onRefrescar,
}: {
  anio: number;
  onAnio: (a: number) => void;
  presupuestos: PresupuestoBodega[];
  resaltar?: string;
  onRefrescar: () => void;
}) {
  const totales = presupuestos.reduce(
    (a, p) => ({
      asignado: a.asignado + p.montoAsignado,
      comprometido: a.comprometido + p.montoComprometido,
      disponible: a.disponible + p.montoDisponible,
    }),
    { asignado: 0, comprometido: 0, disponible: 0 }
  );
  return (
    <Paper
      variant="outlined"
      sx={{ p: 2.5, borderColor: industryColors.border }}
    >
      <Stack
        direction="row"
        alignItems="center"
        justifyContent="space-between"
        mb={1.5}
      >
        <Typography variant="h6">Saldos por bodega</Typography>
        <Stack direction="row" spacing={1} alignItems="center">
          <TextField
            select
            size="small"
            value={anio}
            onChange={(e) => onAnio(Number(e.target.value))}
            sx={{ width: 110 }}
          >
            {ANIOS.map((a) => (
              <MenuItem key={a} value={a}>
                {a}
              </MenuItem>
            ))}
          </TextField>
          <Button
            size="small"
            startIcon={<RefreshCw size={16} />}
            onClick={onRefrescar}
          >
            Refrescar
          </Button>
        </Stack>
      </Stack>
      <TableContainer>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Bodega</TableCell>
              <TableCell align="right">Asignado</TableCell>
              <TableCell align="right">Comprometido</TableCell>
              <TableCell align="right">Disponible</TableCell>
              <TableCell width={200}>Uso</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {presupuestos.map((p) => {
              const pct =
                p.montoAsignado > 0
                  ? (p.montoComprometido / p.montoAsignado) * 100
                  : 0;
              return (
                <TableRow
                  key={`${p.anio}-${p.bodega}`}
                  sx={{
                    bgcolor:
                      p.bodega === resaltar
                        ? industryColors.accent100
                        : undefined,
                  }}
                >
                  <TableCell
                    sx={{ fontWeight: p.bodega === resaltar ? 700 : 400 }}
                  >
                    {p.bodega}
                  </TableCell>
                  <TableCell align="right" sx={{ fontFamily: "monospace" }}>
                    {dinero(p.montoAsignado)}
                  </TableCell>
                  <TableCell align="right" sx={{ fontFamily: "monospace" }}>
                    {dinero(p.montoComprometido)}
                  </TableCell>
                  <TableCell
                    align="right"
                    sx={{ fontFamily: "monospace", fontWeight: 600 }}
                  >
                    {dinero(p.montoDisponible)}
                  </TableCell>
                  <TableCell>
                    <Stack direction="row" alignItems="center" spacing={1}>
                      <LinearProgress
                        variant="determinate"
                        value={Math.min(100, pct)}
                        sx={{
                          flexGrow: 1,
                          height: 6,
                          bgcolor: industryColors.accent100,
                        }}
                      />
                      <Typography
                        variant="caption"
                        sx={{ width: 44, textAlign: "right" }}
                      >
                        {pct.toFixed(pct > 0 && pct < 1 ? 2 : 0)}%
                      </Typography>
                    </Stack>
                  </TableCell>
                </TableRow>
              );
            })}
            {presupuestos.length === 0 && (
              <TableRow>
                <TableCell colSpan={5} align="center">
                  <Typography variant="body2" color="text.secondary" py={2}>
                    Sin presupuesto para {anio}
                  </Typography>
                </TableCell>
              </TableRow>
            )}
            {presupuestos.length > 0 && (
              <TableRow>
                <TableCell sx={{ fontWeight: 700 }}>Total {anio}</TableCell>
                <TableCell
                  align="right"
                  sx={{ fontFamily: "monospace", fontWeight: 700 }}
                >
                  {dinero(totales.asignado)}
                </TableCell>
                <TableCell
                  align="right"
                  sx={{ fontFamily: "monospace", fontWeight: 700 }}
                >
                  {dinero(totales.comprometido)}
                </TableCell>
                <TableCell
                  align="right"
                  sx={{ fontFamily: "monospace", fontWeight: 700 }}
                >
                  {dinero(totales.disponible)}
                </TableCell>
                <TableCell />
              </TableRow>
            )}
          </TableBody>
        </Table>
      </TableContainer>
    </Paper>
  );
}

function DialogoMotivo({
  abierto,
  titulo,
  descripcion,
  auditoria,
  onCancelar,
  onConfirmar,
}: {
  abierto: boolean;
  titulo: string;
  descripcion: string;
  auditoria?: string;
  onCancelar: () => void;
  onConfirmar: (motivo: string) => Promise<void>;
}) {
  const [motivo, setMotivo] = useState("");
  const [enviando, setEnviando] = useState(false);
  useEffect(() => {
    if (abierto) setMotivo("");
  }, [abierto]);
  const valido = motivo.trim().length >= 3 && motivo.trim().length <= 200;

  return (
    <Dialog
      open={abierto}
      onClose={enviando ? undefined : onCancelar}
      maxWidth="sm"
      fullWidth
    >
      <DialogTitle>{titulo}</DialogTitle>
      <DialogContent>
        <Typography variant="body2" mb={2}>
          {descripcion}
        </Typography>
        <TextField
          label="Motivo (obligatorio)"
          fullWidth
          multiline
          minRows={2}
          autoFocus
          value={motivo}
          onChange={(e) => setMotivo(e.target.value)}
          helperText="Entre 3 y 200 caracteres"
        />
        {auditoria && (
          <Box
            mt={2}
            p={1.5}
            sx={{
              bgcolor: industryColors.bg,
              border: `1px solid ${industryColors.border}`,
              fontFamily: "monospace",
              fontSize: 12,
            }}
          >
            Quedará registrado en la auditoría: {auditoria}
          </Box>
        )}
      </DialogContent>
      <DialogActions sx={{ p: 2 }}>
        <Button color="inherit" onClick={onCancelar} disabled={enviando}>
          Cancelar
        </Button>
        <Button
          variant="contained"
          color="error"
          disabled={!valido || enviando}
          onClick={async () => {
            setEnviando(true);
            try {
              await onConfirmar(motivo.trim());
            } finally {
              setEnviando(false);
            }
          }}
        >
          {enviando ? "Anulando..." : "Confirmar anulacion"}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
