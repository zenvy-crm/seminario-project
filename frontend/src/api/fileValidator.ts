import { fileTypeFromBuffer } from "file-type";

const ALLOWED_MIME_TYPES = new Set(["application/pdf", "image/jpeg"]);
const ALLOWED_EXTENSIONS = ["pdf", "jpg", "jpeg"];

export async function validateFileType(
  file: File
): Promise<{ valid: boolean; error?: string }> {
  try {
    const buffer = await file.arrayBuffer();
    const fileType = await fileTypeFromBuffer(new Uint8Array(buffer));

    if (!fileType) {
      return { valid: false, error: "No se pudo detectar el tipo de archivo" };
    }

    if (!ALLOWED_MIME_TYPES.has(fileType.mime)) {
      return { valid: false, error: "Solo se permiten archivos PDF o JPEG" };
    }

    return { valid: true };
  } catch (error) {
    return { valid: false, error: "Error al validar el archivo" };
  }
}

export function validateFileExtension(fileName: string): boolean {
  const extension = fileName.split(".").pop()?.toLowerCase();
  return extension ? ALLOWED_EXTENSIONS.includes(extension) : false;
}
