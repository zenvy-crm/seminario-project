import { Box, BoxProps } from "@mui/material";
import { industryColors } from "./industryTheme";

const cornerStyle = {
  position: "absolute" as const,
  width: 10,
  height: 10,
  color: industryColors.accent700,
  fontSize: 14,
  lineHeight: "10px",
  fontFamily: "monospace",
  pointerEvents: "none" as const,
  userSelect: "none" as const,
};

export default function Blueprint({ children, sx, ...rest }: BoxProps) {
  return (
    <Box
      position="relative"
      border={`1px solid ${industryColors.border}`}
      sx={sx}
      {...rest}
    >
      <Box sx={{ ...cornerStyle, top: -6, left: -6 }}>+</Box>
      <Box sx={{ ...cornerStyle, top: -6, right: -6 }}>+</Box>
      <Box sx={{ ...cornerStyle, bottom: -6, left: -6 }}>+</Box>
      <Box sx={{ ...cornerStyle, bottom: -6, right: -6 }}>+</Box>
      {children}
    </Box>
  );
}
