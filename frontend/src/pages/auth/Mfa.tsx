import { useState } from "react";
import { Box, Typography, TextField, Button, Alert, Link as MLink } from "@mui/material";
import { Link, Navigate, useLocation, useNavigate } from "react-router-dom";
import { useAuth } from "../../auth/AuthContext";
import Blueprint from "../../theme/Blueprint";
import { industryColors } from "../../theme/industryTheme";

export default function Mfa() {
  const { verifyMfa } = useAuth();
  const navigate = useNavigate();
  const location = useLocation() as any;
  const mfaToken = location.state?.mfaToken as string | undefined;
  const [code, setCode] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  if (!mfaToken) return <Navigate to="/login" replace />;

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    if (!code.trim()) { setError("Ingresa el código de seguridad."); return; }
    setSubmitting(true);
    try {
      const result = await verifyMfa(mfaToken, code.trim());
      if (!result.success) { setError(result.error ?? "Código inválido."); return; }
      navigate("/operaciones", { replace: true });
    } catch {
      setError("Error de servidor. Intenta nuevamente.");
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Box minHeight="100vh" display="flex" alignItems="center" justifyContent="center" bgcolor={industryColors.bg} p={2}>
      <Blueprint sx={{ width: 400, p: 4, bgcolor: "background.paper" }}>
        <Typography variant="h4" mb={0.5}>Verificación en dos pasos</Typography>
        <Typography variant="body2" color="text.secondary" mb={3}>Ingresa el código de seguridad enviado/generado.</Typography>
        <Box component="form" onSubmit={onSubmit} display="flex" flexDirection="column" gap={2}>
          {error && <Alert severity="error">{error}</Alert>}
          <TextField label="Código MFA" fullWidth value={code} onChange={(e) => setCode(e.target.value)} inputProps={{ maxLength: 6 }} autoFocus />
          <Button type="submit" variant="contained" size="large" disabled={submitting}>{submitting ? "Verificando..." : "Verificar"}</Button>
        </Box>
        <Box mt={3} textAlign="center">
          <MLink component={Link} to="/login">Volver al inicio de sesión</MLink>
        </Box>
      </Blueprint>
    </Box>
  );
}
