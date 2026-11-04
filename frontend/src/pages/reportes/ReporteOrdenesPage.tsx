import { FormEvent, useRef, useState } from "react";
import {
  Alert, Box, Button, Chip, Grid, LinearProgress, MenuItem, Paper, Stack, Table, TableBody, TableCell,
  TableContainer, TableHead, TablePagination, TableRow, TextField, Typography,
} from "@mui/material";
import { Download, FileBarChart, Search } from "lucide-react";
import { mensajeError, reportesApi } from "../../api/reportesApi";
import { industryColors } from "../../theme/industryTheme";
import { ReporteOrdenesQuery, ReporteOrdenesResult } from "../../types";
import { BODEGAS, BarraContexto, ESTADOS, colorEstado, dinero, entero, formatoFecha, statusHttp } from "./comunes";

const TAMANOS_PAGINA = [25, 50, 100, 200];
const MAX_DIAS = 366;
const MSG_RANGO_OBLIGATORIO =
  "Debe indicar el rango de fechas (desde y hasta) para ejecutar el reporte.";

interface Filtros {
  fechaDesde: string;
  fechaHasta: string;
  proveedorNit: string;
  estado: string;
  bodega: string;
}

const filtrosVacios: Filtros = { fechaDesde: "", fechaHasta: "", proveedorNit: "", estado: "", bodega: "" };

type ErroresFiltros = Partial<Record<"fechaDesde" | "fechaHasta", string>>;

/** Dias entre dos fechas yyyy-MM-dd (en UTC para que el horario de verano no altere el resultado). */
const diasEntre = (desde: string, hasta: string) =>
  (Date.parse(`${hasta}T00:00:00Z`) - Date.parse(`${desde}T00:00:00Z`)) / 86_400_000;

function validar(f: Filtros): ErroresFiltros {
  if (!f.fechaDesde || !f.fechaHasta) {
    return {
      ...(!f.fechaDesde && { fechaDesde: "Requerida" }),
      ...(!f.fechaHasta && { fechaHasta: "Requerida" }),
    };
  }
  if (f.fechaDesde > f.fechaHasta) return { fechaDesde: "La fecha desde no puede ser posterior a la fecha hasta", fechaHasta: " " };
  if (diasEntre(f.fechaDesde, f.fechaHasta) > MAX_DIAS) {
    return { fechaHasta: `El rango no puede superar ${MAX_DIAS} días`, fechaDesde: " " };
  }
  return {};
}

function exportarCsv(res: ReporteOrdenesResult) {
  const comilla = String.fromCharCode(34);
  const celda = (v: string | number) => comilla + String(v).split(comilla).join(comilla + comilla) + comilla;
  const encabezado = ["Número de orden", "Fecha de emisión", "NIT", "Proveedor", "Bodega destino", "Estado", "Moneda", "Monto", "Líneas vigentes", "Líneas anuladas"];
  const filas = res.items.map((o) => [
    o.numeroOrden, formatoFecha(o.fechaEmision), o.proveedorNit, o.proveedorNombre, o.bodegaDestino,
    o.estado, o.moneda, o.monto.toFixed(2), o.lineasVigentes, o.lineasAnuladas,
  ]);
  const csv = [encabezado, ...filas].map((f) => f.map(celda).join(",")).join("\r\n");
  const blob = new Blob(["﻿" + csv], { type: "text/csv;charset=utf-8" });
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = `reporte-ordenes-p${res.page}.csv`;
  a.click();
  URL.revokeObjectURL(url);
}

export default function ReporteOrdenesPage() {
  const [form, setForm] = useState<Filtros>(filtrosVacios);
  const [errores, setErrores] = useState<ErroresFiltros>({});
  const [alerta, setAlerta] = useState<string | null>(null);

  /** Filtros con los que se ejecuto la ultima busqueda: la paginacion los reutiliza aunque el formulario cambie. */
  const [aplicados, setAplicados] = useState<Filtros | null>(null);
  const [resultado, setResultado] = useState<ReporteOrdenesResult | null>(null);
  const [pagina, setPagina] = useState(0);
  const [tamano, setTamano] = useState(50);
  const [cargando, setCargando] = useState(false);
  const [errorServidor, setErrorServidor] = useState<string | null>(null);
  const solicitud = useRef(0);

  const set = <K extends keyof Filtros>(k: K, v: Filtros[K]) => {
    setForm((f) => ({ ...f, [k]: v }));
    if (k === "fechaDesde" || k === "fechaHasta") {
      setErrores({});
      setAlerta(null);
    }
  };

  async function ejecutar(filtros: Filtros, page: number, pageSize: number) {
    const id = ++solicitud.current;
    setCargando(true);
    setErrorServidor(null);
    const query: ReporteOrdenesQuery = {
      fechaDesde: filtros.fechaDesde,
      fechaHasta: filtros.fechaHasta,
      proveedorNit: filtros.proveedorNit.trim(),
      estado: filtros.estado,
      bodega: filtros.bodega,
      page,
      pageSize,
    };
    try {
      const res = await reportesApi.ordenes(query);
      if (id !== solicitud.current) return;
      setResultado(res);
      setAplicados(filtros);
      setPagina(page - 1);
      setTamano(pageSize);
    } catch (e) {
      if (id !== solicitud.current) return;
      setResultado(null);
      setErrorServidor(
        mensajeError(e, statusHttp(e) === 400 ? "La consulta no es válida." : "No se pudo ejecutar el reporte."),
      );
    } finally {
      if (id === solicitud.current) setCargando(false);
    }
  }

  function buscar(e: FormEvent) {
    e.preventDefault();
    const errs = validar(form);
    setErrores(errs);
    setErrorServidor(null);
    if (Object.keys(errs).length > 0) {
      // Se bloquea la consulta: no se llama al API sin un rango de fechas valido.
      setAlerta(!form.fechaDesde || !form.fechaHasta ? MSG_RANGO_OBLIGATORIO : Object.values(errs).find((m) => m.trim()) ?? null);
      return;
    }
    setAlerta(null);
    void ejecutar(form, 1, tamano);
  }

  const items = resultado?.items ?? [];

  return (
    <Box>
      <Typography variant="h4" mb={0.5}>Reporte de órdenes</Typography>
      <Typography variant="body1" color="text.secondary" mb={2}>
        Consulta de órdenes de compra por rango de fechas.
      </Typography>

      <BarraContexto />

      <Paper variant="outlined" sx={{ p: 2.5, mb: 2, borderColor: industryColors.border }}>
        <Stack direction="row" alignItems="center" spacing={1} mb={2}>
          <FileBarChart size={18} strokeWidth={1.5} />
          <Typography variant="h6">Filtros</Typography>
        </Stack>

        {alerta && <Alert severity="error" sx={{ mb: 2 }}>{alerta}</Alert>}

        <Box component="form" onSubmit={buscar} noValidate>
          <Grid container spacing={1.5}>
            <Grid item xs={12} sm={6} md={2.4}>
              <TextField
                label="Fecha desde *" type="date" size="small" fullWidth value={form.fechaDesde}
                onChange={(e) => set("fechaDesde", e.target.value)} InputLabelProps={{ shrink: true }}
                error={!!errores.fechaDesde} helperText={errores.fechaDesde ?? " "}
              />
            </Grid>
            <Grid item xs={12} sm={6} md={2.4}>
              <TextField
                label="Fecha hasta *" type="date" size="small" fullWidth value={form.fechaHasta}
                onChange={(e) => set("fechaHasta", e.target.value)} InputLabelProps={{ shrink: true }}
                error={!!errores.fechaHasta} helperText={errores.fechaHasta ?? " "}
              />
            </Grid>
            <Grid item xs={12} sm={4} md={2.4}>
              <TextField
                label="NIT proveedor" size="small" fullWidth value={form.proveedorNit}
                onChange={(e) => set("proveedorNit", e.target.value)} helperText=" "
              />
            </Grid>
            <Grid item xs={12} sm={4} md={2.4}>
              <TextField
                label="Estado" select size="small" fullWidth value={form.estado} onChange={(e) => set("estado", e.target.value)}
                helperText=" " SelectProps={{ displayEmpty: true }} InputLabelProps={{ shrink: true }}
              >
                <MenuItem value="">Todos</MenuItem>
                {ESTADOS.map((s) => <MenuItem key={s} value={s}>{s}</MenuItem>)}
              </TextField>
            </Grid>
            <Grid item xs={12} sm={4} md={2.4}>
              <TextField
                label="Bodega" select size="small" fullWidth value={form.bodega} onChange={(e) => set("bodega", e.target.value)}
                helperText=" " SelectProps={{ displayEmpty: true }} InputLabelProps={{ shrink: true }}
              >
                <MenuItem value="">Todas</MenuItem>
                {BODEGAS.map((b) => <MenuItem key={b} value={b}>{b}</MenuItem>)}
              </TextField>
            </Grid>
          </Grid>
          <Stack direction="row" spacing={1}>
            <Button type="submit" variant="contained" startIcon={<Search size={16} />} disabled={cargando}>
              {cargando ? "Buscando..." : "Buscar"}
            </Button>
            <Button
              variant="outlined" startIcon={<Download size={16} />}
              disabled={!resultado || resultado.items.length === 0}
              onClick={() => resultado && exportarCsv(resultado)}
            >
              Exportar CSV
            </Button>
          </Stack>
        </Box>
      </Paper>

      {errorServidor && <Alert severity="error" sx={{ mb: 2 }}>{errorServidor}</Alert>}

      <Paper variant="outlined" sx={{ borderColor: industryColors.border }}>
        {resultado && (
          <Box
            sx={{
              px: 2, py: 1, bgcolor: industryColors.accent100, borderBottom: `1px solid ${industryColors.border}`,
              display: "flex", flexWrap: "wrap", gap: 2, justifyContent: "space-between", alignItems: "center",
            }}
          >
            <Typography variant="body2" sx={{ fontFamily: "monospace" }}>
              {entero(resultado.total)} {resultado.total === 1 ? "orden" : "órdenes"} · monto total{" "}
              <b>{dinero(resultado.montoTotal)}</b>
            </Typography>
            {aplicados && (
              <Typography variant="caption" color="text.secondary">
                Del {formatoFecha(aplicados.fechaDesde)} al {formatoFecha(aplicados.fechaHasta)}
              </Typography>
            )}
          </Box>
        )}
        {cargando && <LinearProgress />}

        <TableContainer>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>N.º de orden</TableCell>
                <TableCell>Fecha</TableCell>
                <TableCell>NIT</TableCell>
                <TableCell>Proveedor</TableCell>
                <TableCell>Bodega</TableCell>
                <TableCell>Estado</TableCell>
                <TableCell align="right">Monto</TableCell>
                <TableCell align="right">Líneas vigentes</TableCell>
                <TableCell align="right">Líneas anuladas</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {items.map((o) => (
                <TableRow key={o.id} hover>
                  <TableCell sx={{ fontFamily: "monospace", whiteSpace: "nowrap" }}>{o.numeroOrden}</TableCell>
                  <TableCell sx={{ whiteSpace: "nowrap" }}>{formatoFecha(o.fechaEmision)}</TableCell>
                  <TableCell sx={{ fontFamily: "monospace" }}>{o.proveedorNit}</TableCell>
                  <TableCell>{o.proveedorNombre}</TableCell>
                  <TableCell>{o.bodegaDestino}</TableCell>
                  <TableCell><Chip size="small" label={o.estado} color={colorEstado[o.estado] ?? "default"} variant="outlined" /></TableCell>
                  <TableCell align="right" sx={{ fontFamily: "monospace", whiteSpace: "nowrap" }}>
                    {dinero(o.monto, o.moneda === "USD" ? "$" : "Q")}
                  </TableCell>
                  <TableCell align="right" sx={{ fontFamily: "monospace" }}>{entero(o.lineasVigentes)}</TableCell>
                  <TableCell align="right" sx={{ fontFamily: "monospace", color: o.lineasAnuladas > 0 ? industryColors.danger : undefined }}>
                    {entero(o.lineasAnuladas)}
                  </TableCell>
                </TableRow>
              ))}
              {items.length === 0 && (
                <TableRow>
                  <TableCell colSpan={9} align="center">
                    <Typography variant="body2" color="text.secondary" py={3}>
                      {!resultado
                        ? cargando ? "Ejecutando reporte..." : "Indique el rango de fechas y pulse Buscar para ejecutar el reporte."
                        : "No se encontraron órdenes con los filtros indicados."}
                    </Typography>
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </TableContainer>

        {resultado && (
          <TablePagination
            component="div"
            count={resultado.total}
            page={pagina}
            rowsPerPage={tamano}
            rowsPerPageOptions={TAMANOS_PAGINA}
            labelRowsPerPage="Filas por página"
            labelDisplayedRows={({ from, to, count }) => `${from}–${to} de ${count}`}
            onPageChange={(_, p) => aplicados && void ejecutar(aplicados, p + 1, tamano)}
            onRowsPerPageChange={(e) => aplicados && void ejecutar(aplicados, 1, Number(e.target.value))}
          />
        )}
      </Paper>
    </Box>
  );
}
