import { createRouter, createWebHistory, RouteRecordRaw } from 'vue-router';

const routes: Array<RouteRecordRaw> = [
  {
    path: '/',
    name: 'Home',
    component: () => import('../pages/HomePage.vue'),
  },
  {
    path: '/editor',
    name: 'Editor',
    component: () => import('../pages/EditorPage.vue'),
  },
  {
    path: '/marketplace',
    name: 'Marketplace',
    component: () => import('../pages/MarketplacePage.vue'),
  },
];

const router = createRouter({
  history: createWebHistory(),
  routes,
});

export default router; 