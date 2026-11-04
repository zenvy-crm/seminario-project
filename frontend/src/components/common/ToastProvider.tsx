import { createContext, useCallback, useContext, useState, ReactNode } from "react";
import { Snackbar, Alert, AlertColor } from "@mui/material";

interface ToastContextValue {
  showToast: (message: string, severity?: AlertColor) => void;
}

const ToastContext = createContext<ToastContextValue | undefined>(undefined);

export function ToastProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<{ open: boolean; message: string; severity: AlertColor }>({ open: false, message: "", severity: "success" });

  const showToast = useCallback((message: string, severity: AlertColor = "success") => {
    setState({ open: true, message, severity });
  }, []);

  return (
    <ToastContext.Provider value={{ showToast }}>
      {children}
      <Snackbar open={state.open} autoHideDuration={4000} onClose={() => setState((s) => ({ ...s, open: false }))} anchorOrigin={{ vertical: "bottom", horizontal: "right" }}>
        <Alert severity={state.severity} variant="filled" onClose={() => setState((s) => ({ ...s, open: false }))} sx={{ borderRadius: 0 }}>
          {state.message}
        </Alert>
      </Snackbar>
    </ToastContext.Provider>
  );
}

export function useToast() {
  const ctx = useContext(ToastContext);
  if (!ctx) throw new Error("useToast must be used within ToastProvider");
  return ctx;
}
