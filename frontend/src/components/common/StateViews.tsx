import { Box, CircularProgress, Typography, Button } from "@mui/material";

export function LoadingState({ label = "Cargando..." }: { label?: string }) {
  return (
    <Box
      display="flex"
      flexDirection="column"
      alignItems="center"
      gap={2}
      py={8}
    >
      <CircularProgress size={28} />
      <Typography color="text.secondary">{label}</Typography>
    </Box>
  );
}

export function EmptyState({ message }: { message: string }) {
  return (
    <Box display="flex" alignItems="center" justifyContent="center" py={8}>
      <Typography color="text.secondary">{message}</Typography>
    </Box>
  );
}

export function ErrorState({
  message,
  onRetry,
}: {
  message: string;
  onRetry?: () => void;
}) {
  return (
    <Box
      display="flex"
      flexDirection="column"
      alignItems="center"
      gap={2}
      py={8}
    >
      <Typography color="error">{message}</Typography>
      {onRetry && (
        <Button variant="outlined" onClick={onRetry}>
          Reintentar
        </Button>
      )}
    </Box>
  );
}
