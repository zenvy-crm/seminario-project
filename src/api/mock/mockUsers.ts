import { User } from "../../types";

export const mockUsers: User[] = [
  {
    id: 1, firstName: "Admin", lastName: "User", username: "admin", email: "admin@bowpi.com",
    phone: "+50588880000", identification: "0801199000011", role: "ADMIN", status: "ACTIVE",
    createdAt: "2024-01-10T09:00:00Z", lastLoginAt: "2026-08-04T15:20:00Z",
    documents: [{ id: 1, fileName: "identificacion-admin.pdf", mimeType: "application/pdf", sizeBytes: 1258291, uploadedAt: "2024-01-10T09:05:00Z", url: "#" }],
  },
  {
    id: 2, firstName: "Juan", lastName: "Pérez", username: "jperez", email: "jperez@bowpi.com",
    phone: "+50588881111", identification: "0801199000022", role: "USER", status: "ACTIVE",
    createdAt: "2024-02-14T11:30:00Z", lastLoginAt: "2026-08-05T08:10:00Z",
    documents: [{ id: 2, fileName: "identificacion-jperez.jpg", mimeType: "image/jpeg", sizeBytes: 842120, uploadedAt: "2024-02-14T11:32:00Z", url: "#" }],
  },
  {
    id: 3, firstName: "María", lastName: "Gómez", username: "mgomez", email: "mgomez@bowpi.com",
    phone: "+50588882222", identification: "0801199000033", role: "USER", status: "ACTIVE",
    createdAt: "2024-03-02T10:00:00Z", documents: [],
  },
  {
    id: 4, firstName: "Carlos", lastName: "Ramírez", username: "cramirez", email: "cramirez@bowpi.com",
    phone: "+50588883333", identification: "0801199000044", role: "USER", status: "INACTIVE",
    createdAt: "2024-04-19T14:45:00Z", documents: [],
  },
  {
    id: 5, firstName: "Lucía", lastName: "Fernández", username: "lfernandez", email: "lfernandez@bowpi.com",
    phone: "+50588884444", identification: "0801199000055", role: "ADMIN", status: "ACTIVE",
    createdAt: "2024-05-23T16:15:00Z", documents: [],
  },
  {
    id: 6, firstName: "Diego", lastName: "Martínez", username: "dmartinez", email: "dmartinez@bowpi.com",
    phone: "+50588885555", identification: "0801199000066", role: "USER", status: "INACTIVE",
    createdAt: "2024-06-30T08:20:00Z", documents: [],
  },
];
