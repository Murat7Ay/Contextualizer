import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import { Handler, handlerService } from '../services/api';

export const useHandlerStore = defineStore('handlers', () => {
  const handlers = ref<Handler[]>([]);
  const loading = ref(false);
  const error = ref<string | null>(null);

  const getHandlerById = computed(() => {
    return (id: string) => handlers.value.find(h => h.id === id);
  });

  const fetchHandlers = async () => {
    try {
      loading.value = true;
      error.value = null;
      handlers.value = await handlerService.getHandlers();
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Failed to fetch handlers';
      console.error('Error fetching handlers:', err);
    } finally {
      loading.value = false;
    }
  };

  const createHandler = async (handler: Omit<Handler, 'id'>) => {
    try {
      loading.value = true;
      error.value = null;
      const newHandler = await handlerService.createHandler(handler);
      handlers.value.push(newHandler);
      return newHandler;
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Failed to create handler';
      console.error('Error creating handler:', err);
      throw err;
    } finally {
      loading.value = false;
    }
  };

  const updateHandler = async (id: string, handler: Partial<Handler>) => {
    try {
      loading.value = true;
      error.value = null;
      const updatedHandler = await handlerService.updateHandler(id, handler);
      const index = handlers.value.findIndex(h => h.id === id);
      if (index !== -1) {
        handlers.value[index] = updatedHandler;
      }
      return updatedHandler;
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Failed to update handler';
      console.error('Error updating handler:', err);
      throw err;
    } finally {
      loading.value = false;
    }
  };

  const deleteHandler = async (id: string) => {
    try {
      loading.value = true;
      error.value = null;
      await handlerService.deleteHandler(id);
      handlers.value = handlers.value.filter(h => h.id !== id);
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Failed to delete handler';
      console.error('Error deleting handler:', err);
      throw err;
    } finally {
      loading.value = false;
    }
  };

  const uploadHandler = async (file: File) => {
    try {
      loading.value = true;
      error.value = null;
      const newHandler = await handlerService.uploadHandler(file);
      handlers.value.push(newHandler);
      return newHandler;
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Failed to upload handler';
      console.error('Error uploading handler:', err);
      throw err;
    } finally {
      loading.value = false;
    }
  };

  const installHandler = async (id: string) => {
    try {
      loading.value = true;
      error.value = null;
      await handlerService.installHandler(id);
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Failed to install handler';
      console.error('Error installing handler:', err);
      throw err;
    } finally {
      loading.value = false;
    }
  };

  return {
    handlers,
    loading,
    error,
    getHandlerById,
    fetchHandlers,
    createHandler,
    updateHandler,
    deleteHandler,
    uploadHandler,
    installHandler
  };
}); 