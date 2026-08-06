import { api } from "./axios";
import { AppDocument } from "../types";
import { db } from "./mock/db";

const USE_MOCK = import.meta.env.VITE_USE_MOCK_API !== "false";
const delay = (ms = 700) => new Promise((r) => setTimeout(r, ms));

export const documentsApi = {
  async getDocuments(userId: number): Promise<AppDocument[]> {
    if (USE_MOCK) {
      await delay(300);
      return db.users.find((u) => u.id === userId)?.documents ?? [];
    }
    const { data } = await api.get<AppDocument[]>(`/users/${userId}/documents`);
    return data;
  },

  async upload(userId: number, file: File): Promise<AppDocument> {
    if (USE_MOCK) {
      await delay();
      const doc: AppDocument = { id: Date.now(), fileName: file.name, mimeType: file.type, sizeBytes: file.size, uploadedAt: new Date().toISOString(), url: "#" };
      const user = db.users.find((u) => u.id === userId);
      if (user) user.documents = [...(user.documents ?? []), doc];
      return doc;
    }
    const fd = new FormData();
    fd.append("file", file);
    const { data } = await api.post<AppDocument>(`/users/${userId}/documents`, fd, { headers: { "Content-Type": "multipart/form-data" } });
    return data;
  },

  async getBlob(userId: number, documentId: number): Promise<Blob> {
    if (USE_MOCK) {
      throw new Error("Descarga no disponible en modo mock.");
    }
    const { data } = await api.get(`/users/${userId}/documents/${documentId}/content`, { responseType: "blob" });
    return data;
  },

  async remove(userId: number, documentId: number): Promise<void> {
    if (USE_MOCK) {
      await delay(400);
      const user = db.users.find((u) => u.id === userId);
      if (user) user.documents = (user.documents ?? []).filter((d) => d.id !== documentId);
      return;
    }
    await api.delete(`/users/${userId}/documents/${documentId}`);
  },
};
