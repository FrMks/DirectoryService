import { Location } from "./type";
import { GetLocationsResponse, LocationResponse } from "./getLocationsResponse";
import { apiClient } from "@/shared/api/axios-instance";
import { Envelope } from "@/shared/api/envelope";
import { PaginationResponse } from "@/shared/api/types";
import axios, { AxiosError } from "axios";

export type CreateLocationRequest = {
  name: string;
  address: Location["address"];
  timezone: string;
};

export type GetLocationsParams = {
  page?: number;
  pageSize?: number;
  sortBy?:
    | "Name"
    | "Street"
    | "City"
    | "Country"
    | "IsActive"
    | "CreatedAt"
    | "UpdatedAt";
  sortDirection?: "asc" | "desc";
};

export async function getLocations(
  params?: GetLocationsParams,
): Promise<PaginationResponse<Location>> {
  const response = await apiClient.get<Envelope<GetLocationsResponse>>(
    "/locations",
    {
      params: {
        "Pagination.Page": params?.page,
        "Pagination.PageSize": params?.pageSize,
        SortBy: params?.sortBy,
        SortDirection: params?.sortDirection,
      },
    },
  );
  const result = response.data.result;

  if (!result) {
    throw new Error("Locations response does not contain result.");
  }

  return {
    ...result,
    items: result.items.map(toLocation),
  };
}

function toLocation(location: LocationResponse): Location {
  return {
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
  };
}

export const locationsApi = {
  getLocations,

  createLocation: async (request: CreateLocationRequest): Promise<string> => {
    try {
      const response = await apiClient.post<Envelope<string>>(
        "/locations",
        request,
      );

      const result = response.data.result;

      if (result === null) {
        throw new Error("API response result is null");
      }

      return result;
    } catch (error) {
      if (axios.isAxiosError(error) && error.response?.data) {
        const envelope = error.response.data as Envelope;

        if (envelope?.isError && envelope.errorList)
        {
          throw new Error(envelope.errorList[0].message);
        }
      }
      throw error;
    }
  },
};
