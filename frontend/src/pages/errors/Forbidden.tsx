import { Box, Typography, Button } from "@mui/material";
import { Link } from "react-router-dom";
import { ShieldAlert } from "lucide-react";
import { industryColors } from "../../theme/industryTheme";

export default function Forbidden() {
  return (
    <Box
      minHeight="100vh"
      display="flex"
      flexDirection="column"
      alignItems="center"
      justifyContent="center"
      gap={2}
      bgcolor={industryColors.bg}
      p={2}
    >
      <ShieldAlert
        size={40}
        strokeWidth={1.5}
        color={industryColors.accent700}
      />
      <Typography variant="h3">403</Typography>
      <Typography variant="h6">Acceso denegado</Typography>
      <Typography color="text.secondary">
        No tienes permisos para acceder a este módulo.
      </Typography>
      <Button
        component={Link}
        to="/dashboard"
        variant="contained"
        sx={{ mt: 2 }}
      >
        Volver al dashboard
      </Button>
    </Box>
  );
}
