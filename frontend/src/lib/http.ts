
export function getHeaders(branchCode?: string): HeadersInit {
    const headers: HeadersInit = {
        "Content-Type": "application/json",
        "ngrok-skip-browser-warning": "true",
    };

    if (branchCode) {
        headers["X-Branch-Code"] = branchCode;
    }

    return headers;
}

export { api } from "./api";