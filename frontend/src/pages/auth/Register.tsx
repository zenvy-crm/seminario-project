import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import {
  Box,
  TextField,
  Button,
  Typography,
  Alert,
  Grid,
  Link as MLink,
} from "@mui/material";
import { Link, useNavigate } from "react-router-dom";
import { FileText, X, Upload } from "lucide-react";
import { authApi } from "../../api/authApi";
import Blueprint from "../../theme/Blueprint";
import { industryColors } from "../../theme/industryTheme";

const ACCEPTED_TYPES = ["application/pdf", "image/jpeg", "image/jpg"];
const MAX_SIZE_MB = 5;

const schema = z
  .object({
    firstName: z.string().min(1, "Campo obligatorio."),
    lastName: z.string().min(1, "Campo obligatorio."),
    username: z.string().min(3, "Mínimo 3 caracteres."),
    email: z.string().email("Correo inválido."),
    phone: z.string().min(7, "Teléfono inválido."),
    birthDate: z.string().min(1, "Campo obligatorio."),
    identification: z.string().min(1, "Campo obligatorio."),
    password: z.string().min(8, "Mínimo 8 caracteres."),
    confirmPassword: z.string().min(1, "Confirma tu contraseña."),
  })
  .refine((d) => d.password === d.confirmPassword, {
    message: "Las contraseñas no coinciden.",
    path: ["confirmPassword"],
  });

type FormValues = z.infer<typeof schema>;

export default function Register() {
  const navigate = useNavigate();
  const [file, setFile] = useState<File | null>(null);
  const [fileError, setFileError] = useState<string | null>(null);
  const [formError, setFormError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<FormValues>({ resolver: zodResolver(schema) });

  const onFileChange = (f: File | null) => {
    if (!f) {
      setFile(null);
      return;
    }
    if (!ACCEPTED_TYPES.includes(f.type)) {
      setFileError("Solo se aceptan archivos PDF, JPG o JPEG.");
      return;
    }
    if (f.size > MAX_SIZE_MB * 1024 * 1024) {
      setFileError(`El archivo no debe superar ${MAX_SIZE_MB} MB.`);
      return;
    }
    setFileError(null);
    setFile(f);
  };

  const onSubmit = async (values: FormValues) => {
    setFormError(null);
    if (!file) {
      setFileError("Adjunta tu documento de identificación.");
      return;
    }
    setSubmitting(true);
    try {
      const fd = new FormData();
      Object.entries(values).forEach(([k, v]) => fd.append(k, v));
      fd.append("document", file);
      const res = await authApi.register(fd);
      if (!res.success) {
        setFormError(res.message ?? "No fue posible completar el registro.");
        return;
      }
      navigate("/login");
    } catch {
      setFormError(
        "No fue posible conectar con el servidor. Intenta más tarde."
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
      <Blueprint sx={{ width: 560, p: 4, bgcolor: "background.paper" }}>
        <Typography variant="h4" mb={0.5}>
          Crear cuenta
        </Typography>
        <Typography variant="body2" color="text.secondary" mb={3}>
          Completa tus datos para registrarte
        </Typography>

        <Box component="form" onSubmit={handleSubmit(onSubmit)}>
          {formError && (
            <Alert severity="error" sx={{ mb: 2 }}>
              {formError}
            </Alert>
          )}
          <Grid container spacing={2}>
            <Grid item xs={6}>
              <TextField
                label="Nombre"
                fullWidth
                {...register("firstName")}
                error={!!errors.firstName}
                helperText={errors.firstName?.message}
              />
            </Grid>
            <Grid item xs={6}>
              <TextField
                label="Apellido"
                fullWidth
                {...register("lastName")}
                error={!!errors.lastName}
                helperText={errors.lastName?.message}
              />
            </Grid>
            <Grid item xs={6}>
              <TextField
                label="Username"
                fullWidth
                {...register("username")}
                error={!!errors.username}
                helperText={errors.username?.message}
              />
            </Grid>
            <Grid item xs={6}>
              <TextField
                label="Email"
                fullWidth
                {...register("email")}
                error={!!errors.email}
                helperText={errors.email?.message}
              />
            </Grid>
            <Grid item xs={6}>
              <TextField
                label="Teléfono"
                fullWidth
                {...register("phone")}
                error={!!errors.phone}
                helperText={errors.phone?.message}
              />
            </Grid>
            <Grid item xs={6}>
              <TextField
                label="Fecha de nacimiento"
                type="date"
                fullWidth
                InputLabelProps={{ shrink: true }}
                {...register("birthDate")}
                error={!!errors.birthDate}
                helperText={errors.birthDate?.message}
              />
            </Grid>
            <Grid item xs={6}>
              <TextField
                label="Identificación"
                fullWidth
                {...register("identification")}
                error={!!errors.identification}
                helperText={errors.identification?.message}
              />
            </Grid>
            <Grid item xs={6} />
            <Grid item xs={6}>
              <TextField
                label="Password"
                type="password"
                fullWidth
                {...register("password")}
                error={!!errors.password}
                helperText={errors.password?.message}
              />
            </Grid>
            <Grid item xs={6}>
              <TextField
                label="Confirmar password"
                type="password"
                fullWidth
                {...register("confirmPassword")}
                error={!!errors.confirmPassword}
                helperText={errors.confirmPassword?.message}
              />
            </Grid>

            <Grid item xs={12}>
              <Typography variant="body2" mb={1}>
                Documento de identificación (PDF, JPG o JPEG)
              </Typography>
              {!file ? (
                <Button
                  component="label"
                  variant="outlined"
                  startIcon={<Upload size={16} strokeWidth={1.5} />}
                >
                  Seleccionar archivo
                  <input
                    type="file"
                    hidden
                    accept=".pdf,.jpg,.jpeg"
                    onChange={(e) => onFileChange(e.target.files?.[0] ?? null)}
                  />
                </Button>
              ) : (
                <Box
                  display="flex"
                  alignItems="center"
                  gap={1}
                  border={`1px solid ${industryColors.border}`}
                  p={1.5}
                >
                  <FileText size={18} strokeWidth={1.5} />
                  <Typography variant="body2" flexGrow={1} noWrap>
                    {file.name}
                  </Typography>
                  <Button
                    size="small"
                    color="error"
                    onClick={() => setFile(null)}
                    startIcon={<X size={14} strokeWidth={1.5} />}
                  >
                    Eliminar
                  </Button>
                </Box>
              )}
              {fileError && (
                <Typography variant="caption" color="error">
                  {fileError}
                </Typography>
              )}
            </Grid>

            <Grid item xs={12}>
              <Button
                type="submit"
                variant="contained"
                size="large"
                fullWidth
                disabled={submitting}
              >
                {submitting ? "Registrando..." : "Crear cuenta"}
              </Button>
            </Grid>
          </Grid>
        </Box>

        <Box mt={3} textAlign="center">
          <Typography variant="body2">
            ¿Ya tienes cuenta?{" "}
            <MLink component={Link} to="/login">
              Inicia sesión
            </MLink>
          </Typography>
        </Box>
      </Blueprint>
    </Box>
  );
}
