import { getHeaders } from "@/lib/http";
import { RatePlanAdmin, RatePlanForm } from "@/types/admin-rateplan";


export async function getRatePlansByRoomType(
    roomTypeId: string,
    branchCode: string
): Promise<RatePlanAdmin[]> {
    const res = await fetch(
        `$/api/admin/room-types/${roomTypeId}/rate-plans`,
        {
            headers: getHeaders(branchCode),
            credentials: "include",
        }
    );

    if (!res.ok) throw new Error("Failed to fetch rate plans");
    return res.json();
}

export async function createRatePlan(
    roomTypeId: string,
    branchCode: string,
    payload: RatePlanForm
): Promise<RatePlanAdmin> {
    const res = await fetch(
        `$/api/admin/room-types/${roomTypeId}/rate-plans`,
        {
            method: "POST",
            headers: getHeaders(branchCode),
            credentials: "include",
            body: JSON.stringify(payload),
        }
    );

    if (!res.ok) throw new Error("Failed to create");
    return res.json();
}

export async function updateRatePlan(
    id: string,
    branchCode: string,
    payload: RatePlanForm
): Promise<RatePlanAdmin> {
    const res = await fetch(`$/api/admin/rate-plans/${id}`, {
        method: "PUT",
        headers: getHeaders(branchCode),
        credentials: "include",
        body: JSON.stringify(payload),
    });

    if (!res.ok) throw new Error("Failed to update");
    return res.json();
}

export async function deleteRatePlan(id: string, branchCode: string) {
    const res = await fetch(`$/api/admin/rate-plans/${id}`, {
        method: "DELETE",
        headers: getHeaders(branchCode),
        credentials: "include",
    });

    if (!res.ok) throw new Error("Failed to delete");
}