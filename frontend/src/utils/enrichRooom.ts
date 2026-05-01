import { Room } from "@/types/room";

export function enrichRoom(room: Room) {
    const dummyImages = [
        "https://images.unsplash.com/photo-1631049307264-da0ec9d70304",
        "https://images.unsplash.com/photo-1566665797739-1674de7a421a",
        "https://images.unsplash.com/photo-1582719508461-905c673771fd",
    ];

    const dummyFacilities = [
        "Free WiFi",
        "AC",
        "TV",
        "Breakfast",
        "Hot Shower",
    ];

    return {
        ...room,
        roomType: {
            ...room.roomType,
            image:
                room.roomType.image ??
                dummyImages[Math.floor(Math.random() * dummyImages.length)],
            facilities: room.roomType.facilities ?? dummyFacilities,
            capacity: room.roomType.capacity ?? 2,
            bedType: room.roomType.bedType ?? "King Bed",
            size: room.roomType.size ?? 24,
        },
    };
}