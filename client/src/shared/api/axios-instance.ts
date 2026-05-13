import axios from "axios";
import { Envelope } from "./envelope";
import { EnvelopeError } from "./errors";

export const apiClient = axios.create({
    baseURL: "/api",
    headers: {
        "Content-Type": "application/json",
    },
});

apiClient.interceptors.response.use(
    (response) => {
        const data = response.data as Envelope;

        if (data.isError && data.errorList)
        {
             
        }

        return response
    },
    (error) => {
        if (axios.isAxiosError(error) && error.response?.data) {
            const envelope = error.response.data as Envelope;

            if (envelope.isError && envelope.errorList) {
                throw new EnvelopeError(envelope.errorList);
            }
        }
        
        return Promise.reject(error);
    }
);
