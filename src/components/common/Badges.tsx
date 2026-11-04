import { Chip } from "@mui/material";
import { Role, UserStatus } from "../../types";

export function RoleBadge({ role }: { role: Role }) {
  return <Chip label={role} size="small" color={role === "USER" ? "default" : "primary"} variant={role === "ADMIN" ? "filled" : "outlined"} />;
}

export function StatusBadge({ status }: { status: UserStatus }) {
  return <Chip label={status === "ACTIVE" ? "Activo" : "Inactivo"} size="small" color={status === "ACTIVE" ? "success" : "default"} variant="outlined" />;
}

export function LockedBadge() {
  return <Chip label="Bloqueado" size="small" color="error" variant="filled" />;
}
