import { ApiError } from "./errors";

export type Envelope<T = unknown> = {
    errorList: [ApiError];
    isError: boolean;
    result: T | null;
    timeGenerated: string;
};
