export type ApiError = {
    code: string;
    message: string;
    invalidField?: string | null;
    type: ErrorType;
};

export type ErrorType = "validation" | "not_found" | "failure" | "unknown";