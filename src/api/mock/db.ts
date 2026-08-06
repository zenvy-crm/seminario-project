import { User } from "../../types";
import { mockUsers } from "./mockUsers";

export const db = { users: [...mockUsers] as User[] };
