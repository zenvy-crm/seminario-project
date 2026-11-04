import { useCallback, useEffect, useRef, useState } from "react";
import {
  Alert, Box, Button, Chip, LinearProgress, MenuItem, Paper, Stack, Table, TableBody, TableCell,
  TableContainer, TableHead, TableRow, TextField, Typography,
} from "@mui/material";
import { RefreshCw, Trophy } from "lucide-react";
import { mensajeError, reportesApi } from "../../api/reportesApi";
import { industryColors } from "../../theme/industryTheme";
import { RankingProveedor } from "../../types";
import { ANIOS, BarraContexto, EJERCICIO, dinero, entero } from "./comunes";

const TOPS = [10, 20, 50];

const medallas: Record<number, { bg: string; color: string }> = {
  1: { bg: "#d4af37", color: "#3a2f00" },
  2: { bg: "#b8bcc0", color: "#25282a" },
  3: { bg: "#b87333", color: "#ffffff" },
};

/** Muestra la posicion tal como la devolvio SQL Server (RANK() admite empates, por eso no se usa el indice de la fila). */
function ChipPosicion({ posicion }: { posicion: number }) {
  const medalla = medallas[posicion];
  return (
    <Chip
      size="small"
      label={`#${posicion}`}
      icon={medalla ? <Trophy size={14} strokeWidth={1.75} color="currentColor" /> : undefined}
      sx={{
        minWidth: 64, fontWeight: 700, fontFamily: "monospace",
        ...(medalla
          ? { bgcolor: medalla.bg, color: medalla.color, "& .MuiChip-icon": { color: medalla.color } }
          : { bgcolor: industryColors.accent100, color: industryColors.accent700 }),
      }}
    />
  );
}

export default function RankingProveedoresPage() {
  const [anio, setAnio] = useState(ANIOS.includes(EJERCICIO) ? EJERCICIO : ANIOS[ANIOS.length - 1]);
  const [top, setTop] = useState(20);
  const [filas, setFilas] = useState<RankingProveedor[]>([]);
  const [cargando, setCargando] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const solicitud = useRef(0);

  const cargar = useCallback(async () => {
    const id = ++solicitud.current;
    setCargando(true);
    setError(null);
    try {
      const datos = await reportesApi.rankingProveedores(anio, top);
      if (id === solicitud.current) setFilas(datos);
    } catch (e) {
      if (id === solicitud.current) {
        setFilas([]);
        setError(mensajeError(e, "No se pudo cargar el ranking de proveedores."));
      }
    } finally {
      if (id === solicitud.current) setCargando(false);
    }
  }, [anio, top]);

  useEffect(() => { void cargar(); }, [cargar]);

  return (
    <Box>
      <Typography variant="h4" mb={0.5}>Ranking de proveedores</Typography>
      <Typography variant="body1" color="text.secondary" mb={2}>
        Proveedores ordenados por el monto de sus órdenes vigentes en el ejercicio.
      </Typography>

      <BarraContexto />

      <Paper variant="outlined" sx={{ p: 2.5, borderColor: industryColors.border }}>
        <Stack direction={{ xs: "column", sm: "row" }} spacing={1.5} alignItems={{ sm: "center" }} mb={2}>
          <Stack direction="row" alignItems="center" spacing={1}>
            <Trophy size={18} strokeWidth={1.5} />
            <Typography variant="h6">Proveedores con mayor monto comprometido</Typography>
          </Stack>
          <Box flexGrow={1} />
          <TextField select size="small" label="Ejercicio" value={anio} onChange={(e) => setAnio(Number(e.target.value))} sx={{ width: 120 }}>
            {ANIOS.map((a) => <MenuItem key={a} value={a}>{a}</MenuItem>)}
          </TextField>
          <TextField select size="small" label="Top" value={top} onChange={(e) => setTop(Number(e.target.value))} sx={{ width: 100 }}>
            {TOPS.map((t) => <MenuItem key={t} value={t}>{t}</MenuItem>)}
          </TextField>
          <Button size="small" variant="outlined" startIcon={<RefreshCw size={16} />} onClick={() => void cargar()} disabled={cargando}>
            Refrescar
          </Button>
        </Stack>

        {error && (
          <Alert severity="error" sx={{ mb: 2 }} action={<Button color="inherit" size="small" onClick={() => void cargar()}>Reintentar</Button>}>
            {error}
          </Alert>
        )}
        {cargando && <LinearProgress sx={{ mb: 1 }} />}

        <TableContainer>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell width={110}>Posición</TableCell>
                <TableCell>NIT</TableCell>
                <TableCell>Proveedor</TableCell>
                <TableCell align="right">Órdenes</TableCell>
                <TableCell align="right">Líneas</TableCell>
                <TableCell align="right">Total comprometido</TableCell>
                <TableCell width={220}>Participación</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {filas.map((f) => {
                const pct = Number.isFinite(f.participacionPct) ? f.participacionPct : 0;
                return (
                  <TableRow key={`${f.posicionRanking}-${f.proveedorNit}`} hover>
                    <TableCell><ChipPosicion posicion={f.posicionRanking} /></TableCell>
                    <TableCell sx={{ fontFamily: "monospace" }}>{f.proveedorNit}</TableCell>
                    <TableCell sx={{ fontWeight: f.posicionRanking <= 3 ? 600 : 400 }}>{f.proveedorNombre}</TableCell>
                    <TableCell align="right" sx={{ fontFamily: "monospace" }}>{entero(f.ordenes)}</TableCell>
                    <TableCell align="right" sx={{ fontFamily: "monospace" }}>{entero(f.lineas)}</TableCell>
                    <TableCell align="right" sx={{ fontFamily: "monospace", fontWeight: 600 }}>{dinero(f.totalComprometido)}</TableCell>
                    <TableCell>
                      <Stack direction="row" alignItems="center" spacing={1}>
                        <LinearProgress
                          variant="determinate" value={Math.min(100, Math.max(0, pct))}
                          sx={{ flexGrow: 1, height: 6, bgcolor: industryColors.accent100 }}
                        />
                        <Typography variant="caption" sx={{ width: 52, textAlign: "right", fontFamily: "monospace" }}>
                          {pct.toLocaleString("es-GT", { minimumFractionDigits: 2, maximumFractionDigits: 2 })}%
                        </Typography>
                      </Stack>
                    </TableCell>
                  </TableRow>
                );
              })}
              {!cargando && !error && filas.length === 0 && (
                <TableRow>
                  <TableCell colSpan={7} align="center">
                    <Typography variant="body2" color="text.secondary" py={3}>
                      No hay líneas vigentes para el ejercicio {anio}.
                    </Typography>
                  </TableCell>
                </TableRow>
              )}
              {cargando && filas.length === 0 && (
                <TableRow>
                  <TableCell colSpan={7} align="center">
                    <Typography variant="body2" color="text.secondary" py={3}>Cargando ranking...</Typography>
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </TableContainer>
      </Paper>
    </Box>
  );
}
