import {
  AppBar,
  Toolbar,
  IconButton,
  Typography,
  Box,
  Avatar,
  Menu,
  MenuItem,
  Divider,
} from "@mui/material";
import { Menu as MenuIcon, ChevronDown, LogOut, User } from "lucide-react";
import { useState } from "react";
import { useAuth } from "../../auth/AuthContext";
import { useNavigate } from "react-router-dom";
import { industryColors } from "../../theme/industryTheme";

export default function Header({ onMenuClick }: { onMenuClick: () => void }) {
  const { user, role, logout } = useAuth();
  const navigate = useNavigate();
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);

  return (
    <AppBar
      position="fixed"
      elevation={0}
      sx={{
        bgcolor: "background.paper",
        color: industryColors.text,
        borderBottom: `1px solid ${industryColors.border}`,
        zIndex: (t) => t.zIndex.drawer + 1,
      }}
    >
      <Toolbar sx={{ gap: 2 }}>
        <IconButton onClick={onMenuClick} sx={{ display: { md: "none" } }}>
          <MenuIcon size={20} strokeWidth={1.5} />
        </IconButton>
        <Typography variant="h6" flexGrow={1} sx={{ fontSize: 18 }}>
          Sistema de Gestión
        </Typography>
        <Box
          display="flex"
          alignItems="center"
          gap={1}
          sx={{ cursor: "pointer" }}
          onClick={(e) => setAnchorEl(e.currentTarget)}
        >
          <Avatar
            sx={{
              width: 32,
              height: 32,
              bgcolor: industryColors.accent700,
              fontSize: 14,
            }}
          >
            {user?.firstName?.[0]}
            {user?.lastName?.[0]}
          </Avatar>
          <Box display={{ xs: "none", sm: "block" }}>
            <Typography variant="body2" fontWeight={600} lineHeight={1.2}>
              {user?.firstName} {user?.lastName}
            </Typography>
            <Typography variant="caption" color="text.secondary">
              {role}
            </Typography>
          </Box>
          <ChevronDown size={16} strokeWidth={1.5} />
        </Box>
        <Menu
          anchorEl={anchorEl}
          open={!!anchorEl}
          onClose={() => setAnchorEl(null)}
        >
          <MenuItem
            onClick={() => {
              setAnchorEl(null);
              navigate("/profile");
            }}
          >
            <User size={16} strokeWidth={1.5} style={{ marginRight: 8 }} /> Mi
            perfil
          </MenuItem>
          <Divider />
          <MenuItem
            onClick={() => {
              logout();
              navigate("/login");
            }}
          >
            <LogOut size={16} strokeWidth={1.5} style={{ marginRight: 8 }} />{" "}
            Cerrar sesión
          </MenuItem>
        </Menu>
      </Toolbar>
    </AppBar>
  );
}
