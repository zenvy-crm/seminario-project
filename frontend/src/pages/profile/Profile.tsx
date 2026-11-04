import { useEffect, useState } from "react";
import {
  Box,
  Typography,
  Grid,
  TextField,
  Button,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Divider,
} from "@mui/material";
import { useForm } from "react-hook-form";
import { FileText, Upload } from "lucide-react";
import { useAuth } from "../../auth/AuthContext";
import { profileApi } from "../../api/profileApi";
import { documentsApi } from "../../api/documentsApi";
import { User, AppDocument } from "../../types";
import { RoleBadge, StatusBadge } from "../../components/common/Badges";
import { LoadingState, ErrorState } from "../../components/common/StateViews";
import { useToast } from "../../components/common/ToastProvider";
import Blueprint from "../../theme/Blueprint";

interface ProfileFormValues {
  firstName: string;
  lastName: string;
  email: string;
  phone?: string;
  identification?: string;
}
interface PasswordFormValues {
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
}

export default function Profile() {
  const { user: authUser } = useAuth();
  const { showToast } = useToast();
  const [profile, setProfile] = useState<User | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [documents, setDocuments] = useState<AppDocument[]>([]);
  const [pwdOpen, setPwdOpen] = useState(false);
  const [saving, setSaving] = useState(false);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<ProfileFormValues>();
  const pwdForm = useForm<PasswordFormValues>();

  useEffect(() => {
    if (!authUser) return;
    profileApi
      .getProfile(authUser.id)
      .then((p) => {
        setProfile(p);
        setDocuments(p.documents ?? []);
        reset(p);
      })
      .catch(() => setError("No fue posible cargar la información."));
  }, [authUser, reset]);

  const onSave = async (values: ProfileFormValues) => {
    if (!authUser) return;
    setSaving(true);
    try {
      const updated = await profileApi.updateProfile(authUser.id, values);
      setProfile(updated);
      showToast("Perfil actualizado correctamente.");
    } catch {
      showToast("No fue posible actualizar el perfil.", "error");
    } finally {
      setSaving(false);
    }
  };

  const onChangePassword = async (values: PasswordFormValues) => {
    if (values.newPassword !== values.confirmPassword) {
      pwdForm.setError("confirmPassword", {
        message: "Las contraseñas no coinciden.",
      });
      return;
    }
    try {
      await profileApi.changePassword(
        values.currentPassword,
        values.newPassword
      );
      showToast("Contraseña actualizada correctamente.");
      setPwdOpen(false);
      pwdForm.reset();
    } catch {
      showToast("No fue posible actualizar la contraseña.", "error");
    }
  };

  const onUpload = async (file: File) => {
    if (!authUser) return;
    try {
      const doc = await documentsApi.upload(authUser.id, file);
      setDocuments((d) => [...d, doc]);
      showToast("Documento subido correctamente.");
    } catch {
      showToast("No fue posible subir el documento.", "error");
    }
  };

  if (error) return <ErrorState message={error} />;
  if (!profile) return <LoadingState />;

  return (
    <Box maxWidth={720}>
      <Typography variant="h4" mb={3}>
        Mi perfil
      </Typography>

      <Blueprint sx={{ p: 3, mb: 3, bgcolor: "background.paper" }}>
        <Box display="flex" gap={2} alignItems="center" mb={3}>
          <RoleBadge role={profile.role} />
          <StatusBadge status={profile.status} />
          <Typography variant="caption" color="text.secondary">
            Miembro desde{" "}
            {new Date(profile.createdAt).toLocaleDateString("es-ES")}
          </Typography>
        </Box>
        <Box component="form" onSubmit={handleSubmit(onSave)}>
          <Grid container spacing={2}>
            <Grid item xs={12} md={6}>
              <TextField
                label="Nombre"
                fullWidth
                {...register("firstName", { required: "Campo obligatorio." })}
                error={!!errors.firstName}
                helperText={errors.firstName?.message}
              />
            </Grid>
            <Grid item xs={12} md={6}>
              <TextField
                label="Apellido"
                fullWidth
                {...register("lastName", { required: "Campo obligatorio." })}
                error={!!errors.lastName}
                helperText={errors.lastName?.message}
              />
            </Grid>
            <Grid item xs={12} md={6}>
              <TextField
                label="Username"
                fullWidth
                value={profile.username}
                disabled
              />
            </Grid>
            <Grid item xs={12} md={6}>
              <TextField
                label="Email"
                fullWidth
                {...register("email", { required: "Campo obligatorio." })}
                error={!!errors.email}
                helperText={errors.email?.message}
              />
            </Grid>
            <Grid item xs={12} md={6}>
              <TextField label="Teléfono" fullWidth {...register("phone")} />
            </Grid>
            <Grid item xs={12} md={6}>
              <TextField
                label="Identificación"
                fullWidth
                {...register("identification")}
              />
            </Grid>
            <Grid item xs={12} display="flex" gap={2}>
              <Button type="submit" variant="contained" disabled={saving}>
                {saving ? "Guardando..." : "Guardar cambios"}
              </Button>
              <Button variant="outlined" onClick={() => setPwdOpen(true)}>
                Cambiar contraseña
              </Button>
            </Grid>
          </Grid>
        </Box>
      </Blueprint>

      <Typography variant="h6" mb={1.5}>
        Documentos
      </Typography>
      <Blueprint sx={{ p: 3, bgcolor: "background.paper" }}>
        {documents.length === 0 && (
          <Typography color="text.secondary" mb={2}>
            No has cargado documentos.
          </Typography>
        )}
        {documents.map((doc, i) => (
          <Box key={doc.id}>
            {i > 0 && <Divider sx={{ my: 2 }} />}
            <Box display="flex" alignItems="center" gap={2}>
              <FileText size={18} strokeWidth={1.5} />
              <Box flexGrow={1}>
                <Typography variant="body2" fontWeight={600}>
                  {doc.fileName}
                </Typography>
                <Typography variant="caption" color="text.secondary">
                  {(doc.sizeBytes / (1024 * 1024)).toFixed(1)} MB
                </Typography>
              </Box>
              <Button size="small" variant="outlined">
                Ver
              </Button>
              <Button size="small" variant="outlined">
                Descargar
              </Button>
            </Box>
          </Box>
        ))}
        <Button
          component="label"
          variant="outlined"
          startIcon={<Upload size={16} strokeWidth={1.5} />}
          sx={{ mt: 2 }}
        >
          Subir documento
          <input
            type="file"
            hidden
            accept=".pdf,.jpg,.jpeg"
            onChange={(e) => e.target.files?.[0] && onUpload(e.target.files[0])}
          />
        </Button>
      </Blueprint>

      <Dialog
        open={pwdOpen}
        onClose={() => setPwdOpen(false)}
        maxWidth="xs"
        fullWidth
      >
        <DialogTitle>Cambiar contraseña</DialogTitle>
        <Box component="form" onSubmit={pwdForm.handleSubmit(onChangePassword)}>
          <DialogContent
            sx={{ display: "flex", flexDirection: "column", gap: 2 }}
          >
            <TextField
              label="Contraseña actual"
              type="password"
              fullWidth
              {...pwdForm.register("currentPassword", {
                required: "Campo obligatorio.",
              })}
              error={!!pwdForm.formState.errors.currentPassword}
              helperText={pwdForm.formState.errors.currentPassword?.message}
            />
            <TextField
              label="Nueva contraseña"
              type="password"
              fullWidth
              {...pwdForm.register("newPassword", {
                required: "Campo obligatorio.",
                minLength: { value: 8, message: "Mínimo 8 caracteres." },
              })}
              error={!!pwdForm.formState.errors.newPassword}
              helperText={pwdForm.formState.errors.newPassword?.message}
            />
            <TextField
              label="Confirmar nueva contraseña"
              type="password"
              fullWidth
              {...pwdForm.register("confirmPassword", {
                required: "Campo obligatorio.",
              })}
              error={!!pwdForm.formState.errors.confirmPassword}
              helperText={pwdForm.formState.errors.confirmPassword?.message}
            />
          </DialogContent>
          <DialogActions sx={{ p: 2 }}>
            <Button onClick={() => setPwdOpen(false)} color="inherit">
              Cancelar
            </Button>
            <Button type="submit" variant="contained">
              Guardar
            </Button>
          </DialogActions>
        </Box>
      </Dialog>
    </Box>
  );
}
