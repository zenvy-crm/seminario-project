import { List, ListItemButton, ListItemIcon, ListItemText, ListSubheader, Box, Typography, Divider, Button } from "@mui/material";
import { useNavigate, useLocation } from "react-router-dom";
import { Home, User, Users, LogOut, ClipboardList, FileSpreadsheet, Trophy, FileBarChart, ShieldAlert } from "lucide-react";
import { useAuth } from "../../auth/AuthContext";
import { NIVEL_ROL } from "../../types";
import { industryColors } from "../../theme/industryTheme";

export default function Sidebar({ onNavigate }: { onNavigate?: () => void }) {
  const { user, role, logout } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const items = [
    { to: "/dashboard", label: "Dashboard", icon: <Home size={18} strokeWidth={1.5} /> },
    { to: "/ordenes-compra", label: "Orden de Compra", icon: <FileSpreadsheet size={18} strokeWidth={1.5} /> },
    { to: "/operaciones", label: "Operaciones", icon: <ClipboardList size={18} strokeWidth={1.5} /> },
    { to: "/profile", label: "Mi perfil", icon: <User size={18} strokeWidth={1.5} /> },
    ...(role === "ADMIN" ? [{ to: "/users", label: "Usuarios", icon: <Users size={18} strokeWidth={1.5} /> }] : []),
  ];

  // Grupo "Reportes": ranking y reporte de ordenes para todos los niveles, auditoria solo para el nivel 3 (Administrador).
  const nivel = role ? NIVEL_ROL[role] : 1;
  const reportes = [
    { to: "/reportes/ranking", label: "Ranking de proveedores", icon: <Trophy size={18} strokeWidth={1.5} /> },
    { to: "/reportes/ordenes", label: "Reporte de órdenes", icon: <FileBarChart size={18} strokeWidth={1.5} /> },
    ...(nivel === 3 ? [{ to: "/reportes/auditoria", label: "Auditoría de anulaciones", icon: <ShieldAlert size={18} strokeWidth={1.5} /> }] : []),
  ];

  const renderItem = (item: { to: string; label: string; icon: JSX.Element }) => {
    const isActive = location.pathname === item.to;
    return (
      <ListItemButton
        key={item.to}
        onClick={() => {
          navigate(item.to);
          onNavigate?.();
        }}
        sx={{
          py: 1,
          color: industryColors.text,
          ...(isActive && {
            bgcolor: industryColors.accent100,
            color: industryColors.accent700,
            borderRight: `2px solid ${industryColors.accent700}`,
          }),
        }}
      >
        <ListItemIcon sx={{ minWidth: 36, color: "inherit" }}>{item.icon}</ListItemIcon>
        <ListItemText primaryTypographyProps={{ fontSize: 14 }} primary={item.label} />
      </ListItemButton>
    );
  };

  return (
    <Box display="flex" flexDirection="column" height="100%" bgcolor="background.paper">
      <Box p={2.5} borderBottom={`1px solid ${industryColors.border}`}>
        <Typography variant="h6" sx={{ color: industryColors.accent700 }}>INDUSTRY</Typography>
        <Typography variant="caption" color="text.secondary">Panel administrativo</Typography>
      </Box>

      <List sx={{ flexGrow: 1, py: 1 }}>
        {items.map(renderItem)}
        <ListSubheader
          disableSticky
          sx={{ mt: 1, lineHeight: "32px", fontSize: 12, fontWeight: 600, letterSpacing: 0.5, textTransform: "uppercase", color: "text.secondary" }}
        >
          Reportes
        </ListSubheader>
        {reportes.map(renderItem)}
      </List>

      <Divider />
      <Box p={2}>
        <Typography variant="body2" fontWeight={600}>{user?.firstName} {user?.lastName}</Typography>
        <Typography variant="caption" color="text.secondary">{role}</Typography>
      </Box>
      <Box borderTop={`1px solid ${industryColors.border}`} px={1.5} py={1}>
        <Button
          onClick={() => { logout(); navigate("/login"); }}
          startIcon={<LogOut size={18} strokeWidth={1.5} />}
          sx={{
            textTransform: "none",
            justifyContent: "flex-start",
            color: industryColors.text,
            fontSize: 14,
            width: "100%",
            "&:hover": { bgcolor: "rgba(0,0,0,0.04)" }
          }}
        >
          Cerrar sesión
        </Button>
      </Box>
    </Box>
  );
}
