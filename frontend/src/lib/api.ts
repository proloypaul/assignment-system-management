import axios from 'axios';

// Create an Axios instance with base configuration
const api = axios.create({
  baseURL: 'http://localhost:5221/api/v1',
  withCredentials: true, // Crucial for sending and receiving HttpOnly cookies
  headers: {
    'Content-Type': 'application/json',
  },
});

// Response interceptor to handle global API errors
api.interceptors.response.use(
  (response) => {
    return response;
  },
  async (error) => {
    // Check if the error is 401 Unauthorized
    if (error.response && error.response.status === 401) {
      // Typically, here we might try to refresh the token. 
      // If the refresh token also fails, we redirect to login.
      // For now, we will handle logout globally using the Zustand store
      // We can emit a custom event that the AuthGuard or AuthStore listens to.
      if (typeof window !== 'undefined') {
        window.dispatchEvent(new Event('unauthorized'));
      }
    }
    return Promise.reject(error);
  }
);

export default api;
