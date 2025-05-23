<template>
  <v-app>
    <v-app-bar app>
      <v-app-bar-nav-icon @click="drawer = !drawer"></v-app-bar-nav-icon>
      <v-toolbar-title>Contextualizer Studio</v-toolbar-title>
      <v-spacer></v-spacer>
      <v-btn icon @click="toggleTheme">
        <v-icon>{{ isDark ? 'mdi-weather-sunny' : 'mdi-weather-night' }}</v-icon>
      </v-btn>
    </v-app-bar>

    <v-navigation-drawer v-model="drawer" app>
      <v-list>
        <v-list-item to="/" :title="'Home'" prepend-icon="mdi-home"></v-list-item>
        <v-list-item to="/editor" :title="'Handler Editor'" prepend-icon="mdi-pencil"></v-list-item>
        <v-list-item to="/marketplace" :title="'Marketplace'" prepend-icon="mdi-store"></v-list-item>
      </v-list>
    </v-navigation-drawer>

    <v-main>
      <v-container fluid>
        <router-view></router-view>
      </v-container>
    </v-main>
  </v-app>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { useTheme } from 'vuetify';

const theme = useTheme();
const drawer = ref(false);
const isDark = ref(theme.global.current.value.dark);

const toggleTheme = () => {
  theme.global.name.value = theme.global.current.value.dark ? 'light' : 'dark';
  isDark.value = !isDark.value;
};
</script>

<style>
.v-application {
  font-family: 'Roboto', sans-serif;
}
</style>
