import { createTheme } from "@mui/material/styles";

export const industryColors = {
  bg: "#f2f2f3",
  surface: "#ffffff",
  text: "#1d1f20",
  textMuted: "#5a5f63",
  accent: "#5980a6",
  accent100: "#e6ebf0",
  accent300: "#b9c8d7",
  accent500: "#5980a6",
  accent700: "#3d5b78",
  accent900: "#26374a",
  border: "#c7cbcd",
  success: "#4c7a5a",
  danger: "#a4423a",
};

export const theme = createTheme({
  palette: {
    mode: "light",
    background: { default: industryColors.bg, paper: industryColors.surface },
    primary: { main: industryColors.accent, light: industryColors.accent300, dark: industryColors.accent700, contrastText: "#ffffff" },
    error: { main: industryColors.danger },
    success: { main: industryColors.success },
    text: { primary: industryColors.text, secondary: industryColors.textMuted },
    divider: industryColors.border,
  },
  typography: {
    fontFamily: '"Barlow", sans-serif',
    h1: { fontFamily: '"Barlow Condensed", sans-serif', fontWeight: 600 },
    h2: { fontFamily: '"Barlow Condensed", sans-serif', fontWeight: 600 },
    h3: { fontFamily: '"Barlow Condensed", sans-serif', fontWeight: 600 },
    h4: { fontFamily: '"Barlow Condensed", sans-serif', fontWeight: 600 },
    h5: { fontFamily: '"Barlow Condensed", sans-serif', fontWeight: 600, letterSpacing: 0.2 },
    h6: { fontFamily: '"Barlow Condensed", sans-serif', fontWeight: 600, letterSpacing: 0.2 },
    button: { fontFamily: '"Barlow Condensed", sans-serif', fontWeight: 600, letterSpacing: 0.5, textTransform: "uppercase" },
  },
  shape: { borderRadius: 0 },
  components: {
    MuiButton: {
      styleOverrides: {
        root: { borderRadius: 0, boxShadow: "none" },
        containedPrimary: { boxShadow: "none", "&:hover": { backgroundColor: industryColors.accent700, boxShadow: "none" } },
        outlined: { borderColor: industryColors.border },
      },
    },
    MuiPaper: { styleOverrides: { root: { borderRadius: 0, backgroundImage: "none" } } },
    MuiCard: { styleOverrides: { root: { borderRadius: 0, border: `1px solid ${industryColors.border}` } } },
    MuiTextField: { defaultProps: { size: "small" } },
    MuiOutlinedInput: { styleOverrides: { root: { borderRadius: 0 } } },
    MuiChip: { styleOverrides: { root: { borderRadius: 0 } } },
    MuiTableCell: { styleOverrides: { root: { borderColor: industryColors.border } } },
    MuiDialog: { styleOverrides: { paper: { borderRadius: 0, border: `1px solid ${industryColors.border}` } } },
    MuiDrawer: { styleOverrides: { paper: { borderRadius: 0 } } },
    MuiAlert: { styleOverrides: { root: { borderRadius: 0 } } },
  },
});
