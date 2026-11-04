import { Routes, Route, Navigate } from "react-router-dom";
import MainLayout from "./components/layout/MainLayout";
import ProtectedRoute from "./auth/ProtectedRoute";
import RoleRoute from "./auth/RoleRoute";
import Login from "./pages/auth/Login";
import Register from "./pages/auth/Register";
import Mfa from "./pages/auth/Mfa";
import Dashboard from "./pages/dashboard/Dashboard";
import Operaciones from "./pages/operaciones/Operaciones";
import OrdenCompraPage from "./pages/ordenes/OrdenCompraPage";
import RankingProveedoresPage from "./pages/reportes/RankingProveedoresPage";
import ReporteOrdenesPage from "./pages/reportes/ReporteOrdenesPage";
import AuditoriaAnulacionesPage from "./pages/reportes/AuditoriaAnulacionesPage";
import Profile from "./pages/profile/Profile";
import UsersList from "./pages/users/UsersList";
import UserCreate from "./pages/users/UserCreate";
import UserEdit from "./pages/users/UserEdit";
import UserDetail from "./pages/users/UserDetail";
import Forbidden from "./pages/errors/Forbidden";
import NotFound from "./pages/errors/NotFound";

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<Login />} />
      <Route path="/register" element={<Register />} />
      <Route path="/mfa" element={<Mfa />} />
      <Route path="/403" element={<Forbidden />} />

      <Route element={<ProtectedRoute />}>
        <Route element={<MainLayout />}>
          <Route path="/dashboard" element={<Dashboard />} />
          <Route path="/operaciones" element={<Operaciones />} />
          <Route path="/ordenes-compra" element={<OrdenCompraPage />} />
          <Route
            path="/reportes/ranking"
            element={<RankingProveedoresPage />}
          />
          <Route path="/reportes/ordenes" element={<ReporteOrdenesPage />} />
          {/* Sin RoleRoute a proposito: el 403 lo debe devolver el backend y mostrarse en la pagina. */}
          <Route
            path="/reportes/auditoria"
            element={<AuditoriaAnulacionesPage />}
          />
          <Route path="/profile" element={<Profile />} />

          <Route element={<RoleRoute allowed={["ADMIN"]} />}>
            <Route path="/users" element={<UsersList />} />
            <Route path="/users/create" element={<UserCreate />} />
            <Route path="/users/:id" element={<UserDetail />} />
            <Route path="/users/:id/edit" element={<UserEdit />} />
          </Route>
        </Route>
      </Route>

      <Route path="/" element={<Navigate to="/operaciones" replace />} />
      <Route path="*" element={<NotFound />} />
    </Routes>
  );
}
