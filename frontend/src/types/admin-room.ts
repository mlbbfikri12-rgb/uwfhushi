export interface AdminRoomType {
    id: string;
    name: string;
    description: string;
    basePrice: number;
    maxAdults?: number;
    maxChildren?: number;
}

export interface AdminRoomImage {
    id: string;
    url: string;
    format: string;
}

export interface AdminRoom {
    id: string;
    roomNumber: string;
    status: string;

    roomType: AdminRoomType | null; // 🔥 beda dari client
    images: AdminRoomImage[];
}


