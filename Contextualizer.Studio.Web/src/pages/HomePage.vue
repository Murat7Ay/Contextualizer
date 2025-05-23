<template>
  <v-row>
    <v-col cols="12">
      <v-card>
        <v-card-title class="text-h4">
          Welcome to Contextualizer Studio
        </v-card-title>
        <v-card-text>
          <p class="text-body-1">
            Contextualizer Studio is a powerful tool for creating and managing handlers for the Contextualizer application.
          </p>
          <v-row class="mt-4">
            <v-col cols="12" md="6">
              <v-card>
                <v-card-title>Handler Editor</v-card-title>
                <v-card-text>
                  Create and edit handlers with our visual editor. Test your handlers before deploying them.
                </v-card-text>
                <v-card-actions>
                  <v-btn color="primary" to="/editor">
                    Open Editor
                  </v-btn>
                </v-card-actions>
              </v-card>
            </v-col>
            <v-col cols="12" md="6">
              <v-card>
                <v-card-title>Marketplace</v-card-title>
                <v-card-text>
                  Browse and discover handlers created by the community. Share your own handlers with others.
                </v-card-text>
                <v-card-actions>
                  <v-btn color="primary" to="/marketplace">
                    Visit Marketplace
                  </v-btn>
                </v-card-actions>
              </v-card>
            </v-col>
          </v-row>
        </v-card-text>
      </v-card>
    </v-col>
  </v-row>
</template>

<script setup lang="ts">
// Component logic here
</script> 