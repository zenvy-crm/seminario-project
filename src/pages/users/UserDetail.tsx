import { useEffect, useState } from "react";
import { Box, Typography, Grid, Button, Divider } from "@mui/material";
import { useNavigate, useParams } from "react-router-dom";
import { FileText } from "lucide-react";
import { usersApi } from "../../api/usersApi";
import { User } from "../../types";
import { RoleBadge, StatusBadge } from "../../components/common/Badges";
import { LoadingState, ErrorState, EmptyState } from "../../components/common/StateViews";
import Blueprint from "../../theme/Blueprint";
import { industryColors } from "../../theme/industryTheme";

function formatBytes(bytes: number) {
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

export default function UserDetail() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [user, setUser] = useState<User | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    usersApi.getUser(Number(id)).then(setUser).catch(() => setError("No fue posible cargar la información."));
  }, [id]);

  if (error) return <ErrorState message={error} />;
  if (!user) return <LoadingState />;

  return (
    <Box maxWidth={720}>
      <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
        <Typography variant="h4">Detalle de usuario</Typography>
        <Button variant="outlined" onClick={() => navigate(`/users/${id}/edit`)}>Editar</Button>
      </Box>

      <Blueprint sx={{ p: 3, mb: 3, bgcolor: "background.paper" }}>
        <Grid container spacing={2}>
          <Grid item xs={6}><Typography variant="caption" color="text.secondary">Nombre</Typography><Typography>{user.firstName} {user.lastName}</Typography></Grid>
          <Grid item xs={6}><Typography variant="caption" color="text.secondary">Username</Typography><Typography>{user.username}</Typography></Grid>
          <Grid item xs={6}><Typography variant="caption" color="text.secondary">Email</Typography><Typography>{user.email}</Typography></Grid>
          <Grid item xs={6}><Typography variant="caption" color="text.secondary">Teléfono</Typography><Typography>{user.phone ?? "—"}</Typography></Grid>
          <Grid item xs={6}><Typography variant="caption" color="text.secondary">Rol</Typography><Box mt={0.5}><RoleBadge role={user.role} /></Box></Grid>
          <Grid item xs={6}><Typography variant="caption" color="text.secondary">Estado</Typography><Box mt={0.5}><StatusBadge status={user.status} /></Box></Grid>
          <Grid item xs={6}><Typography variant="caption" color="text.secondary">Fecha de creación</Typography><Typography>{new Date(user.createdAt).toLocaleDateString("es-ES")}</Typography></Grid>
        </Grid>
      </Blueprint>

      <Typography variant="h6" mb={1.5}>Documentos</Typography>
      <Blueprint sx={{ p: 3, bgcolor: "background.paper" }}>
        {(user.documents ?? []).length === 0 && <EmptyState message="Este usuario no tiene documentos cargados." />}
        {(user.documents ?? []).map((doc, i) => (
          <Box key={doc.id}>
            {i > 0 && <Divider sx={{ my: 2 }} />}
            <Box display="flex" alignItems="center" gap={2} flexWrap="wrap">
              <FileText size={20} strokeWidth={1.5} color={industryColors.accent700} />
              <Box flexGrow={1}>
                <Typography variant="body2" fontWeight={600}>{doc.fileName}</Typography>
                <Typography variant="caption" color="text.secondary">{formatBytes(doc.sizeBytes)}</Typography>
              </Box>
              <Button size="small" variant="outlined">Ver</Button>
              <Button size="small" variant="outlined">Descargar</Button>
            </Box>
          </Box>
        ))}
      </Blueprint>
    </Box>
  );
}
