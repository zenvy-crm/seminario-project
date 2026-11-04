import { useEffect, useState, useCallback } from "react";
import {
  Box,
  Typography,
  TextField,
  MenuItem,
  Select,
  Button,
  Table,
  TableHead,
  TableBody,
  TableRow,
  TableCell,
  IconButton,
  Menu,
  TablePagination,
  InputAdornment,
} from "@mui/material";
import { Search, MoreVertical, Plus } from "lucide-react";
import { useNavigate } from "react-router-dom";
import { usersApi } from "../../api/usersApi";
import { User, Role, UserStatus } from "../../types";
import {
  RoleBadge,
  StatusBadge,
  LockedBadge,
} from "../../components/common/Badges";
import {
  LoadingState,
  EmptyState,
  ErrorState,
} from "../../components/common/StateViews";
import ConfirmDialog from "../../components/common/ConfirmDialog";
import { useToast } from "../../components/common/ToastProvider";
import { industryColors } from "../../theme/industryTheme";

type PendingAction =
  | { type: "status" | "role"; user: User; value: UserStatus | Role }
  | { type: "unlock"; user: User }
  | null;

export default function UsersList() {
  const navigate = useNavigate();
  const { showToast } = useToast();
  const [users, setUsers] = useState<User[] | null>(null);
  const [total, setTotal] = useState(0);
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState("");
  const [role, setRole] = useState<Role | "ALL">("ALL");
  const [status, setStatus] = useState<UserStatus | "ALL">("ALL");
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(5);
  const [menuAnchor, setMenuAnchor] = useState<{
    el: HTMLElement;
    user: User;
  } | null>(null);
  const [pending, setPending] = useState<PendingAction>(null);

  const load = useCallback(async () => {
    setError(null);
    try {
      const res = await usersApi.getUsers({
        search,
        role,
        status,
        page: page + 1,
        pageSize,
      });
      setUsers(res.items);
      setTotal(res.total);
    } catch {
      setError("No fue posible cargar la información.");
    }
  }, [search, role, status, page, pageSize]);

  useEffect(() => {
    load();
  }, [load]);

  const applyAction = async () => {
    if (!pending) return;
    try {
      if (pending.type === "status")
        await usersApi.changeStatus(
          pending.user.id,
          pending.value as UserStatus
        );
      else if (pending.type === "role")
        await usersApi.changeRole(pending.user.id, pending.value as Role);
      else await usersApi.unlockUser(pending.user.id);
      showToast("Usuario actualizado correctamente.");
      setPending(null);
      load();
    } catch {
      showToast("No fue posible completar la acción.", "error");
    }
  };

  return (
    <Box>
      <Box
        display="flex"
        justifyContent="space-between"
        alignItems="center"
        mb={3}
        flexWrap="wrap"
        gap={2}
      >
        <Typography variant="h4">Usuarios</Typography>
        <Button
          variant="contained"
          startIcon={<Plus size={16} strokeWidth={1.5} />}
          onClick={() => navigate("/users/create")}
        >
          Crear usuario
        </Button>
      </Box>

      <Box display="flex" gap={2} mb={2} flexWrap="wrap">
        <TextField
          placeholder="Buscar usuario..."
          value={search}
          onChange={(e) => {
            setSearch(e.target.value);
            setPage(0);
          }}
          size="small"
          InputProps={{
            startAdornment: (
              <InputAdornment position="start">
                <Search size={16} strokeWidth={1.5} />
              </InputAdornment>
            ),
          }}
          sx={{ minWidth: 240 }}
        />
        <Select
          size="small"
          value={role}
          onChange={(e) => {
            setRole(e.target.value as Role | "ALL");
            setPage(0);
          }}
          sx={{ minWidth: 140 }}
        >
          <MenuItem value="ALL">Todos los roles</MenuItem>
          <MenuItem value="ADMIN">ADMIN</MenuItem>
          <MenuItem value="INTERMEDIO">INTERMEDIO</MenuItem>
          <MenuItem value="USER">USER</MenuItem>
        </Select>
        <Select
          size="small"
          value={status}
          onChange={(e) => {
            setStatus(e.target.value as UserStatus | "ALL");
            setPage(0);
          }}
          sx={{ minWidth: 140 }}
        >
          <MenuItem value="ALL">Todos los estados</MenuItem>
          <MenuItem value="ACTIVE">Activo</MenuItem>
          <MenuItem value="INACTIVE">Inactivo</MenuItem>
        </Select>
      </Box>

      <Box
        border={`1px solid ${industryColors.border}`}
        bgcolor="background.paper"
      >
        {users === null && !error && <LoadingState />}
        {error && <ErrorState message={error} onRetry={load} />}
        {users !== null && users.length === 0 && !error && (
          <EmptyState message="No existen usuarios registrados." />
        )}
        {users !== null && users.length > 0 && (
          <Box sx={{ overflowX: "auto" }}>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>ID</TableCell>
                  <TableCell>Nombre</TableCell>
                  <TableCell>Email</TableCell>
                  <TableCell>Rol</TableCell>
                  <TableCell>Estado</TableCell>
                  <TableCell>Creado</TableCell>
                  <TableCell align="right">Acciones</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {users.map((u) => (
                  <TableRow key={u.id} hover>
                    <TableCell>{u.id}</TableCell>
                    <TableCell>
                      {u.firstName} {u.lastName}
                    </TableCell>
                    <TableCell>{u.email}</TableCell>
                    <TableCell>
                      <RoleBadge role={u.role} />
                    </TableCell>
                    <TableCell>
                      <Box display="flex" gap={0.5} flexWrap="wrap">
                        <StatusBadge status={u.status} />
                        {u.isLocked && <LockedBadge />}
                      </Box>
                    </TableCell>
                    <TableCell>
                      {new Date(u.createdAt).toLocaleDateString("es-ES")}
                    </TableCell>
                    <TableCell align="right">
                      <IconButton
                        size="small"
                        onClick={(e) =>
                          setMenuAnchor({ el: e.currentTarget, user: u })
                        }
                      >
                        <MoreVertical size={16} strokeWidth={1.5} />
                      </IconButton>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </Box>
        )}
        <TablePagination
          component="div"
          count={total}
          page={page}
          onPageChange={(_, p) => setPage(p)}
          rowsPerPage={pageSize}
          onRowsPerPageChange={(e) => {
            setPageSize(Number.parseInt(e.target.value, 10));
            setPage(0);
          }}
          rowsPerPageOptions={[5, 10, 25]}
          labelRowsPerPage="Filas por página"
        />
      </Box>

      <Menu
        anchorEl={menuAnchor?.el}
        open={!!menuAnchor}
        onClose={() => setMenuAnchor(null)}
      >
        <MenuItem
          onClick={() => {
            navigate(`/users/${menuAnchor!.user.id}`);
            setMenuAnchor(null);
          }}
        >
          Ver
        </MenuItem>
        <MenuItem
          onClick={() => {
            navigate(`/users/${menuAnchor!.user.id}/edit`);
            setMenuAnchor(null);
          }}
        >
          Editar
        </MenuItem>
        <MenuItem
          onClick={() => {
            setPending({
              type: "status",
              user: menuAnchor!.user,
              value:
                menuAnchor!.user.status === "ACTIVE" ? "INACTIVE" : "ACTIVE",
            });
            setMenuAnchor(null);
          }}
        >
          {menuAnchor?.user.status === "ACTIVE" ? "Desactivar" : "Activar"}
        </MenuItem>
        <MenuItem
          onClick={() => {
            setPending({
              type: "role",
              user: menuAnchor!.user,
              value: menuAnchor!.user.role === "ADMIN" ? "USER" : "ADMIN",
            });
            setMenuAnchor(null);
          }}
        >
          Cambiar rol
        </MenuItem>
        {menuAnchor?.user.isLocked && (
          <MenuItem
            onClick={() => {
              setPending({ type: "unlock", user: menuAnchor!.user });
              setMenuAnchor(null);
            }}
          >
            Desbloquear
          </MenuItem>
        )}
      </Menu>

      <ConfirmDialog
        open={!!pending}
        title={
          pending?.type === "status"
            ? pending.value === "INACTIVE"
              ? "¿Deseas desactivar este usuario?"
              : "¿Deseas activar este usuario?"
            : pending?.type === "role"
              ? "¿Deseas cambiar el rol de este usuario?"
              : "¿Deseas desbloquear este usuario?"
        }
        description={
          pending?.type === "status"
            ? "Esta acción impedirá que el usuario pueda iniciar sesión."
            : pending?.type === "role"
              ? `El usuario pasará a tener el rol ${pending.value}.`
              : "Se reiniciará el contador de intentos fallidos y podrá iniciar sesión de inmediato."
        }
        onCancel={() => setPending(null)}
        onConfirm={applyAction}
      />
    </Box>
  );
}
