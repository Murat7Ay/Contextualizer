<template>
  <v-container>
    <v-row>
      <v-col cols="12">
        <v-card>
          <v-card-title class="d-flex align-center">
            Handler Marketplace
            <v-spacer></v-spacer>
            <v-text-field
              v-model="search"
              append-inner-icon="mdi-magnify"
              label="Search handlers"
              single-line
              hide-details
              class="mx-4"
              style="max-width: 300px"
            ></v-text-field>
            <v-btn color="primary" prepend-icon="mdi-plus" @click="uploadHandler">
              Upload Handler
            </v-btn>
          </v-card-title>
        </v-card>
      </v-col>
    </v-row>

    <v-row>
      <v-col cols="3">
        <v-card>
          <v-card-title>Filters</v-card-title>
          <v-card-text>
            <v-select
              v-model="selectedTypes"
              :items="handlerTypes"
              label="Handler Types"
              multiple
              chips
            ></v-select>
            <v-select
              v-model="sortBy"
              :items="sortOptions"
              label="Sort By"
            ></v-select>
          </v-card-text>
        </v-card>
      </v-col>

      <v-col cols="9">
        <v-row>
          <v-col v-for="handler in filteredHandlers" :key="handler.id" cols="12" md="6">
            <v-card>
              <v-card-title>{{ handler.name }}</v-card-title>
              <v-card-subtitle>
                Version {{ handler.version }} | {{ handler.type }}
              </v-card-subtitle>
              <v-card-text>
                <p>{{ handler.description }}</p>
                <v-chip-group>
                  <v-chip color="primary" size="small">
                    {{ handler.downloads }} Downloads
                  </v-chip>
                  <v-chip color="secondary" size="small">
                    {{ handler.rating }} ★
                  </v-chip>
                </v-chip-group>
              </v-card-text>
              <v-card-actions>
                <v-btn variant="text" @click="viewDetails(handler)">
                  View Details
                </v-btn>
                <v-spacer></v-spacer>
                <v-btn color="primary" @click="installHandler(handler)">
                  Install
                </v-btn>
              </v-card-actions>
            </v-card>
          </v-col>
        </v-row>
      </v-col>
    </v-row>

    <!-- Upload Dialog -->
    <v-dialog v-model="uploadDialog" max-width="500px">
      <v-card>
        <v-card-title>Upload Handler</v-card-title>
        <v-card-text>
          <v-file-input
            v-model="uploadFile"
            label="Handler Configuration"
            accept=".json"
            show-size
          ></v-file-input>
        </v-card-text>
        <v-card-actions>
          <v-spacer></v-spacer>
          <v-btn color="primary" @click="confirmUpload">
            Upload
          </v-btn>
          <v-btn color="error" @click="uploadDialog = false">
            Cancel
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </v-container>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';

// Mock data
const handlers = ref([
  {
    id: 'github-user-info',
    name: 'GitHub User Info',
    version: '1.0.0',
    type: 'Api',
    description: 'Fetches user information from GitHub API',
    downloads: 1200,
    rating: 4.5
  },
  {
    id: 'jira-ticket',
    name: 'JIRA Ticket Handler',
    version: '2.1.0',
    type: 'Api',
    description: 'Creates and updates JIRA tickets',
    downloads: 800,
    rating: 4.2
  }
]);

const handlerTypes = ['Api', 'Database', 'File', 'Custom', 'Regex', 'Manual', 'Lookup'];
const sortOptions = ['Name', 'Downloads', 'Rating', 'Latest'];

const search = ref('');
const selectedTypes = ref([]);
const sortBy = ref('Latest');
const uploadDialog = ref(false);
const uploadFile = ref(null);

const filteredHandlers = computed(() => {
  return handlers.value.filter(handler => {
    const matchesSearch = handler.name.toLowerCase().includes(search.value.toLowerCase()) ||
                         handler.description.toLowerCase().includes(search.value.toLowerCase());
    const matchesType = selectedTypes.value.length === 0 || selectedTypes.value.includes(handler.type);
    return matchesSearch && matchesType;
  });
});

const uploadHandler = () => {
  uploadDialog.value = true;
};

const confirmUpload = () => {
  // TODO: Implement upload functionality
  console.log('Uploading file:', uploadFile.value);
  uploadDialog.value = false;
};

const viewDetails = (handler: any) => {
  // TODO: Implement view details functionality
  console.log('Viewing details:', handler);
};

const installHandler = (handler: any) => {
  // TODO: Implement install functionality
  console.log('Installing handler:', handler);
};
</script> 