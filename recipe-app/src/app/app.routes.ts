import { Routes } from '@angular/router';
import { Home } from './home/home';

export const routes: Routes = [
  { path: '', redirectTo: 'home', pathMatch: 'full' },

  // Public pages
  { path: 'home', component: Home },
  {
    path: 'login',
    loadComponent: () => import('./pages/login/login').then(m => m.Login)
  },
  {
    path: 'register',
    loadComponent: () => import('./pages/register/register').then(m => m.Register)
  },

  // Recipe routes
  {
    path: 'recipe/new',
    loadComponent: () => import('./pages/recipe-form/recipe-form').then(m => m.RecipeForm)
  },
  {
    path: 'recipe/:id',
    loadComponent: () => import('./pages/recipe-detail/recipe-detail').then(m => m.RecipeDetail)
  },
  {
    path: 'recipe/:id/edit',
    loadComponent: () => import('./pages/recipe-form/recipe-form').then(m => m.RecipeForm)
  },

  // Fallback
  { path: '**', redirectTo: 'home' }
];