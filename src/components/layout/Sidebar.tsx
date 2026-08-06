import { List, ListItemButton, ListItemIcon, ListItemText, Box, Typography, Divider, Button } from "@mui/material";
import { useNavigate, useLocation } from "react-router-dom";
import { Home, User, Users, LogOut } from "lucide-react";
import { useAuth } from "../../auth/AuthContext";
import { industryColors } from "../../theme/industryTheme";

export default function Sidebar({ onNavigate }: { onNavigate?: () => void }) {
  const { user, role, logout } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const items = [
    { to: "/dashboard", label: "Dashboard", icon: <Home size={18} strokeWidth={1.5} /> },
    { to: "/profile", label: "Mi perfil", icon: <User size={18} strokeWidth={1.5} /> },
    ...(role === "ADMIN" ? [{ to: "/users", label: "Usuarios", icon: <Users size={18} strokeWidth={1.5} /> }] : []),
  ];

  return (
    <Box display="flex" flexDirection="column" height="100%" bgcolor="background.paper">
      <Box p={2.5} borderBottom={`1px solid ${industryColors.border}`}>
        <Typography variant="h6" sx={{ color: industryColors.accent700 }}>INDUSTRY</Typography>
        <Typography variant="caption" color="text.secondary">Panel administrativo</Typography>
      </Box>

      <List sx={{ flexGrow: 1, py: 1 }}>
        {items.map((item) => {
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
        })}
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
