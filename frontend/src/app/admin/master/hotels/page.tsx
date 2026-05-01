"use client";

import { useEffect, useMemo, useState } from "react";
import { useMutation, useQuery } from "@tanstack/react-query";
import { useRouter } from "next/navigation";
import { AdminLayout } from "@/components/admin/layout/AdminLayout";
import {
  addHotelImage,
  addNearbyPlace,
  createHotel,
  deleteHotel,
  deleteHotelImage,
  deleteNearbyPlace,
  getBrands,
  getBranches,
  getCities,
  getFacilities,
  getHotelById,
  getHotels,
  setHotelFacilities,
  updateHotel,
  uploadImage,
} from "@/services/admin.service";
import { getCurrentStaff } from "@/services/auth.service";

type HotelForm = {
  name: string;
  slug: string;
  branchCode: string;
  cityId: string;
  brandId: string;
  address: string;
  description: string;
  starRating: number;
  latitude: number;
  longitude: number;
  isActive: boolean;
};

const initialForm: HotelForm = {
  name: "",
  slug: "",
  branchCode: "",
  cityId: "",
  brandId: "",
  address: "",
  description: "",
  starRating: 4,
  latitude: 0,
  longitude: 0,
  isActive: true,
};

export default function AdminMasterHotelsPage() {
  const router = useRouter();
  const [q, setQ] = useState("");
  const [selectedHotelId, setSelectedHotelId] = useState<string | null>(null);
  const [form, setForm] = useState<HotelForm>(initialForm);
  const [imageUrl, setImageUrl] = useState("");
  const [imageSort] = useState(1);
  const [nearbyName, setNearbyName] = useState("");
  const [nearbyDistance, setNearbyDistance] = useState("");
  const [selectedFacilityIds, setSelectedFacilityIds] = useState<string[]>([]);

  const staffQuery = useQuery({
    queryKey: ["staff-me"],
    queryFn: getCurrentStaff,
    retry: false,
  });
  useEffect(() => {
    if (staffQuery.isError) router.replace("/admin/login");
  }, [staffQuery.isError, router]);

  const hotelsQuery = useQuery({
    queryKey: ["admin-hotels", q],
    queryFn: () => getHotels(q),
    enabled: !!staffQuery.data,
  });
  const citiesQuery = useQuery({
    queryKey: ["admin-cities-all"],
    queryFn: () => getCities(""),
    enabled: !!staffQuery.data,
  });
  const brandsQuery = useQuery({
    queryKey: ["admin-brands-all"],
    queryFn: () => getBrands(""),
    enabled: !!staffQuery.data,
  });
  const branchesQuery = useQuery({
    queryKey: ["admin-branches-all"],
    queryFn: getBranches,
    enabled: !!staffQuery.data,
  });
  const facilitiesQuery = useQuery({
    queryKey: ["admin-facilities-all"],
    queryFn: () => getFacilities(""),
    enabled: !!staffQuery.data,
  });
  const hotelDetailQuery = useQuery({
    queryKey: ["admin-hotel-detail", selectedHotelId],
    queryFn: () => getHotelById(selectedHotelId!),
    enabled: !!selectedHotelId,
  });

  const selectedHotel = hotelDetailQuery.data;

  useEffect(() => {
    if (!selectedHotel) return;
    setForm({
      name: selectedHotel.name,
      slug: selectedHotel.slug,
      branchCode: selectedHotel.branchCode,
      cityId: selectedHotel.cityId,
      brandId: selectedHotel.brandId ?? "",
      address: selectedHotel.address,
      description: selectedHotel.description,
      starRating: selectedHotel.starRating,
      latitude: selectedHotel.latitude,
      longitude: selectedHotel.longitude,
      isActive: selectedHotel.isActive,
    });
    setSelectedFacilityIds(selectedHotel.facilities.map((f) => f.facilityId));
  }, [selectedHotel]);

  const saveMutation = useMutation({
    mutationFn: async () => {
      const payload = {
        ...form,
        brandId: form.brandId || undefined,
      };
      if (selectedHotelId) return updateHotel(selectedHotelId, payload);
      return createHotel(payload);
    },
    onSuccess: async (saved) => {
      setSelectedHotelId(saved.id);
      await hotelsQuery.refetch();
      await hotelDetailQuery.refetch();
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteHotel(id),
    onSuccess: async () => {
      setSelectedHotelId(null);
      setForm(initialForm);
      await hotelsQuery.refetch();
    },
  });

  const addImageMutation = useMutation({
    mutationFn: () =>
      addHotelImage(selectedHotelId!, {
        url: imageUrl,
        isPrimary: true,
        sortOrder: imageSort,
      }),
    onSuccess: async () => {
      setImageUrl("");
      await hotelDetailQuery.refetch();
    },
  });

  const setFacilitiesMutation = useMutation({
    mutationFn: () => setHotelFacilities(selectedHotelId!, selectedFacilityIds),
    onSuccess: () => hotelDetailQuery.refetch(),
  });

  const addNearbyMutation = useMutation({
    mutationFn: () =>
      addNearbyPlace(selectedHotelId!, {
        name: nearbyName,
        distance: nearbyDistance,
      }),
    onSuccess: async () => {
      setNearbyName("");
      setNearbyDistance("");
      await hotelDetailQuery.refetch();
    },
  });

  const branchOptions = useMemo(
    () => branchesQuery.data ?? [],
    [branchesQuery.data],
  );

  if (!staffQuery.data) return null;

  return (
    <AdminLayout role={staffQuery.data.role}>
      <h1 className="text-xl font-semibold text-slate-900">Master Hotels</h1>
      <div className="mt-4 grid gap-4 lg:grid-cols-[1fr_1.4fr]">
        <div className="rounded-lg border bg-white p-4">
          <div className="mb-3 flex gap-2">
            <input
              value={q}
              onChange={(e) => setQ(e.target.value)}
              placeholder="Search hotel"
              className="w-full rounded border px-3 py-2 text-sm"
            />
            <button
              onClick={() => {
                setSelectedHotelId(null);
                setForm(initialForm);
              }}
              className="rounded border px-3 py-2 text-sm"
            >
              New
            </button>
          </div>
          <div className="space-y-2">
            {(hotelsQuery.data ?? []).map((hotel) => (
              <button
                key={hotel.id}
                onClick={() => setSelectedHotelId(hotel.id)}
                className="w-full rounded border p-3 text-left hover:bg-slate-50"
              >
                <p className="font-semibold">{hotel.name}</p>
                <p className="text-xs text-slate-500">
                  {hotel.branchCode} • {hotel.cityName}
                </p>
              </button>
            ))}
          </div>
        </div>

        <div className="space-y-4 rounded-lg border bg-white p-4">
          <h2 className="font-semibold">
            {selectedHotelId ? "Edit Hotel" : "Create Hotel"}
          </h2>
          <div className="grid gap-2 md:grid-cols-2">
            <input
              value={form.name}
              onChange={(e) => setForm((x) => ({ ...x, name: e.target.value }))}
              placeholder="Name"
              className="rounded border px-3 py-2 text-sm"
            />
            <input
              value={form.slug}
              onChange={(e) => setForm((x) => ({ ...x, slug: e.target.value }))}
              placeholder="Slug"
              className="rounded border px-3 py-2 text-sm"
            />
            <select
              value={form.branchCode}
              onChange={(e) =>
                setForm((x) => ({ ...x, branchCode: e.target.value }))
              }
              className="rounded border px-3 py-2 text-sm"
            >
              <option value="">Select branch</option>
              {branchOptions.map((branch) => (
                <option key={branch.id} value={branch.code}>
                  {branch.code} - {branch.name}
                </option>
              ))}
            </select>
            <select
              value={form.cityId}
              onChange={(e) =>
                setForm((x) => ({ ...x, cityId: e.target.value }))
              }
              className="rounded border px-3 py-2 text-sm"
            >
              <option value="">Select city</option>
              {(citiesQuery.data ?? []).map((city) => (
                <option key={city.id} value={city.id}>
                  {city.name}
                </option>
              ))}
            </select>
            <select
              value={form.brandId}
              onChange={(e) =>
                setForm((x) => ({ ...x, brandId: e.target.value }))
              }
              className="rounded border px-3 py-2 text-sm"
            >
              <option value="">No brand</option>
              {(brandsQuery.data ?? []).map((brand) => (
                <option key={brand.id} value={brand.id}>
                  {brand.name}
                </option>
              ))}
            </select>
            <input
              type="number"
              value={form.starRating}
              onChange={(e) =>
                setForm((x) => ({
                  ...x,
                  starRating: Number(e.target.value) || 1,
                }))
              }
              placeholder="Star rating"
              className="rounded border px-3 py-2 text-sm"
            />
            <input
              value={form.address}
              onChange={(e) =>
                setForm((x) => ({ ...x, address: e.target.value }))
              }
              placeholder="Address"
              className="rounded border px-3 py-2 text-sm md:col-span-2"
            />
            <textarea
              value={form.description}
              onChange={(e) =>
                setForm((x) => ({ ...x, description: e.target.value }))
              }
              placeholder="Description"
              className="rounded border px-3 py-2 text-sm md:col-span-2"
            />
            <input
              type="number"
              value={form.latitude}
              onChange={(e) =>
                setForm((x) => ({
                  ...x,
                  latitude: Number(e.target.value) || 0,
                }))
              }
              placeholder="Latitude"
              className="rounded border px-3 py-2 text-sm"
            />
            <input
              type="number"
              value={form.longitude}
              onChange={(e) =>
                setForm((x) => ({
                  ...x,
                  longitude: Number(e.target.value) || 0,
                }))
              }
              placeholder="Longitude"
              className="rounded border px-3 py-2 text-sm"
            />
          </div>
          <div className="flex gap-2">
            <button
              onClick={() => saveMutation.mutate()}
              className="rounded bg-[#1a1f3c] px-4 py-2 text-sm text-white"
            >
              Save
            </button>
            {selectedHotelId && (
              <button
                onClick={() => deleteMutation.mutate(selectedHotelId)}
                className="rounded border border-red-400 px-4 py-2 text-sm text-red-600"
              >
                Delete
              </button>
            )}
          </div>

          {selectedHotelId && selectedHotel && (
            <>
              <div className="border-t pt-4">
                <h3 className="mb-2 font-semibold">Hotel Images</h3>
                <div className="mb-2 flex flex-wrap gap-2">
                  <input
                    value={imageUrl}
                    onChange={(e) => setImageUrl(e.target.value)}
                    placeholder="Image URL"
                    className="rounded border px-3 py-2 text-sm"
                  />
                  <label className="rounded border px-3 py-2 text-sm">
                    Upload
                    <input
                      type="file"
                      className="hidden"
                      onChange={async (e) => {
                        const file = e.target.files?.[0];
                        if (!file) return;
                        const uploaded = await uploadImage(file, "hotels");
                        setImageUrl(uploaded.url);
                      }}
                    />
                  </label>
                  <button
                    onClick={() => addImageMutation.mutate()}
                    className="rounded bg-slate-800 px-3 py-2 text-sm text-white"
                  >
                    Add
                  </button>
                </div>
                <div className="grid gap-2 md:grid-cols-3">
                  {selectedHotel.images.map((image) => (
                    <div key={image.id} className="rounded border p-2">
                      <p className="truncate text-xs">{image.url}</p>
                      <button
                        onClick={() =>
                          deleteHotelImage(selectedHotel.id, image.id).then(
                            () => hotelDetailQuery.refetch(),
                          )
                        }
                        className="mt-2 text-xs text-red-600"
                      >
                        Delete
                      </button>
                    </div>
                  ))}
                </div>
              </div>

              <div className="border-t pt-4">
                <h3 className="mb-2 font-semibold">Hotel Facilities</h3>
                <div className="grid gap-2 md:grid-cols-3">
                  {(facilitiesQuery.data ?? []).map((facility) => (
                    <label
                      key={facility.id}
                      className="flex items-center gap-2 text-sm"
                    >
                      <input
                        type="checkbox"
                        checked={selectedFacilityIds.includes(facility.id)}
                        onChange={(e) => {
                          setSelectedFacilityIds((prev) =>
                            e.target.checked
                              ? [...prev, facility.id]
                              : prev.filter((x) => x !== facility.id),
                          );
                        }}
                      />
                      {facility.name}
                    </label>
                  ))}
                </div>
                <button
                  onClick={() => setFacilitiesMutation.mutate()}
                  className="mt-3 rounded bg-slate-800 px-3 py-2 text-sm text-white"
                >
                  Save Facilities
                </button>
              </div>

              <div className="border-t pt-4">
                <h3 className="mb-2 font-semibold">Nearby Places</h3>
                <div className="mb-2 flex flex-wrap gap-2">
                  <input
                    value={nearbyName}
                    onChange={(e) => setNearbyName(e.target.value)}
                    placeholder="Place name"
                    className="rounded border px-3 py-2 text-sm"
                  />
                  <input
                    value={nearbyDistance}
                    onChange={(e) => setNearbyDistance(e.target.value)}
                    placeholder="Distance (ex: 2 km)"
                    className="rounded border px-3 py-2 text-sm"
                  />
                  <button
                    onClick={() => addNearbyMutation.mutate()}
                    className="rounded bg-slate-800 px-3 py-2 text-sm text-white"
                  >
                    Add
                  </button>
                </div>
                <div className="space-y-1">
                  {selectedHotel.nearbyPlaces.map((place) => (
                    <div
                      key={place.id}
                      className="flex items-center justify-between rounded border px-3 py-2 text-sm"
                    >
                      <span>
                        {place.name} - {place.distance}
                      </span>
                      <button
                        onClick={() =>
                          deleteNearbyPlace(selectedHotel.id, place.id).then(
                            () => hotelDetailQuery.refetch(),
                          )
                        }
                        className="text-red-600"
                      >
                        Delete
                      </button>
                    </div>
                  ))}
                </div>
              </div>
            </>
          )}
        </div>
      </div>
    </AdminLayout>
  );
}
