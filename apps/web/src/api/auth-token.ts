const STORAGE_KEY = "vi-ai-studio:auth";

export const AUTH_EXPIRED_EVENT = "vi-ai-studio:auth-expired";

export interface AuthenticatedUser {
  id: number;
  email: string;
  fullName: string | null;
  avatarUrl: string | null;
  roles: string[];
}

export interface StoredAuth {
  token: string;
  expiresAt: string;
  user: AuthenticatedUser;
}

function loadFromStorage(): StoredAuth | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    return raw ? (JSON.parse(raw) as StoredAuth) : null;
  } catch {
    return null;
  }
}

let current: StoredAuth | null = loadFromStorage();

export function getAuth(): StoredAuth | null {
  return current;
}

export function setAuth(auth: StoredAuth) {
  current = auth;
  localStorage.setItem(STORAGE_KEY, JSON.stringify(auth));
}

export function clearAuth() {
  current = null;
  localStorage.removeItem(STORAGE_KEY);
  window.dispatchEvent(new Event(AUTH_EXPIRED_EVENT));
}
