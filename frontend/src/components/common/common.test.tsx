import { act, fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { LockedBadge, RoleBadge, StatusBadge } from "./Badges";
import ConfirmDialog from "./ConfirmDialog";
import { ErrorBoundary } from "./ErrorBoundary";
import { EmptyState, ErrorState, LoadingState } from "./StateViews";
import { ToastProvider, useToast } from "./ToastProvider";

describe("componentes comunes", () => {
  it("FE-UI-01 las insignias muestran rol, estado y bloqueo", () => {
    render(
      <>
        <RoleBadge role="ADMIN" />
        <RoleBadge role="USER" />
        <StatusBadge status="ACTIVE" />
        <StatusBadge status="INACTIVE" />
        <LockedBadge />
      </>
    );
    expect(screen.getByText("ADMIN")).toBeInTheDocument();
    expect(screen.getByText("USER")).toBeInTheDocument();
    expect(screen.getByText("Activo")).toBeInTheDocument();
    expect(screen.getByText("Inactivo")).toBeInTheDocument();
    expect(screen.getByText("Bloqueado")).toBeInTheDocument();
  });

  it("FE-UI-02 vistas de estado: carga, vacio y error con reintento", () => {
    const reintentar = vi.fn();
    render(
      <>
        <LoadingState />
        <EmptyState message="Sin registros" />
        <ErrorState message="Fallo la carga" onRetry={reintentar} />
      </>
    );
    expect(screen.getByText("Cargando...")).toBeInTheDocument();
    expect(screen.getByText("Sin registros")).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Reintentar" }));
    expect(reintentar).toHaveBeenCalledTimes(1);
  });

  it("FE-UI-03 ErrorState sin onRetry no muestra el boton", () => {
    render(<ErrorState message="Error" />);
    expect(screen.queryByRole("button")).not.toBeInTheDocument();
  });

  it("FE-UI-04 ConfirmDialog ejecuta confirmar y cancelar", () => {
    const confirmar = vi.fn();
    const cancelar = vi.fn();
    render(
      <ConfirmDialog
        open
        title="Anular orden"
        description="Esta accion no se puede deshacer"
        confirmLabel="Anular"
        destructive
        onConfirm={confirmar}
        onCancel={cancelar}
      />
    );
    fireEvent.click(screen.getByRole("button", { name: "Anular" }));
    fireEvent.click(screen.getByRole("button", { name: "Cancelar" }));
    expect(confirmar).toHaveBeenCalledTimes(1);
    expect(cancelar).toHaveBeenCalledTimes(1);
  });

  it("FE-UI-05 ToastProvider muestra el mensaje y se puede cerrar", async () => {
    function Disparador() {
      const { showToast } = useToast();
      return (
        <button onClick={() => showToast("Guardado", "info")}>lanzar</button>
      );
    }
    render(
      <ToastProvider>
        <Disparador />
      </ToastProvider>
    );

    fireEvent.click(screen.getByText("lanzar"));
    expect(await screen.findByText("Guardado")).toBeInTheDocument();

    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: /close/i }));
    });
  });

  it("FE-UI-06 useToast fuera del proveedor lanza error", () => {
    vi.spyOn(console, "error").mockImplementation(() => {});
    function SinProveedor() {
      useToast();
      return null;
    }
    expect(() => render(<SinProveedor />)).toThrow(/ToastProvider/);
  });

  it("FE-UI-07 ErrorBoundary captura el error y permite reintentar", () => {
    vi.spyOn(console, "error").mockImplementation(() => {});
    let fallar = true;
    function Inestable() {
      if (fallar) throw new Error("explota");
      return <span>recuperado</span>;
    }
    render(
      <ErrorBoundary>
        <Inestable />
      </ErrorBoundary>
    );

    expect(screen.getByText("¡Algo salió mal!")).toBeInTheDocument();
    fallar = false;
    fireEvent.click(screen.getByRole("button", { name: "Intentar de nuevo" }));
    expect(screen.getByText("recuperado")).toBeInTheDocument();
  });
});
