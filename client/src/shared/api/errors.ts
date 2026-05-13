export type ApiError = {
    code: string;
    message: string;
    invalidField?: string | null;
    type: ErrorType;
};

export type ErrorType = "validation" | "not_found" | "failure" | "unknown";

export class EnvelopeError extends Error {
    public readonly apiErrors: ApiError[];
    public readonly type: ErrorType

    constructor(apiErrors: ApiError[]) {
        const firstMessage = apiErrors[0].message ?? "Unknown error";
        
        super(firstMessage);

        this.name = "EnvelopeError";
        this.apiErrors = apiErrors;
        this.type = apiErrors[0].type;

        Object.setPrototypeOf(this, EnvelopeError.prototype);
    }

    get messages(): ApiError[]
    {
        return this.apiErrors;
    }

    get firstMessage(): string {
        return this.apiErrors[0].message  ?? "Unknown error";
    }

    getAllMessages(): string[] {
        return this.apiErrors.map((msg) => msg.message);
    }
}

export function isEnvelopeError(error: unknown): error is EnvelopeError {
    return error instanceof EnvelopeError;
}