import { Box, Typography, Button } from "@mui/material";
import { Link } from "react-router-dom";
import { industryColors } from "../../theme/industryTheme";

export default function NotFound() {
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
      <Typography variant="h3">404</Typography>
      <Typography variant="h6">Página no encontrada</Typography>
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
