
export function getHeaders(branchCode?: string): HeadersInit {
    const headers: HeadersInit = {
        "Content-Type": "application/json",
    };

    if (branchCode) {
        headers["X-Branch-Code"] = branchCode;
    }

    return headers;
}

export { api } from "./api";