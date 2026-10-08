import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'recipes' },
  { path: 'recipes', loadComponent: () => import('./recipes/recipes').then(m => m.Recipes) },
  { path: 'recipes/new', loadComponent: () => import('./recipe-form/recipe-form').then(m => m.RecipeForm) },
  { path: 'recipes/:id', loadComponent: () => import('./recipe-detail/recipe-detail').then(m => m.RecipeDetail) },
  { path: 'recipes/:id/edit', loadComponent: () => import('./recipe-form/recipe-form').then(m => m.RecipeForm) },
  { path: 'import', loadComponent: () => import('./import/import').then(m => m.Import) },
  { path: 'settings', loadComponent: () => import('./settings/settings').then(m => m.Settings) },
  { path: '**', redirectTo: 'recipes' }
];
