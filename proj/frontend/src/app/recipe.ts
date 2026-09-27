export interface Ingredient {
  id?: number;
  name: string;
  amount: number | null;
  unit: string | null;
}

export interface Step {
  id?: number;
  text: string;
}

export interface Recipe {
  id?: number;
  title: string;
  description: string | null;
  baseServings: number;
  prepMinutes: number | null;
  cookMinutes: number | null;
  method: string | null;
  category: string | null;
  imageUrl: string | null;
  sourceUrl: string | null;
  createdAt: string | null;
  ingredients: Ingredient[];
  steps: Step[];
}

export interface ExtractResult {
  engine: string | null;
  error: string | null;
  recipe: Recipe;
}
