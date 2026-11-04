import { useCallback, useEffect, useMemo, useState } from "react";
import {
  Box, Typography, Grid, Paper, TextField, MenuItem, Button, Table, TableBody,
  TableCell, TableContainer, TableHead, TableRow, TablePagination, Chip, Divider,
  FormControlLabel, Switch, Stack, LinearProgress,
} from "@mui/material";
import { PackagePlus, Database, RefreshCw, Zap } from "lucide-react";
import { useAuth } from "../../auth/AuthContext";
import { useToast } from "../../components/common/ToastProvider";
import { industryColors } from "../../theme/industryTheme";
import { operacionesApi } from "../../api/operacionesApi";
import {
  CreateOrdenCompra, Moneda, OrdenCompra, OrdenEstado, ParticionInfo,
} from "../../types";

const ESTADOS: OrdenEstado[] = ["CREADA", "EMITIDA", "RECIBIDA", "PAGADA", "ANULADA"];
const MONEDAS: Moneda[] = ["GTQ", "USD"];

const BODEGAS = [
  "Bodega Central Zona 12",
  "Centro de Distribucion Villa Nueva",
  "Bodega Norte Chimaltenango",
  "Bodega Regional Quetzaltenango",
  "Cross-dock Puerto Barrios",
];

const hoy = () => new Date().toISOString().slice(0, 10);

const formularioVacio: CreateOrdenCompra = {
  proveedorNit: "",
  proveedorNombre: "",
  monto: 0,
  moneda: "GTQ",
  fechaEmision: hoy(),
  bodegaDestino: BODEGAS[0],
  detalle: "",
};

const colorEstado: Record<OrdenEstado, "default" | "info" | "success" | "error" | "warning"> = {
  CREADA: "warning",
  EMITIDA: "info",
  RECIBIDA: "default",
  PAGADA: "success",
  ANULADA: "error",
};

export default function Operaciones() {
  const { user, role } = useAuth();
  const { showToast } = useToast();

  const [form, setForm] = useState<CreateOrdenCompra>(formularioVacio);
  const [guardando, setGuardando] = useState(false);
  const [errores, setErrores] = useState<Record<string, string>>({});

  const [ordenes, setOrdenes] = useState<OrdenCompra[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(10);
  const [cargando, setCargando] = useState(false);

  const [desde, setDesde] = useState("");
  const [hasta, setHasta] = useState("");
  const [filtroEstado, setFiltroEstado] = useState("");
  const [soloMias, setSoloMias] = useState(false);

  const [particiones, setParticiones] = useState<ParticionInfo[]>([]);
  const [cantidadSeed, setCantidadSeed] = useState(50000);
  const [sembrando, setSembrando] = useState(false);

  const esAdmin = role === "ADMIN";

  const cargarOrdenes = useCallback(async () => {
    setCargando(true);
    try {
      const data = await operacionesApi.listarOrdenes({
        desde: desde || undefined,
        hasta: hasta || undefined,
        estado: filtroEstado || undefined,
        soloMias,
        page: page + 1,
        pageSize,
      });
      setOrdenes(data.items);
      setTotal(data.total);
    } catch {
      showToast("No se pudieron cargar las ordenes", "error");
    } finally {
      setCargando(false);
    }
  }, [desde, hasta, filtroEstado, soloMias, page, pageSize, showToast]);

  const cargarParticiones = useCallback(async () => {
    try {
      setParticiones(await operacionesApi.getParticiones());
    } catch {
      // La tabla puede no existir todavia si no se corrieron las migraciones.
      setParticiones([]);
    }
  }, []);

  useEffect(() => { void cargarOrdenes(); }, [cargarOrdenes]);
  useEffect(() => { void cargarParticiones(); }, [cargarParticiones]);

  const totalFilas = useMemo(
    () => particiones.reduce((acc, p) => acc + p.registrosEnEsteFragmento, 0),
    [particiones],
  );

  function validar(): boolean {
    const e: Record<string, string> = {};
    if (!/^[0-9Kk-]{4,20}$/.test(form.proveedorNit)) e.proveedorNit = "NIT invalido (digitos, guion o K)";
    if (form.proveedorNombre.trim().length < 3) e.proveedorNombre = "Minimo 3 caracteres";
    if (!((form.monto ?? 0) > 0)) e.monto = "El monto debe ser mayor a cero";
    if (!form.fechaEmision) e.fechaEmision = "Requerida";
    else if (form.fechaEmision > hoy()) e.fechaEmision = "No puede ser futura";
    if (form.bodegaDestino.trim().length < 2) e.bodegaDestino = "Requerida";
    setErrores(e);
    return Object.keys(e).length === 0;
  }

  async function guardar(evento: React.FormEvent) {
    evento.preventDefault();
    if (!validar()) return;

    setGuardando(true);
    try {
      const orden = await operacionesApi.crearOrden({ ...form, monto: Number(form.monto) });
      showToast(`Orden ${orden.numeroOrden} registrada`, "success");
      setForm({ ...formularioVacio, fechaEmision: hoy() });
      setPage(0);
      await Promise.all([cargarOrdenes(), cargarParticiones()]);
    } catch (error: any) {
      const detalle = error?.response?.data?.errors?.[0] ?? error?.response?.data?.message;
      showToast(detalle ?? "No se pudo registrar la orden", "error");
    } finally {
      setGuardando(false);
    }
  }

  async function inyectarData() {
    setSembrando(true);
    try {
      const resultado = await operacionesApi.inyectarData({
        cantidad: cantidadSeed,
        anioInicial: 2024,
        anioFinal: 2026,
      });
      setParticiones(resultado.particiones);
      showToast(
        `${resultado.registrosInsertados.toLocaleString()} órdenes generadas en ${(resultado.duracionMs / 1000).toFixed(1)} s`,
        "success",
      );
      await cargarOrdenes();
    } catch {
      showToast("No se pudieron generar las órdenes", "error");
    } finally {
      setSembrando(false);
    }
  }

  const campo = (name: keyof CreateOrdenCompra) => ({
    value: (form[name] ?? "") as string | number,
    onChange: (e: React.ChangeEvent<HTMLInputElement>) =>
      setForm((f) => ({ ...f, [name]: e.target.value })),
    error: Boolean(errores[name]),
    helperText: errores[name] ?? " ",
    size: "small" as const,
    fullWidth: true,
  });

  return (
    <Box>
      <Typography variant="h4" mb={0.5}>Dashboard de Operaciones</Typography>
      <Typography variant="body1" color="text.secondary" mb={3}>
        Ordenes de compra a proveedores &mdash; {user?.firstName}, las operaciones se registran a su nombre desde el token de sesion.
      </Typography>

      <Grid container spacing={2}>
        {/* ------------------------- Formulario ------------------------- */}
        <Grid item xs={12} lg={5}>
          <Paper variant="outlined" sx={{ p: 3, borderColor: industryColors.border }}>
            <Stack direction="row" alignItems="center" spacing={1} mb={2}>
              <PackagePlus size={18} strokeWidth={1.5} />
              <Typography variant="h6">Registrar orden de compra</Typography>
            </Stack>

            <Box component="form" onSubmit={guardar} noValidate>
              <Grid container spacing={1.5}>
                <Grid item xs={12} sm={5}>
                  <TextField label="NIT del proveedor" {...campo("proveedorNit")} />
                </Grid>
                <Grid item xs={12} sm={7}>
                  <TextField label="Nombre del proveedor" {...campo("proveedorNombre")} />
                </Grid>
                <Grid item xs={12} sm={5}>
                  <TextField label="Monto" type="number" inputProps={{ step: "0.01", min: "0" }} {...campo("monto")} />
                </Grid>
                <Grid item xs={12} sm={3}>
                  <TextField label="Moneda" select {...campo("moneda")}>
                    {MONEDAS.map((m) => <MenuItem key={m} value={m}>{m}</MenuItem>)}
                  </TextField>
                </Grid>
                <Grid item xs={12} sm={4}>
                  <TextField
                    label="Fecha de emision"
                    type="date"
                    InputLabelProps={{ shrink: true }}
                    inputProps={{ max: hoy() }}
                    {...campo("fechaEmision")}
                  />
                </Grid>
                <Grid item xs={12}>
                  <TextField label="Bodega destino" select {...campo("bodegaDestino")}>
                    {BODEGAS.map((b) => <MenuItem key={b} value={b}>{b}</MenuItem>)}
                  </TextField>
                </Grid>
                <Grid item xs={12}>
                  <TextField label="Detalle (opcional)" multiline rows={2} {...campo("detalle")} />
                </Grid>
              </Grid>

              <Button type="submit" variant="contained" disabled={guardando} sx={{ mt: 1, textTransform: "none" }}>
                {guardando ? "Registrando..." : "Registrar orden"}
              </Button>
            </Box>
          </Paper>
        </Grid>

        {/* ------------------- Auditoria de particiones ------------------ */}
        <Grid item xs={12} lg={7}>
          <Paper variant="outlined" sx={{ p: 3, borderColor: industryColors.border }}>
            <Stack direction="row" alignItems="center" justifyContent="space-between" mb={2}>
              <Stack direction="row" alignItems="center" spacing={1}>
                <Database size={18} strokeWidth={1.5} />
                <Typography variant="h6">Órdenes por período</Typography>
              </Stack>
              <Button
                onClick={() => void cargarParticiones()}
                startIcon={<RefreshCw size={16} strokeWidth={1.5} />}
                size="small"
                sx={{ textTransform: "none" }}
              >
                Refrescar
              </Button>
            </Stack>

            <TableContainer>
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>#</TableCell>
                    <TableCell>Período</TableCell>
                    <TableCell align="right">Registros</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {particiones.map((p) => (
                    <TableRow key={p.numeroParticion}>
                      <TableCell>{p.numeroParticion}</TableCell>
                      <TableCell>{p.rangoDesde} &rarr; {p.rangoHasta}</TableCell>
                      <TableCell align="right">{p.registrosEnEsteFragmento.toLocaleString()}</TableCell>
                    </TableRow>
                  ))}
                  {particiones.length === 0 && (
                    <TableRow>
                      <TableCell colSpan={3} align="center">
                        <Typography variant="body2" color="text.secondary" py={2}>
                          Sin datos disponibles.
                        </Typography>
                      </TableCell>
                    </TableRow>
                  )}
                </TableBody>
              </Table>
            </TableContainer>

            {particiones.length > 0 && (
              <Typography variant="caption" color="text.secondary">
                Total: {totalFilas.toLocaleString()} órdenes en {particiones.length} períodos.
              </Typography>
            )}

            {esAdmin && (
              <>
                <Divider sx={{ my: 2 }} />
                <Stack direction="row" alignItems="center" spacing={1} mb={1.5}>
                  <Zap size={18} strokeWidth={1.5} />
                  <Typography variant="subtitle1">Datos de prueba</Typography>
                </Stack>
                <Stack direction={{ xs: "column", sm: "row" }} spacing={1.5} alignItems="flex-start">
                  <TextField
                    label="Órdenes a generar"
                    type="number"
                    size="small"
                    value={cantidadSeed}
                    onChange={(e) => setCantidadSeed(Number(e.target.value))}
                    inputProps={{ min: 1, max: 500000, step: 10000 }}
                    sx={{ width: 200 }}
                  />
                  <Button
                    variant="outlined"
                    onClick={() => void inyectarData()}
                    disabled={sembrando}
                    sx={{ textTransform: "none", mt: 0.25 }}
                  >
                    {sembrando ? "Generando..." : "Generar órdenes"}
                  </Button>
                </Stack>
                {sembrando && <LinearProgress sx={{ mt: 1.5 }} />}
                <Typography variant="caption" color="text.secondary" display="block" mt={1}>
                  Genera órdenes con fechas aleatorias entre 2024 y 2026.
                </Typography>
              </>
            )}
          </Paper>
        </Grid>

        {/* --------------------------- Listado --------------------------- */}
        <Grid item xs={12}>
          <Paper variant="outlined" sx={{ p: 3, borderColor: industryColors.border }}>
            <Typography variant="h6" mb={2}>Ordenes registradas</Typography>

            <Stack direction={{ xs: "column", md: "row" }} spacing={1.5} mb={2} alignItems={{ md: "center" }}>
              <TextField
                label="Desde" type="date" size="small" InputLabelProps={{ shrink: true }}
                value={desde} onChange={(e) => { setDesde(e.target.value); setPage(0); }}
              />
              <TextField
                label="Hasta" type="date" size="small" InputLabelProps={{ shrink: true }}
                value={hasta} onChange={(e) => { setHasta(e.target.value); setPage(0); }}
              />
              <TextField
                label="Estado" select size="small" sx={{ minWidth: 150 }}
                value={filtroEstado} onChange={(e) => { setFiltroEstado(e.target.value); setPage(0); }}
              >
                <MenuItem value="">Todos</MenuItem>
                {ESTADOS.map((e) => <MenuItem key={e} value={e}>{e}</MenuItem>)}
              </TextField>
              <FormControlLabel
                control={<Switch checked={soloMias} onChange={(e) => { setSoloMias(e.target.checked); setPage(0); }} />}
                label="Solo mis ordenes"
              />
            </Stack>

            {cargando && <LinearProgress sx={{ mb: 1 }} />}

            <TableContainer>
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>Numero</TableCell>
                    <TableCell>Fecha</TableCell>
                    <TableCell>Proveedor</TableCell>
                    <TableCell align="right">Monto</TableCell>
                    <TableCell>Estado</TableCell>
                    <TableCell>Bodega</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {ordenes.map((o) => (
                    <TableRow key={o.id} hover>
                      <TableCell>{o.numeroOrden}</TableCell>
                      <TableCell>{o.fechaEmision.slice(0, 10)}</TableCell>
                      <TableCell>
                        {o.proveedorNombre}
                        <Typography variant="caption" color="text.secondary" display="block">{o.proveedorNit}</Typography>
                      </TableCell>
                      <TableCell align="right">
                        {o.moneda} {o.monto.toLocaleString(undefined, { minimumFractionDigits: 2 })}
                      </TableCell>
                      <TableCell><Chip size="small" label={o.estado} color={colorEstado[o.estado]} /></TableCell>
                      <TableCell>{o.bodegaDestino}</TableCell>
                    </TableRow>
                  ))}
                  {!cargando && ordenes.length === 0 && (
                    <TableRow>
                      <TableCell colSpan={6} align="center">
                        <Typography variant="body2" color="text.secondary" py={3}>
                          No hay ordenes para los filtros seleccionados.
                        </Typography>
                      </TableCell>
                    </TableRow>
                  )}
                </TableBody>
              </Table>
            </TableContainer>

            <TablePagination
              component="div"
              count={total}
              page={page}
              onPageChange={(_, p) => setPage(p)}
              rowsPerPage={pageSize}
              onRowsPerPageChange={(e) => { setPageSize(Number(e.target.value)); setPage(0); }}
              rowsPerPageOptions={[10, 25, 50, 100]}
              labelRowsPerPage="Filas por pagina"
            />
          </Paper>
        </Grid>
      </Grid>
    </Box>
  );
}
