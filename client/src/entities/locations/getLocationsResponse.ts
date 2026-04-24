export type GetLocationsResponse = {
    locations: Array<{
        id: string;
        name: string;
        street: string;
        city: string;
        country: string;
        timezone: string;
        isActive: boolean;
        createdAt: string;
        updatedAt: string;
    }>;
    totalCount: number;
};
