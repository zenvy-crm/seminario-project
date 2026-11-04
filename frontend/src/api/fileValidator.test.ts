import { describe, expect, it, vi } from "vitest";
import { validateFileExtension, validateFileType } from "./fileValidator";

const PDF = new Uint8Array([
  0x25, 0x50, 0x44, 0x46, 0x2d, 0x31, 0x2e, 0x37, 0x0a, 0x25, 0xe2, 0xe3,
  0xcf, 0xd3, 0x0a,
]);
const JPEG = new Uint8Array([
  0xff, 0xd8, 0xff, 0xe0, 0x00, 0x10, 0x4a, 0x46, 0x49, 0x46, 0x00, 0x01,
]);
const PNG = new Uint8Array([
  0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a, 0x00, 0x00, 0x00, 0x0d,
  0x49, 0x48, 0x44, 0x52,
]);

describe("validador de archivos", () => {
  it.each([
    ["contrato.pdf", true],
    ["FOTO.JPG", true],
    ["imagen.jpeg", true],
    ["script.exe", false],
    ["sin-extension", false],
    ["doble.pdf.exe", false],
  ])("FE-VAL-01 extension de %s => %s", (nombre, esperado) => {
    expect(validateFileExtension(nombre)).toBe(esperado);
  });

  it("FE-VAL-02 acepta PDF y JPEG por su firma real", async () => {
    await expect(validateFileType(new File([PDF], "a.pdf"))).resolves.toEqual({
      valid: true,
    });
    await expect(validateFileType(new File([JPEG], "a.jpg"))).resolves.toEqual({
      valid: true,
    });
  });

  it("FE-VAL-03 rechaza un PNG renombrado y un archivo sin firma conocida", async () => {
    const png = await validateFileType(new File([PNG], "falso.pdf"));
    expect(png.valid).toBe(false);
    expect(png.error).toMatch(/PDF o JPEG/);

    const texto = await validateFileType(new File(["hola"], "a.pdf"));
    expect(texto).toEqual({
      valid: false,
      error: "No se pudo detectar el tipo de archivo",
    });
  });

  it("FE-VAL-04 si la lectura del archivo falla devuelve un error controlado", async () => {
    const roto = new File([PDF], "a.pdf");
    vi.spyOn(roto, "arrayBuffer").mockRejectedValue(new Error("io"));
    await expect(validateFileType(roto)).resolves.toEqual({
      valid: false,
      error: "Error al validar el archivo",
    });
  });
});
