import { useForm, Controller } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import {
  Box,
  TextField,
  Button,
  Grid,
  MenuItem,
  Select,
  InputLabel,
  FormControl,
  Alert,
} from "@mui/material";

const baseFields = {
  firstName: z.string().min(1, "Campo obligatorio."),
  lastName: z.string().min(1, "Campo obligatorio."),
  username: z.string().min(3, "Mínimo 3 caracteres."),
  email: z.string().email("Correo inválido."),
  phone: z.string().optional(),
  birthDate: z.string().optional(),
  identification: z.string().min(1, "Campo obligatorio."),
  role: z.enum(["USER", "INTERMEDIO", "ADMIN"]),
  status: z.enum(["ACTIVE", "INACTIVE"]),
};

const createSchema = z
  .object({
    ...baseFields,
    password: z.string().min(8, "Mínimo 8 caracteres."),
    confirmPassword: z.string(),
  })
  .refine((d) => d.password === d.confirmPassword, {
    message: "Las contraseñas no coinciden.",
    path: ["confirmPassword"],
  });

const editSchema = z.object({
  ...baseFields,
  password: z.string().optional(),
  confirmPassword: z.string().optional(),
});

export type UserFormValues = z.infer<typeof createSchema>;

interface Props {
  mode: "create" | "edit";
  defaultValues?: Partial<UserFormValues>;
  onSubmit: (values: UserFormValues) => Promise<void>;
  submitError?: string | null;
  submitting?: boolean;
}

export default function UserForm({
  mode,
  defaultValues,
  onSubmit,
  submitError,
  submitting,
}: Props) {
  const {
    register,
    handleSubmit,
    control,
    formState: { errors },
  } = useForm<UserFormValues>({
    resolver: zodResolver(
      mode === "create"
        ? createSchema
        : (editSchema as unknown as typeof createSchema)
    ),
    defaultValues: { role: "USER", status: "ACTIVE", ...defaultValues },
  });

  return (
    <Box component="form" onSubmit={handleSubmit(onSubmit)}>
      {submitError && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {submitError}
        </Alert>
      )}
      <Grid container spacing={2}>
        <Grid item xs={12} md={6}>
          <TextField
            label="Nombre"
            fullWidth
            {...register("firstName")}
            error={!!errors.firstName}
            helperText={errors.firstName?.message}
          />
        </Grid>
        <Grid item xs={12} md={6}>
          <TextField
            label="Apellido"
            fullWidth
            {...register("lastName")}
            error={!!errors.lastName}
            helperText={errors.lastName?.message}
          />
        </Grid>
        <Grid item xs={12} md={6}>
          <TextField
            label="Username"
            fullWidth
            disabled={mode === "edit"}
            {...register("username")}
            error={!!errors.username}
            helperText={errors.username?.message}
          />
        </Grid>
        <Grid item xs={12} md={6}>
          <TextField
            label="Email"
            fullWidth
            {...register("email")}
            error={!!errors.email}
            helperText={errors.email?.message}
          />
        </Grid>
        <Grid item xs={12} md={6}>
          <TextField
            label="Teléfono"
            fullWidth
            {...register("phone")}
            error={!!errors.phone}
            helperText={errors.phone?.message}
          />
        </Grid>
        <Grid item xs={12} md={6}>
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
        <Grid item xs={12} md={6}>
          <TextField
            label="Identificación"
            fullWidth
            {...register("identification")}
            error={!!errors.identification}
            helperText={errors.identification?.message}
          />
        </Grid>
        <Grid item xs={12} md={3}>
          <FormControl fullWidth>
            <InputLabel>Rol</InputLabel>
            <Controller
              name="role"
              control={control}
              render={({ field }) => (
                <Select label="Rol" {...field}>
                  <MenuItem value="USER">USER</MenuItem>
                  <MenuItem value="INTERMEDIO">INTERMEDIO</MenuItem>
                  <MenuItem value="ADMIN">ADMIN</MenuItem>
                </Select>
              )}
            />
          </FormControl>
        </Grid>
        <Grid item xs={12} md={3}>
          <FormControl fullWidth>
            <InputLabel>Estado</InputLabel>
            <Controller
              name="status"
              control={control}
              render={({ field }) => (
                <Select label="Estado" {...field}>
                  <MenuItem value="ACTIVE">Activo</MenuItem>
                  <MenuItem value="INACTIVE">Inactivo</MenuItem>
                </Select>
              )}
            />
          </FormControl>
        </Grid>

        {mode === "create" && (
          <>
            <Grid item xs={12} md={6}>
              <TextField
                label="Password"
                type="password"
                fullWidth
                {...register("password")}
                error={!!errors.password}
                helperText={errors.password?.message}
              />
            </Grid>
            <Grid item xs={12} md={6}>
              <TextField
                label="Confirmar password"
                type="password"
                fullWidth
                {...register("confirmPassword")}
                error={!!errors.confirmPassword}
                helperText={errors.confirmPassword?.message}
              />
            </Grid>
          </>
        )}

        <Grid item xs={12}>
          <Button
            type="submit"
            variant="contained"
            size="large"
            disabled={submitting}
          >
            {submitting
              ? "Guardando..."
              : mode === "create"
                ? "Crear usuario"
                : "Guardar cambios"}
          </Button>
        </Grid>
      </Grid>
    </Box>
  );
}
