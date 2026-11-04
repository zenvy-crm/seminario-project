import { useState } from "react";
import { Box, Drawer, Toolbar } from "@mui/material";
import { Outlet } from "react-router-dom";
import Header from "./Header";
import Sidebar from "./Sidebar";
import { industryColors } from "../../theme/industryTheme";

const DRAWER_WIDTH = 240;

export default function MainLayout() {
  const [mobileOpen, setMobileOpen] = useState(false);

  return (
    <Box display="flex" minHeight="100vh" bgcolor={industryColors.bg}>
      <Header onMenuClick={() => setMobileOpen(true)} />

      <Drawer
        variant="permanent"
        sx={{
          display: { xs: "none", md: "block" },
          width: DRAWER_WIDTH,
          "& .MuiDrawer-paper": {
            width: DRAWER_WIDTH,
            boxSizing: "border-box",
            borderRight: `1px solid ${industryColors.border}`,
          },
        }}
      >
        <Toolbar />
        <Sidebar />
      </Drawer>

      <Drawer
        variant="temporary"
        open={mobileOpen}
        onClose={() => setMobileOpen(false)}
        sx={{
          display: { xs: "block", md: "none" },
          "& .MuiDrawer-paper": { width: DRAWER_WIDTH },
        }}
      >
        <Sidebar onNavigate={() => setMobileOpen(false)} />
      </Drawer>

      <Box
        component="main"
        flexGrow={1}
        sx={{ width: { md: `calc(100% - ${DRAWER_WIDTH}px)` } }}
      >
        <Toolbar />
        <Box p={{ xs: 2, md: 4 }}>
          <Outlet />
        </Box>
      </Box>
    </Box>
  );
}
