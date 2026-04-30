import { PaginationResponse } from "@/shared/api/types";

export type GetLocationsResponse = PaginationResponse<LocationResponse>;

export type LocationResponse = {
  id: string;
  name: string;
  street: string;
  city: string;
  country: string;
  timezone: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
};
