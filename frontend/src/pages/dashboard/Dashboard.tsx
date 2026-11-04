import { Box, Typography, Grid, Paper } from "@mui/material";
import { useAuth } from "../../auth/AuthContext";
import { industryColors } from "../../theme/industryTheme";

export default function Dashboard() {
  const { user } = useAuth();
  return (
    <Box>
      <Typography variant="h4" mb={0.5}>
        Dashboard
      </Typography>
      <Typography variant="body1" color="text.secondary" mb={4}>
        Bienvenido, {user?.firstName}
      </Typography>

      <Grid container spacing={2}>
        {[1, 2, 3].map((i) => (
          <Grid item xs={12} md={4} key={i}>
            <Paper
              variant="outlined"
              sx={{
                p: 3,
                minHeight: 160,
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
                borderStyle: "dashed",
                borderColor: industryColors.border,
              }}
            >
              <Typography variant="body2" color="text.secondary">
                Espacio reservado para widgets
              </Typography>
            </Paper>
          </Grid>
        ))}
      </Grid>
    </Box>
  );
}
