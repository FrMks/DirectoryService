import axios from "axios";
import { Address, Location } from "./type";
import { apiClient } from "@/shared/api/axios-instance";

export type CreateLocationRequest = {
    name: string;
    address: Address;
    timezone: string;
};

export const locationsApi = {
    getLocations: async (): Promise<Location[]> => {
        const response = await apiClient.get<Location[]>("/locations");

        return response.data;
    },

    createLocation: async (request: CreateLocationRequest) => {
        const response = await apiClient.post<Location>("/locations", request);
        
        return response.data;
    }
}