import { Component, inject, OnInit, OnDestroy } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { ApiService } from '../api';
import { Recipe } from '../recipe';

@Component({
  selector: 'app-recipes',
  standalone: true,
  imports: [RouterLink, FormsModule, CommonModule],
  templateUrl: './recipes.html',
  styleUrl: './recipes.css'
})
export class Recipes implements OnInit, OnDestroy {
  readonly api = inject(ApiService);

  readonly recipes = this.api.recipes;
  readonly loading = this.api.loading;
  readonly error = this.api.error;
  readonly search = this.api.search;

  private searchTimer: ReturnType<typeof setTimeout> | null = null;
  private loadSub: { unsubscribe(): void } | null = null;

  onSearchInput(v: string) {
    this.api.setSearch(v);
    if (this.searchTimer) clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => this.reload(), 300);
  }

  clearSearch() {
    if (this.searchTimer) clearTimeout(this.searchTimer);
    this.api.setSearch('');
    this.reload();
  }

  private reload() {
    this.loadSub?.unsubscribe();
    this.loadSub = this.api.loadRecipes().subscribe();
  }

  ngOnInit() {
    this.reload();
  }

  ngOnDestroy() {
    if (this.searchTimer) clearTimeout(this.searchTimer);
    this.loadSub?.unsubscribe();
  }
}
