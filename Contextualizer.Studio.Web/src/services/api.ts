import axios from 'axios';

const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5000/api',
  headers: {
    'Content-Type': 'application/json'
  }
});

export interface Handler {
  id: string;
  name: string;
  version: string;
  type: string;
  description: string;
  config: Record<string, any>;
  downloads?: number;
  rating?: number;
}

export const handlerService = {
  async getHandlers(): Promise<Handler[]> {
    const response = await api.get('/handlers');
    return response.data;
  },

  async getHandler(id: string): Promise<Handler> {
    const response = await api.get(`/handlers/${id}`);
    return response.data;
  },

  async createHandler(handler: Omit<Handler, 'id'>): Promise<Handler> {
    const response = await api.post('/handlers', handler);
    return response.data;
  },

  async updateHandler(id: string, handler: Partial<Handler>): Promise<Handler> {
    const response = await api.put(`/handlers/${id}`, handler);
    return response.data;
  },

  async deleteHandler(id: string): Promise<void> {
    await api.delete(`/handlers/${id}`);
  },

  async uploadHandler(file: File): Promise<Handler> {
    const formData = new FormData();
    formData.append('file', file);
    const response = await api.post('/handlers/upload', formData, {
      headers: {
        'Content-Type': 'multipart/form-data'
      }
    });
    return response.data;
  },

  async installHandler(id: string): Promise<void> {
    await api.post(`/handlers/${id}/install`);
  }
};

export default api; 