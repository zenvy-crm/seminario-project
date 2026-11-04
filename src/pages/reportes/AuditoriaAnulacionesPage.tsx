import { useCallback, useEffect, useRef, useState } from "react";
import {
  Alert, AlertTitle, Box, Button, Chip, LinearProgress, Paper, Stack, Tab, Table, TableBody, TableCell,
  TableContainer, TableHead, TablePagination, TableRow, Tabs, TextField, Typography,
} from "@mui/material";
import { RefreshCw, ShieldAlert } from "lucide-react";
import { mensajeError, reportesApi } from "../../api/reportesApi";
import { industryColors } from "../../theme/industryTheme";
import { useAuth } from "../../auth/AuthContext";
import { AuditoriaSoftDeleteResult, NIVEL_ROL, TipoSoftDelete } from "../../types";
import { BarraContexto, dinero, entero, formatoFechaHora, statusHttp } from "./comunes";

const TAMANOS_PAGINA = [25, 50, 100, 200];

const TABS: { tipo: TipoSoftDelete; label: string; vacio: string }[] = [
  { tipo: "ordenes", label: "Órdenes anuladas", vacio: "No hay órdenes anuladas en el período." },
  { tipo: "lineas", label: "Líneas anuladas", vacio: "No hay líneas anuladas en el período." },
  { tipo: "documentos", label: "Documentos eliminados", vacio: "No hay documentos eliminados en el período." },
];

export default function AuditoriaAnulacionesPage() {
  const { role } = useAuth();
  const esAdmin = !!role && NIVEL_ROL[role] === 3;
  const [tipo, setTipo] = useState<TipoSoftDelete>("ordenes");
  const [fechaDesde, setFechaDesde] = useState("");
  const [fechaHasta, setFechaHasta] = useState("");
  const [pagina, setPagina] = useState(0);
  const [tamano, setTamano] = useState(50);
  const [resultado, setResultado] = useState<AuditoriaSoftDeleteResult | null>(null);
  const [cargando, setCargando] = useState(false);
  const [error, setError] = useState<string | null>(null);
  /** Mensaje del backend cuando responde 403: el acceso lo decide el servidor, no la interfaz. */
  const [denegado, setDenegado] = useState<string | null>(null);
  const solicitud = useRef(0);

  const rangoInvalido = !!fechaDesde && !!fechaHasta && fechaDesde > fechaHasta;

  const cargar = useCallback(async () => {
    if (rangoInvalido) return;
    const id = ++solicitud.current;
    setCargando(true);
    setError(null);
    setDenegado(null);
    try {
      const res = await reportesApi.auditoriaSoftDelete({
        tipo, fechaDesde, fechaHasta, page: pagina + 1, pageSize: tamano,
      });
      if (id === solicitud.current) setResultado(res);
    } catch (e) {
      if (id !== solicitud.current) return;
      setResultado(null);
      if (statusHttp(e) === 403) setDenegado(mensajeError(e, "No tiene permisos para realizar esta accion."));
      else setError(mensajeError(e, "No se pudo cargar la auditoría de anulaciones."));
    } finally {
      if (id === solicitud.current) setCargando(false);
    }
  }, [tipo, fechaDesde, fechaHasta, pagina, tamano, rangoInvalido]);

  useEffect(() => { void cargar(); }, [cargar]);

  const items = resultado?.items ?? [];
  const pestana = TABS.find((t) => t.tipo === tipo) ?? TABS[0];
  const conteo = (t: TipoSoftDelete) => resultado?.resumen?.[t];

  return (
    <Box>
      <Typography variant="h4" mb={0.5}>Auditoría de anulaciones</Typography>
      <Typography variant="body1" color="text.secondary" mb={2}>
        Órdenes, líneas y documentos dados de baja, con quién lo hizo, cuándo y por qué.
      </Typography>

      <BarraContexto />

      {denegado !== null || !esAdmin ? (
        // Sin redireccion: la consulta se hace igual y la respuesta 403 del backend queda a la vista.
        <Alert severity="error">
          <AlertTitle>Acceso denegado: este reporte es exclusivo del rol Administrador.</AlertTitle>
          {denegado ?? (cargando ? "Verificando permisos..." : "No tiene acceso a este reporte.")}
        </Alert>
      ) : (
        <Paper variant="outlined" sx={{ borderColor: industryColors.border }}>
          <Stack direction="row" alignItems="center" spacing={1} px={2.5} pt={2}>
            <ShieldAlert size={18} strokeWidth={1.5} />
            <Typography variant="h6">Bajas registradas</Typography>
          </Stack>

          <Tabs
            value={tipo}
            onChange={(_, v: TipoSoftDelete) => { setTipo(v); setPagina(0); }}
            variant="scrollable"
            sx={{ px: 1.5, borderBottom: `1px solid ${industryColors.border}` }}
          >
            {TABS.map((t) => (
              <Tab
                key={t.tipo}
                value={t.tipo}
                label={
                  <Stack direction="row" alignItems="center" spacing={1}>
                    <span>{t.label}</span>
                    {conteo(t.tipo) !== undefined && (
                      <Chip
                        size="small"
                        label={entero(conteo(t.tipo) ?? 0)}
                        color={t.tipo === tipo ? "error" : "default"}
                        sx={{ height: 20, fontSize: 11 }}
                      />
                    )}
                  </Stack>
                }
              />
            ))}
          </Tabs>

          <Stack direction={{ xs: "column", sm: "row" }} spacing={1.5} alignItems={{ sm: "flex-start" }} p={2.5} pb={1}>
            <TextField
              label="Fecha desde" type="date" size="small" value={fechaDesde} InputLabelProps={{ shrink: true }}
              onChange={(e) => { setFechaDesde(e.target.value); setPagina(0); }}
              error={rangoInvalido} helperText={rangoInvalido ? "Debe ser anterior o igual a la fecha hasta" : " "}
            />
            <TextField
              label="Fecha hasta" type="date" size="small" value={fechaHasta} InputLabelProps={{ shrink: true }}
              onChange={(e) => { setFechaHasta(e.target.value); setPagina(0); }}
              error={rangoInvalido} helperText=" "
            />
            <Button
              variant="outlined" size="small" startIcon={<RefreshCw size={16} />}
              onClick={() => void cargar()} disabled={cargando || rangoInvalido} sx={{ mt: 0.5 }}
            >
              Refrescar
            </Button>
            {(fechaDesde || fechaHasta) && (
              <Button size="small" color="inherit" sx={{ mt: 0.5 }} onClick={() => { setFechaDesde(""); setFechaHasta(""); setPagina(0); }}>
                Quitar fechas
              </Button>
            )}
          </Stack>

          {error && (
            <Alert severity="error" sx={{ mx: 2.5, mb: 2 }} action={<Button color="inherit" size="small" onClick={() => void cargar()}>Reintentar</Button>}>
              {error}
            </Alert>
          )}
          {cargando && <LinearProgress />}

          <TableContainer>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>Referencia</TableCell>
                  <TableCell>Detalle</TableCell>
                  <TableCell align="right">Monto</TableCell>
                  <TableCell>Fecha de baja</TableCell>
                  <TableCell>Usuario</TableCell>
                  <TableCell>Motivo</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {items.map((a) => (
                  <TableRow key={`${a.tipo}-${a.entidadId}-${a.referencia}`} hover>
                    <TableCell sx={{ fontFamily: "monospace", whiteSpace: "nowrap" }}>{a.referencia}</TableCell>
                    <TableCell>{a.detalle}</TableCell>
                    <TableCell align="right" sx={{ fontFamily: "monospace", whiteSpace: "nowrap" }}>
                      {a.monto == null ? "-" : dinero(a.monto)}
                    </TableCell>
                    <TableCell sx={{ whiteSpace: "nowrap" }}>{formatoFechaHora(a.fechaBaja)}</TableCell>
                    <TableCell sx={{ whiteSpace: "nowrap" }}>
                      {a.usuarioBaja || "-"} <Typography component="span" variant="caption" color="text.secondary">(#{a.usuarioBajaId})</Typography>
                    </TableCell>
                    <TableCell>{a.motivo || "-"}</TableCell>
                  </TableRow>
                ))}
                {items.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={6} align="center">
                      <Typography variant="body2" color="text.secondary" py={3}>
                        {cargando ? "Cargando..." : error ? "No se pudo obtener información." : pestana.vacio}
                      </Typography>
                    </TableCell>
                  </TableRow>
                )}
              </TableBody>
            </Table>
          </TableContainer>

          <TablePagination
            component="div"
            count={resultado?.total ?? 0}
            page={resultado ? pagina : 0}
            rowsPerPage={tamano}
            rowsPerPageOptions={TAMANOS_PAGINA}
            labelRowsPerPage="Filas por página"
            labelDisplayedRows={({ from, to, count }) => `${from}–${to} de ${entero(count)}`}
            onPageChange={(_, p) => setPagina(p)}
            onRowsPerPageChange={(e) => { setTamano(Number(e.target.value)); setPagina(0); }}
          />
        </Paper>
      )}
    </Box>
  );
}
