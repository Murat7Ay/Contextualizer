<template>
  <v-row>
    <v-col cols="12" md="4">
      <v-card>
        <v-card-title>Handler Properties</v-card-title>
        <v-card-text>
          <v-form>
            <v-text-field
              v-model="handlerConfig.id"
              label="Handler ID"
              required
              hint="Unique identifier for the handler"
            ></v-text-field>
            <v-text-field
              v-model="handlerConfig.name"
              label="Name"
              required
              hint="Display name for the handler"
            ></v-text-field>
            <v-text-field
              v-model="handlerConfig.version"
              label="Version"
              required
              hint="Semantic version (e.g. 1.0.0)"
            ></v-text-field>
            <v-textarea
              v-model="handlerConfig.description"
              label="Description"
              hint="Detailed description of the handler"
              rows="3"
            ></v-textarea>
            <v-select
              v-model="handlerConfig.type"
              :items="handlerTypes"
              label="Handler Type"
              required
            ></v-select>
          </v-form>
        </v-card-text>
      </v-card>
    </v-col>
    <v-col cols="12" md="8">
      <v-card>
        <v-card-title class="d-flex align-center">
          Configuration
          <v-spacer></v-spacer>
          <v-btn color="primary" @click="saveHandler">
            Save Handler
          </v-btn>
        </v-card-title>
        <v-card-text>
          <div ref="editorContainer" style="height: 600px; border: 1px solid #ccc;"></div>
        </v-card-text>
      </v-card>
    </v-col>
  </v-row>
</template>

<script setup lang="ts">
import { ref, onMounted, reactive } from 'vue';
import * as monaco from 'monaco-editor';

const editorContainer = ref<HTMLElement | null>(null);
let editor: monaco.editor.IStandaloneCodeEditor | null = null;

const handlerTypes = ['Api', 'Database', 'File', 'Custom', 'Regex', 'Manual', 'Lookup'];

const handlerConfig = reactive({
  id: '',
  name: '',
  version: '1.0.0',
  description: '',
  type: 'Api',
  config: {}
});

onMounted(() => {
  if (editorContainer.value) {
    editor = monaco.editor.create(editorContainer.value, {
      value: JSON.stringify(handlerConfig.config, null, 2),
      language: 'json',
      theme: 'vs-dark',
      automaticLayout: true,
      minimap: {
        enabled: true
      }
    });

    editor.onDidChangeModelContent(() => {
      try {
        const value = editor?.getValue() || '{}';
        handlerConfig.config = JSON.parse(value);
      } catch (error) {
        console.error('Invalid JSON:', error);
      }
    });
  }
});

const saveHandler = () => {
  // TODO: Implement save functionality
  console.log('Saving handler:', handlerConfig);
};
</script>

<style scoped>
.v-card-text {
  padding-top: 20px;
}
</style> 