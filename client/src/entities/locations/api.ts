import { Location } from "./type";
import { GetLocationsResponse } from "./getLocationsResponse";
import { apiClient } from "@/shared/api/axios-instance";

export type CreateLocationRequest = {
    name: string;
    address: Location["address"];
    timezone: string;
};

export async function getLocations(): Promise<Location[]> {
    const response = await apiClient.get<GetLocationsResponse>("/locations");

    return response.data.locations.map((location) => ({
        id: location.id,
        name: location.name,
        address: {
            street: location.street,
            city: location.city,
            country: location.country,
        },
        timeZone: location.timezone,
        isActive: location.isActive,
        createdAt: location.createdAt,
        updatedAt: location.updatedAt,
    }));
}

export const locationsApi = {
    getLocations,

    createLocation: async (request: CreateLocationRequest) => {
        const response = await apiClient.post<Location>("/locations", request);

        return response.data;
    }
};
