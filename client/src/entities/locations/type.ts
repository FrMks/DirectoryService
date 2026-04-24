export type Location = {
    id: string;
    name: string;
    address: Address;
    timeZone: string;
    isActive: boolean;
    createdAt: string;
    updatedAt: string;
}

export type Address = {
    street: string;
    city: string;
    country: string;
}