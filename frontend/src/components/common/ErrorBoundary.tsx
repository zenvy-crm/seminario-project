import React from "react";
import { Box, Container, Typography, Button } from "@mui/material";
import { AlertTriangle } from "lucide-react";

interface Props {
  children: React.ReactNode;
}

interface State {
  hasError: boolean;
  error: Error | null;
}

export class ErrorBoundary extends React.Component<Props, State> {
  constructor(props: Props) {
    super(props);
    this.state = { hasError: false, error: null };
  }

  static getDerivedStateFromError(error: Error): State {
    return { hasError: true, error };
  }

  componentDidCatch(error: Error) {
    console.error("ErrorBoundary caught an error:", error);
  }

  handleReset = () => {
    this.setState({ hasError: false, error: null });
  };

  render() {
    if (this.state.hasError) {
      return (
        <Box
          sx={{
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            minHeight: "100vh",
            backgroundColor: "#f5f5f5",
          }}
        >
          <Container maxWidth="sm">
            <Box sx={{ textAlign: "center" }}>
              <AlertTriangle
                size={64}
                color="#d32f2f"
                style={{ marginBottom: 16 }}
              />
              <Typography variant="h4" gutterBottom sx={{ fontWeight: "bold" }}>
                ¡Algo salió mal!
              </Typography>
              <Typography
                variant="body1"
                sx={{ color: "#666", marginBottom: 2 }}
              >
                Disculpa, parece que hubo un error inesperado. Por favor,
                intenta recargar la página.
              </Typography>
              {process.env.NODE_ENV === "development" && (
                <Box
                  sx={{
                    backgroundColor: "#fff",
                    border: "1px solid #ddd",
                    borderRadius: 1,
                    padding: 2,
                    marginBottom: 2,
                    textAlign: "left",
                    overflowX: "auto",
                  }}
                >
                  <Typography
                    variant="caption"
                    component="pre"
                    sx={{ fontSize: "0.75rem" }}
                  >
                    {this.state.error?.toString()}
                  </Typography>
                </Box>
              )}
              <Button
                variant="contained"
                color="primary"
                onClick={this.handleReset}
              >
                Intentar de nuevo
              </Button>
            </Box>
          </Container>
        </Box>
      );
    }

    return this.props.children;
  }
}
