import { useState } from "react";
import { Box, Typography } from "@mui/material";
import { useNavigate } from "react-router-dom";
import UserForm, { UserFormValues } from "../../components/users/UserForm";
import { usersApi } from "../../api/usersApi";
import { useToast } from "../../components/common/ToastProvider";

export default function UserCreate() {
  const navigate = useNavigate();
  const { showToast } = useToast();
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const handleSubmit = async (values: UserFormValues) => {
    setError(null);
    setSubmitting(true);
    try {
      await usersApi.createUser(values as any);
      showToast("Usuario creado correctamente.");
      navigate("/users");
    } catch {
      setError("No fue posible crear el usuario.");
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Box maxWidth={720}>
      <Typography variant="h4" mb={3}>Crear usuario</Typography>
      <UserForm mode="create" onSubmit={handleSubmit} submitError={error} submitting={submitting} />
    </Box>
  );
}
