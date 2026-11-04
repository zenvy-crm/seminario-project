import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { TextField, Button, Typography, Box, Alert } from "@mui/material";
import { useNavigate, useLocation } from "react-router-dom";
import { useState } from "react";
import { useAuth } from "../../auth/AuthContext";
import Blueprint from "../../theme/Blueprint";
import { industryColors } from "../../theme/industryTheme";

const schema = z.object({
  username: z.string().min(1, "Ingresa tu usuario o correo."),
  password: z.string().min(1, "Ingresa tu contraseña."),
});
type FormValues = z.infer<typeof schema>;

export default function Login() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const desde = (location.state as { from?: { pathname?: string } } | null)
    ?.from?.pathname;
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<FormValues>({ resolver: zodResolver(schema) });

  const onSubmit = async (values: FormValues) => {
    setError(null);
    setSubmitting(true);
    try {
      const result = await login(values.username, values.password);
      if (result.error) {
        setError(result.error);
        return;
      }
      if (result.requiresMfa) {
        navigate("/mfa", {
          state: { mfaToken: result.mfaToken, username: values.username },
        });
        return;
      }
      navigate(desde ?? "/operaciones", { replace: true });
    } catch (e) {
      // Con respuesta (401 credenciales, 403 cuenta bloqueada, 429 demasiados intentos) se
      // muestra el mensaje del backend; el generico queda solo para cuando no hay respuesta.
      const respuesta = (e as { response?: { data?: { message?: string } } })
        .response;
      const mensaje = respuesta?.data?.message;
      setError(
        respuesta
          ? (mensaje ?? "No fue posible iniciar sesión.")
          : "No fue posible conectar con el servidor. Intenta más tarde."
      );
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Box
      minHeight="100vh"
      display="flex"
      alignItems="center"
      justifyContent="center"
      bgcolor={industryColors.bg}
      p={2}
    >
      <Blueprint sx={{ width: 400, p: 4, bgcolor: "background.paper" }}>
        <Typography variant="h4" mb={0.5}>
          Iniciar sesión
        </Typography>
        <Typography variant="body2" color="text.secondary" mb={3}>
          Accede a tu panel de administración
        </Typography>

        <Box
          component="form"
          onSubmit={handleSubmit(onSubmit)}
          display="flex"
          flexDirection="column"
          gap={2}
        >
          {error && <Alert severity="error">{error}</Alert>}
          <TextField
            label="Usuario o correo"
            fullWidth
            {...register("username")}
            error={!!errors.username}
            helperText={errors.username?.message}
            autoFocus
          />
          <TextField
            label="Contraseña"
            type="password"
            fullWidth
            {...register("password")}
            error={!!errors.password}
            helperText={errors.password?.message}
          />
          <Button
            type="submit"
            variant="contained"
            size="large"
            disabled={submitting}
            sx={{ mt: 1 }}
          >
            {submitting ? "Ingresando..." : "Iniciar sesión"}
          </Button>
        </Box>

        <Box
          mt={3}
          display="flex"
          flexDirection="column"
          gap={1}
          alignItems="center"
        >
          {/*<Typography variant="body2">*/}
          {/*  ¿No tienes cuenta? <MLink component={Link} to="/register">Regístrate</MLink>*/}
          {/*</Typography>*/}
          <Typography variant="body2" color="text.secondary">
            ¿Olvidaste tu contraseña?
          </Typography>
        </Box>
      </Blueprint>
    </Box>
  );
}
