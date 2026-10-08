import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ApiService } from '../api';
import { Recipe } from '../recipe';

@Component({
  selector: 'app-recipe-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './recipe-detail.html',
  styleUrl: './recipe-detail.css'
})
export class RecipeDetail implements OnInit {
  readonly api = inject(ApiService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  recipe = signal<Recipe | null>(null);
  error = signal<string | null>(null);
  servings = signal(4);
  savedPhoto = signal(false);
  confirmDelete = signal(false);

  scale = computed(() => {
    const base = this.recipe()?.baseServings || 1;
    return this.servings() / base;
  });

  amountText(r: Recipe, i: { name: string; amount: number | null; unit: string | null }) {
    if (i.amount == null) return i.unit ? i.unit : '';
    const scaled = i.amount * this.scale();
    const rounded = scaled >= 10 ? Math.round(scaled) : Math.round(scaled * 10) / 10;
    return rounded + (i.unit ? ' ' + i.unit : '');
  }

  decServings() { this.servings.set(Math.max(1, this.servings() - 1)); }

  ngOnInit() {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.api.getRecipe(id).subscribe({
      next: r => {
        this.recipe.set(r);
        this.servings.set(r.baseServings || 4);
        // Reset savedPhoto when loading a new recipe
        this.savedPhoto.set(false);
      },
      error: e => this.error.set('Errore: ' + (e?.message ?? e))
    });
  }

  onPhotoChange(ev: Event) {
    const file = (ev.target as HTMLInputElement).files?.[0];
    const r = this.recipe();
    if (!file || !r?.id) return;
    this.api.uploadPhoto(r.id, file).subscribe({
      next: res => {
        this.recipe.update(x => x ? { ...x, imageUrl: res.imageUrl } : x);
        this.savedPhoto.set(true);
        setTimeout(() => this.savedPhoto.set(false), 2000);
      },
      error: e => this.error.set('Upload foto: ' + (e?.message ?? e))
    });
  }

  delete() {
    const r = this.recipe();
    if (!r?.id) return;
    this.confirmDelete.set(false);
    this.api.deleteRecipe(r.id).subscribe({
      next: () => this.router.navigate(['/recipes']),
      error: e => this.error.set('Eliminazione: ' + (e?.message ?? e))
    });
  }
}
