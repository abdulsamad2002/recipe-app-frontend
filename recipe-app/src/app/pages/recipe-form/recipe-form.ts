import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { RecipeService } from '../../services/recipe-service';
import { CategoryService } from '../../services/category.service';
import { AuthService } from '../../services/auth.service';
import { Category, Ingredient } from '../../interface/recipes';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-recipe-form',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './recipe-form.html',
  styleUrl: './recipe-form.css'
})
export class RecipeForm implements OnInit {
  // Form fields
  title       = '';
  description = '';
  stepsText   = '';   // newline-separated input -> split into string[]
  ingredients = signal<Ingredient[]>([{ name: '', quantity: '' }]);
  selectedCategoryIds = signal<number[]>([]);
  categories  = signal<Category[]>([]);
  imageFile: File | null = null;

  isEdit   = false;
  recipeId = 0;
  loading  = signal(false);
  error    = signal<string | null>(null);
  imagePreview = signal<string | null>(null);

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private recipeService: RecipeService,
    private categoryService: CategoryService,
    private auth: AuthService
  ) {}

  ngOnInit(): void {
    if (!this.auth.isLoggedIn()) {
      this.router.navigate(['/login']);
      return;
    }
    this.categoryService.getAll().subscribe(c => this.categories.set(c));
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (id) {
      this.isEdit = true;
      this.recipeId = id;
      this.recipeService.getById(id).subscribe({
        next: (r) => {
          this.title       = r.title;
          this.description = r.description;
          this.stepsText   = r.steps.join('\n');
          this.ingredients.set(r.ingredients.length ? r.ingredients : [{ name: '', quantity: '' }]);
          this.imagePreview.set(r.imageUrl ? this.resolveImageUrl(r.imageUrl) : null);
          // Match categories by name
          const allCats = this.categories();
          const matchedIds = r.categories
            .map(name => allCats.find(c => c.name === name)?.categoryId)
            .filter((id): id is number => !!id);
          this.selectedCategoryIds.set(matchedIds);
        },
        error: () => this.router.navigate(['/'])
      });
    }
  }

  addIngredient(): void {
    this.ingredients.update(list => [...list, { name: '', quantity: '' }]);
  }

  removeIngredient(i: number): void {
    this.ingredients.update(list => list.filter((_, idx) => idx !== i));
  }

  toggleCategory(id: number): void {
    this.selectedCategoryIds.update(ids =>
      ids.includes(id) ? ids.filter(x => x !== id) : [...ids, id]
    );
  }

  onFileChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file  = input.files?.[0] ?? null;
    this.imageFile = file;
    if (file) {
      const reader = new FileReader();
      reader.onload = () => this.imagePreview.set(reader.result as string);
      reader.readAsDataURL(file);
    }
  }

  submit(): void {
    const steps = this.stepsText.split('\n').map(s => s.trim()).filter(Boolean);
    const ings  = this.ingredients().filter(i => i.name.trim());

    if (!this.title.trim()) { this.error.set('Title is required.'); return; }
    if (steps.length === 0) { this.error.set('At least one step is required.'); return; }

    this.loading.set(true);
    this.error.set(null);

    const dto = {
      title: this.title.trim(),
      description: this.description.trim(),
      steps,
      ingredients: ings,
      categoryIds: this.selectedCategoryIds()
    };

    const obs = this.isEdit
      ? this.recipeService.update(this.recipeId, dto)
      : this.recipeService.create(dto);

    obs.subscribe({
      next: (recipe) => {
        const id = recipe.recipeId;
        if (this.imageFile) {
          this.recipeService.uploadImage(id, this.imageFile).subscribe({
            next: () => this.router.navigate(['/recipe', id]),
            error: () => this.router.navigate(['/recipe', id])  // navigate even if upload fails
          });
        } else {
          this.router.navigate(['/recipe', id]);
        }
      },
      error: (err) => {
        this.error.set(err?.error?.message ?? 'Failed to save recipe.');
        this.loading.set(false);
      }
    });
  }

  /** Resolve a backend-relative image URL to absolute for the preview <img>. */
  resolveImageUrl(url: string | null | undefined): string | null {
    if (!url) return null;
    if (url.startsWith('http://') || url.startsWith('https://')) return url;
    const base = environment.mediaBase.replace(/\/$/, '');
    return `${base}${url.startsWith('/') ? url : '/' + url}`;
  }
}
