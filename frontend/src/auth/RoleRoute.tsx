import { Navigate, Outlet } from "react-router-dom";
import { Role } from "../types";
import { useAuth } from "./AuthContext";

export default function RoleRoute({ allowed }: { allowed: Role[] }) {
  const { role } = useAuth();
  if (!role || !allowed.includes(role)) return <Navigate to="/403" replace />;
  return <Outlet />;
}
