import axios from 'axios';
import { apiUrl } from '../../config';

// Create an Axios instance with base configuration
const api = axios.create({
  baseURL: `${apiUrl}`,
  withCredentials: true, // Crucial for sending and receiving HttpOnly cookies
  headers: {
    'Content-Type': 'application/json',
  },
});

// Track whether we're currently refreshing to avoid infinite loops
let isRefreshing = false;
// Queue of callbacks waiting on the refresh to complete
let failedQueue: Array<{ resolve: () => void; reject: (err: unknown) => void }> = [];

const processQueue = (error: unknown) => {
  failedQueue.forEach((prom) => {
    if (error) {
      prom.reject(error);
    } else {
      prom.resolve();
    }
  });
  failedQueue = [];
};

// Response interceptor — silently refresh access token when it expires
api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;

    // Only attempt refresh on 401 and only once per request
    if (error.response?.status === 401 && !originalRequest._retry) {
      if (isRefreshing) {
        // Wait for the ongoing refresh to finish then replay this request
        return new Promise((resolve, reject) => {
          failedQueue.push({
            resolve: () => resolve(api(originalRequest)),
            reject,
          });
        });
      }

      originalRequest._retry = true;
      isRefreshing = true;

      try {
        // The refresh token is an HttpOnly cookie — just call the endpoint
        await axios.post(
          'http://localhost:5221/api/v1/auth/refresh-token',
          {},
          { withCredentials: true }
        );
        processQueue(null);
        // Retry the original request — the new access token cookie is now set
        return api(originalRequest);
      } catch (refreshError) {
        processQueue(refreshError);
        // Refresh token is also expired — force logout
        if (typeof window !== 'undefined') {
          window.dispatchEvent(new Event('unauthorized'));
        }
        return Promise.reject(refreshError);
      } finally {
        isRefreshing = false;
      }
    }

    return Promise.reject(error);
  }
);

export default api;

