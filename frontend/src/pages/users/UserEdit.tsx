import { useEffect, useState } from "react";
import { Box, Typography } from "@mui/material";
import { useNavigate, useParams } from "react-router-dom";
import UserForm, { UserFormValues } from "../../components/users/UserForm";
import { usersApi } from "../../api/usersApi";
import { User } from "../../types";
import { LoadingState, ErrorState } from "../../components/common/StateViews";
import { useToast } from "../../components/common/ToastProvider";

export default function UserEdit() {
  const { id } = useParams();
  const navigate = useNavigate();
  const { showToast } = useToast();
  const [user, setUser] = useState<User | null>(null);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    usersApi
      .getUser(Number(id))
      .then(setUser)
      .catch(() => setLoadError("No fue posible cargar la información."));
  }, [id]);

  const handleSubmit = async (values: UserFormValues) => {
    setSubmitError(null);
    setSubmitting(true);
    try {
      await usersApi.updateUser(Number(id), values);
      showToast("Usuario actualizado correctamente.");
      navigate("/users");
    } catch {
      setSubmitError("No fue posible guardar los cambios.");
    } finally {
      setSubmitting(false);
    }
  };

  if (loadError) return <ErrorState message={loadError} />;
  if (!user) return <LoadingState />;

  return (
    <Box maxWidth={720}>
      <Typography variant="h4" mb={3}>
        Editar usuario
      </Typography>
      <UserForm
        mode="edit"
        defaultValues={user}
        onSubmit={handleSubmit}
        submitError={submitError}
        submitting={submitting}
      />
    </Box>
  );
}
