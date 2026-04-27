/** Set by AuthProvider so api modules can attach Bearer tokens to API calls. */
let getIdToken: () => Promise<string | null> = async () => null

export function setIdTokenGetter(fn: () => Promise<string | null>): void {
  getIdToken = fn
}

export async function getAuthHeaders(): Promise<HeadersInit> {
  const token = await getIdToken()
  if (!token) return {}
  return { Authorization: `Bearer ${token}` }
}
