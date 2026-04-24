export type Envelope<T = unknown> = {
    result: T | null;
    error: ApiError | null;
    isError: boolean;
    timeGenerated: string;
};

export type ApiError = {
    messages: ErrorMessage[];
    type: ErrorType;
};

export type ErrorMessage = {
    code: string;
    message: string;
    invalidField?: string | null;
};

export type ErrorType = "validation" | "not_found" | "failure" | "unknown";
