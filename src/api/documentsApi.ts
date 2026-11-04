import { api } from "./axios";
import { AppDocument } from "../types";
import { validateFileType, validateFileExtension } from "./fileValidator";

export const documentsApi = {
  async getDocuments(userId: number): Promise<AppDocument[]> {
    const { data } = await api.get<AppDocument[]>(`/users/${userId}/documents`);
    return data;
  },

  async upload(userId: number, file: File): Promise<AppDocument> {
    // Validar extensión del archivo
    if (!validateFileExtension(file.name)) {
      throw new Error('Solo se permiten archivos PDF o JPEG');
    }

    // Validar tipo MIME real del archivo
    const validation = await validateFileType(file);
    if (!validation.valid) {
      throw new Error(validation.error || 'Error al validar el archivo');
    }

    const fd = new FormData();
    fd.append("file", file);
    const { data } = await api.post<AppDocument>(`/users/${userId}/documents`, fd, { headers: { "Content-Type": "multipart/form-data" } });
    return data;
  },

  async getBlob(userId: number, documentId: number): Promise<Blob> {
    const { data } = await api.get(`/users/${userId}/documents/${documentId}/content`, { responseType: "blob" });
    return data;
  },

  async remove(userId: number, documentId: number): Promise<void> {
    await api.delete(`/users/${userId}/documents/${documentId}`);
  },
};
