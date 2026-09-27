import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ApiService } from '../api';
import { Recipe, Ingredient, Step } from '../recipe';

@Component({
  selector: 'app-recipe-form',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './recipe-form.html',
  styleUrl: './recipe-form.css'
})
export class RecipeForm implements OnInit {
  readonly api = inject(ApiService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  editing = signal(false);
  editingId = signal<number | null>(null);
  saving = signal(false);
  error = signal<string | null>(null);
  touchedTitle = signal(false);
  confirmCancel = signal(false);

  titleInvalid = () => this.touchedTitle() && !this.form().title?.trim();

  form = signal<Recipe>({
    title: '',
    description: '',
    baseServings: 4,
    prepMinutes: null,
    cookMinutes: null,
    method: '',
    category: '',
    imageUrl: null,
    sourceUrl: null,
    createdAt: null,
    ingredients: [],
    steps: []
  });

  ngOnInit() {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      const id = Number(idParam);
      this.editing.set(true);
      this.editingId.set(id);
      this.api.getRecipe(id).subscribe({
        next: r => this.form.set({
          ...r,
          description: r.description ?? '',
          method: r.method ?? '',
          category: r.category ?? ''
        }),
        error: e => this.error.set('Errore: ' + (e?.message ?? e))
      });
    }
  }

  addIngredient() {
    this.form.update(f => ({
      ...f,
      ingredients: [...f.ingredients, { name: '', amount: null, unit: '' }]
    }));
  }

  removeIngredient(i: number) {
    this.form.update(f => ({ ...f, ingredients: f.ingredients.filter((_, idx) => idx !== i) }));
  }

  addStep() {
    this.form.update(f => ({ ...f, steps: [...f.steps, { text: '' }] }));
  }

  removeStep(i: number) {
    this.form.update(f => ({ ...f, steps: f.steps.filter((_, idx) => idx !== i) }));
  }

  onCancelClick() {
    this.confirmCancel.set(true);
  }

  cancel() {
    this.confirmCancel.set(false);
    this.router.navigate(['/recipes']);
  }

  save() {
    const f = this.form();
    if (!f.title?.trim()) {
      this.touchedTitle.set(true);
      this.error.set('Il titolo è obbligatorio');
      return;
    }
    this.saving.set(true);
    this.error.set(null);
    const payload: Recipe = {
      ...f,
      ingredients: f.ingredients.filter(i => i.name?.trim()),
      steps: f.steps.filter(s => s.text?.trim())
    };
    const op = this.editingId()
      ? this.api.updateRecipe(this.editingId()!, payload)
      : this.api.createRecipe(payload);
    op.subscribe({
      next: r => this.router.navigate(['/recipes', r.id]),
      error: e => {
        this.error.set('Salvataggio: ' + (e?.error?.error ?? e?.message ?? e));
        this.saving.set(false);
      }
    });
  }
}
