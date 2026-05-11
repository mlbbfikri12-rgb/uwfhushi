type LogPayload = Record<string, unknown>;

const isDevelopment = process.env.NODE_ENV !== "production";

export const appLogger = {
  info(message: string, payload?: LogPayload) {
    if (!isDevelopment) return;
    console.info(message, payload ?? {});
  },
  warn(message: string, payload?: LogPayload) {
    if (!isDevelopment) return;
    console.warn(message, payload ?? {});
  },
  error(message: string, payload?: LogPayload) {
    if (!isDevelopment) return;
    console.error(message, payload ?? {});
  },
};
